using System;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Api;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Integration.Infrastructure.CosmosDb
{
    [Collection("Cosmos DB collection")]
    public class StatisticsCosmosRepositorySpec
    {
        private readonly IStatisticsRepository _statisticsRepository;

        [Fact]
        public async Task Able_to_retrieve_stored_entity()
        {
            var statistics = await StoreNewStatistics();

            var stored = await _statisticsRepository.Get(statistics.MigrationId);

            CompareStoredWithActual(stored.Value, statistics);
        }

        private static void CompareStoredWithActual(Statistics stored, Statistics actual)
        {
            stored.Eta.ShouldBe(actual.Eta);
            stored.Percentage.ShouldBe(actual.Percentage);
            stored.DestinationCount.ShouldBe(actual.DestinationCount);
            stored.SourceCount.ShouldBe(actual.SourceCount);
            stored.EndTime.ShouldBe(actual.EndTime);
            stored.LastUpdate.ShouldBe(actual.LastUpdate);
            stored.StartTime.ShouldBe(actual.StartTime);
            stored.AverageInsertRatePerSeconds.ShouldBe(actual.AverageInsertRatePerSeconds);
            stored.CurrentInsertRatePerSeconds.ShouldBe(actual.CurrentInsertRatePerSeconds);
            stored.Version.ShouldNotBeNull();
            stored.Version.ShouldNotBe(actual.Version);
        }

        [Fact]
        public async Task Able_to_update_entity()
        {
            var statistics = await StoreNewStatistics();
            var actual = (await _statisticsRepository.Get(statistics.MigrationId)).Value;
            actual.Start();
            actual.Update(100, 10);

            await _statisticsRepository.Update(actual);

            var stored = await _statisticsRepository.Get(statistics.MigrationId);

            CompareStoredWithActual(stored.Value, actual);
        }

        private async Task<Statistics> StoreNewStatistics()
        {
            var statistics = Statistics.Create(Guid.NewGuid());
            await _statisticsRepository.Add(statistics);

            return statistics;
        }

        public StatisticsCosmosRepositorySpec(CosmosMigratorApplicationFactory<Program> factory)
        {
            factory.Server.AllowSynchronousIO = true;
            _statisticsRepository = factory.Services.GetRequiredService<IStatisticsRepository>();
        }

        private class TestDocumentBase : DocumentBase
        {
            public override string ContainerName { get; }
            public override string PartitionKey => Id;
            public override string Id { get; }

            public string SomeData { get; }

            public TestDocumentBase(Guid id, string someData, string containerName)
            {
                ContainerName = containerName;

                Id = id.ToString();
                SomeData = someData;
            }
        }
    }
}