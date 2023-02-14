using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Application.Migration
{
    public class MigrationProgressManagerSpec
    {
        private readonly ApplicationFixture _fixture;

        public MigrationProgressManagerSpec()
        {
            _fixture = new ApplicationFixture();

            _fixture
                .Build();
        }

        [Fact]
        public async Task No_active_migration()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProgressManager>();

            //when:
            await sut.ExecuteAsync(CancellationToken.None);
        }

        [Fact]
        public async Task Active_migration_empty_collections()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProgressManager>();
            var migration = await _fixture.WithActiveMigration("first");

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:

            var statistics = await GetStatisticsForMigration(migration);

            statistics.SourceCount.ShouldBe(0);
            statistics.DestinationCount.ShouldBe(0);
            statistics.Percentage.ShouldBe(double.NaN);
        }

        [Fact]
        public async Task Active_migration_nothing_was_migrated()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProgressManager>();
            var migration = await _fixture.WithActiveMigration("first");

            await WithDocuments(1, migration.SourceConfig);

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:

            var statistics = await GetStatisticsForMigration(migration);

            statistics.SourceCount.ShouldBe(1);
            statistics.DestinationCount.ShouldBe(0);
            statistics.Percentage.ShouldBe(0);
        }

        [Fact]
        public async Task Active_migration_in_progress()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProgressManager>();
            var migration = await _fixture.WithActiveMigration("first");

            await WithDocuments(10, migration.SourceConfig);

            await WithDocuments(5, migration.DestinationConfig);

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            var statistics = await GetStatisticsForMigration(migration);

            statistics.SourceCount.ShouldBe(10);
            statistics.DestinationCount.ShouldBe(5);
            statistics.Percentage.ShouldBe(0.5);
        }

        [Fact]
        public async Task Pause_migration_not_populating_progress()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProgressManager>();
            var migration = await _fixture.WithActiveMigration("first");

            await WithDocuments(10, migration.SourceConfig);

            await WithDocuments(5, migration.DestinationConfig);

            //when:
            migration.Pause();
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            var statistics = await GetStatisticsForMigration(migration);
            statistics.SourceCount.ShouldBe(0);
            statistics.DestinationCount.ShouldBe(0);
            statistics.Percentage.ShouldBe(double.NaN);
        }

        private async Task<Statistics> GetStatisticsForMigration(Migrator.Core.Migrations.Migration migration)
        {
            return (await _fixture.GetService<IStatisticsRepository>().Get(migration.Id)).Value;
        }

        private async Task WithDocuments(int count, CollectionConfig confg)
        {
            var client = _fixture.GetCrudDocumentClientFor(
                confg);

            for (var i = 0; i < count; i++)
            {
                await client.Add(new DocumentDummy() { Id = i.ToString() });
            }
        }

        private class DocumentDummy : IDocument
        {
            public string Id { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }

            public T GetValue<T>(string propertyName)
            {
                throw new NotImplementedException();
            }

            public string GetValueAsString(string propertyName)
            {
                throw new NotImplementedException();
            }
        }
    }
}