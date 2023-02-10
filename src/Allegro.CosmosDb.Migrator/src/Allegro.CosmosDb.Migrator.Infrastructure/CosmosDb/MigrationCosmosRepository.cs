using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb
{
    internal sealed class MigrationCosmosRepository : IMigrationRepository
    {
        private readonly ICosmosRepository<MigrationDocument> _repository;

        public MigrationCosmosRepository(ICosmosRepository<MigrationDocument> repository)
        {
            _repository = repository;
        }

        public Task Add(Migration migration)
        {
            var migrationDocument = MigrationDocument.CreateFrom(migration);
            return _repository.Add(migrationDocument);
        }

        public async Task Update(Migration migration)
        {
            var migrationDocument = MigrationDocument.CreateFrom(migration);
            await _repository.Update(migrationDocument);
        }

        public async Task<Migration> Get(AggregateId id)
        {
            var migrationDocumentResponse = await _repository.Get(new CosmosId(id, id));
            return migrationDocumentResponse.ToMigration();
        }

        public async IAsyncEnumerable<Migration> FindActive([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var migrationDocuments = _repository.Container.GetItemLinqQueryable<MigrationDocument>(requestOptions: new QueryRequestOptions()
            {
                MaxItemCount = 100
            })
                .Where(m => (m.EntityType == nameof(MigrationDocument) && m.MigrationSnapshot.IsActive) || !m.MigrationSnapshot.Initialized)
                .ToFeedIterator().ReadAll();

            await foreach (var migrationDocument in migrationDocuments.WithCancellation(cancellationToken))
            {
                yield return migrationDocument.ToMigration();
            }
        }
    }

    internal class MigrationDocument : ICosmosDocument
    {
        public const string PartitionKeyFieldName = "partitionKey";

        [JsonProperty("id")]
        public string Id { get; }

        public MigrationSnapshot MigrationSnapshot { get; }

        [JsonProperty(PartitionKeyFieldName)]
        public string PartitionKey => Id;

        [JsonProperty("entityType")]
        public string EntityType => nameof(MigrationDocument);

        [JsonProperty("_etag")]
        public string Etag { get; private set; }

        [JsonConstructor]
        // ReSharper disable once InconsistentNaming
        private MigrationDocument(string id, MigrationSnapshot migrationSnapshot, string etag)
        {
            Id = id;
            MigrationSnapshot = migrationSnapshot;
            Etag = etag;
        }

        public static MigrationDocument CreateFrom(Migration migration)
        {
            var migrationSnapshot = migration.ToSnapshot();
            return new MigrationDocument(migration.Id, migrationSnapshot, migration.Version);
        }

        public Migration ToMigration()
        {
            return Migration.FromSnapshot(MigrationSnapshot, Etag);
        }

        public MigrationDto AsDto()
        {
            return new MigrationDto(
                Id,
                MigrationSnapshot.SourceConfig,
                MigrationSnapshot.DestinationConfig,
                MigrationSnapshot.StartFrom,
                MigrationSnapshot.Initialized,
                MigrationSnapshot.Completed,
                MigrationSnapshot.Paused,
                Etag
                );
        }
    }

    internal sealed class MigrationsContainerInitializer : ICosmosDbContainerInitializer
    {
        public Task Initialize(ICosmosStorage cosmosStorage, CancellationToken cancellationToken)
        {
            return cosmosStorage.InitializeContainerIfNotExistsForDocument<MigrationDocument>(
                new ContainerProperties()
                {
                    Id = "migrations",
                    PartitionKeyPath = $"/{MigrationDocument.PartitionKeyFieldName}",
                    IndexingPolicy = new IndexingPolicy()
                    {
                        Automatic = false,
                        IndexingMode = IndexingMode.None
                    }
                }
            );
        }
    }
}