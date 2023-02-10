using System;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Api;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Integration.Infrastructure.CosmosDb
{
    internal class CosmosDbFixture : IAsyncDisposable
    {
        public CollectionConfig Config { get; }
        private readonly CosmosClient _client;
        private Database? _database;

        public CosmosDbFixture(CollectionConfig config)
        {
            Config = config;
            _client = new CosmosClient(config.ConnectionString)
            {
                ClientOptions =
                {
                    MaxRetryAttemptsOnRateLimitedRequests   = 10,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(100)
                }
            };
        }

        public async Task AddDocument<T>(T documentBase) where T : DocumentBase
        {
            var container = (await _database!.CreateContainerIfNotExistsAsync(new ContainerProperties(documentBase.ContainerName, "/partitionKey"))).Container;

            await container.CreateItemAsync(documentBase);
        }

        public async ValueTask DisposeAsync()
        {
            if (_database is not null)
                await _database.DeleteAsync();
            _client.Dispose();
        }

        public async Task Initialize()
        {
            _database = await _client.CreateDatabaseIfNotExistsAsync(Config.DbName);
        }

        public async Task DropCollection(string collectionName)
        {
            var container = (await _database!.CreateContainerIfNotExistsAsync(new ContainerProperties(collectionName, "/partitionKey"))).Container;

            await container.DeleteContainerAsync();
        }
    }

    [CollectionDefinition("Cosmos DB collection")]
    public class DatabaseCollection : ICollectionFixture<CosmosMigratorApplicationFactory<Program>>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    internal abstract class DocumentBase
    {
        [JsonIgnore]
        public abstract string ContainerName { get; }

        [JsonProperty("partitionKey")]
        public abstract string PartitionKey { get; }

        [JsonProperty("id")]
        public abstract string Id { get; }
    }
}