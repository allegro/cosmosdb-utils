using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb
{
    internal sealed class StatisticsCosmosRepository : IStatisticsRepository
    {
        private readonly ICosmosRepository<StatisticsDocument> _repository;

        public StatisticsCosmosRepository(ICosmosRepository<StatisticsDocument> repository)
        {
            _repository = repository;
        }

        public Task Add(Statistics statistics)
        {
            var document = StatisticsDocument.CreateFrom(statistics);
            return _repository.Add(document);
        }

        public Task Update(Statistics statistics)
        {
            var document = StatisticsDocument.CreateFrom(statistics);
            return _repository.Update(document);
        }

        public async Task<Option<Statistics>> Get(AggregateId id)
        {
            try
            {
                var document = await _repository.Get(new CosmosId(StatisticsDocument.BuildId(id), id));
                return new Option<Statistics>(document.ToStatistics());
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Option<Statistics>.Empty;
            }
        }
    }

    internal class StatisticsDocument : ICosmosDocument
    {
        public static StatisticsDocument CreateFrom(Statistics statistics) =>
            new(BuildId(statistics.MigrationId),
                statistics.MigrationId,
                statistics.ToSnapshot(),
                statistics.Version);

        public static string BuildId(Guid migrationId) => $"Statistics:{migrationId}";

        public const string PartitionKeyFieldName = "partitionKey";

        [JsonProperty("id")]
        public string Id { get; }

        public string MigrationId { get; }

        public StatisticsSnapshot StatisticsSnapshot { get; }

        [JsonProperty(PartitionKeyFieldName)]
        public string PartitionKey => MigrationId;

        [JsonProperty("entityType")]
        public string EntityType => nameof(StatisticsDocument);

        [JsonProperty("_etag")]// TODO: check if it can be moved to interface
        public string Etag { get; }

        [JsonConstructor]
        // ReSharper disable once InconsistentNaming
        private StatisticsDocument(string id, string migrationId, StatisticsSnapshot statisticsSnapshot, string etag)
        {
            Id = id;
            MigrationId = migrationId;
            StatisticsSnapshot = statisticsSnapshot;
            Etag = etag;
        }

        public Statistics ToStatistics()
        {
            return Statistics.FromSnapshot(StatisticsSnapshot, Etag);
        }

        public StatisticsDto AsDto()
        {
            return new()
            {
                MigrationId = MigrationId,
                Eta = StatisticsSnapshot.Eta,
                Percentage = StatisticsSnapshot.Percentage,
                DestinationCount = StatisticsSnapshot.DestinationCount,
                EndTime = StatisticsSnapshot.EndTime,
                LastUpdate = StatisticsSnapshot.LastUpdate,
                SourceCount = StatisticsSnapshot.SourceCount,
                StartTime = StatisticsSnapshot.StartTime,
                AverageInsertRatePerSeconds = StatisticsSnapshot.AverageInsertRatePerSeconds,
                CurrentInsertRatePerSeconds = StatisticsSnapshot.CurrentInsertRatePerSeconds
            };
        }
    }

    internal sealed class StatisticsContainerInitializer : ICosmosDbContainerInitializer
    {
        public Task Initialize(ICosmosStorage cosmosStorage, CancellationToken cancellationToken)
        {
            return cosmosStorage.InitializeContainerIfNotExistsForDocument<StatisticsDocument>(
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