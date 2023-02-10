using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals.Exceptions;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals
{
    internal interface ICosmosStorage
    {
        CosmosClient Client { get; }
        Database? Database { get; }
        Container GetContainerForDocument<T>() where T : class, ICosmosDocument;

        Task InitializeStorage();

        Task InitializeContainerIfNotExistsForDocument<T>(ContainerProperties containerProperties) where T : class, ICosmosDocument;
    }

    internal sealed class CosmosStorage : ICosmosStorage
    {
        private readonly CosmosDbOptions _options;
        private readonly ConcurrentDictionary<string, Container> _containers = new();
        public CosmosClient Client { get; }
        public Database? Database { get; private set; }

        public CosmosStorage(CosmosClient cosmosClient, CosmosDbOptions options)
        {
            _options = options;
            Client = cosmosClient;
        }

        public Container GetContainerForDocument<T>() where T : class, ICosmosDocument
        {
            var documentTypeKey = DocumentTypeKey<T>();
            if (_containers.TryGetValue(documentTypeKey, out var container))
            {
                return container;
            }

            throw new CosmosContainerNotInitializedException(documentTypeKey);
        }

        public async Task InitializeStorage()
        {
            if (Database is not null)
            {
                return;
            }

            Database = await Client.CreateDatabaseIfNotExistsAsync(_options.Database);
        }

        public async Task InitializeContainerIfNotExistsForDocument<T>(ContainerProperties containerProperties)
            where T : class, ICosmosDocument
        {
            if (Database is null)
            {
                throw new NotSupportedException("Database must be initialized at that point");
            }

            var containerResponse = await Database.CreateContainerIfNotExistsAsync(containerProperties);

            _containers.TryAdd(DocumentTypeKey<T>(), containerResponse.Container);
        }

        private static string DocumentTypeKey<T>() where T : class, ICosmosDocument
        {
            return typeof(T).FullName!;
        }
    }
}