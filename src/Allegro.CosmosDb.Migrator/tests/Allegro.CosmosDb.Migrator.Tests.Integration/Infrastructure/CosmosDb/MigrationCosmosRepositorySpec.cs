using System;
using System.Collections.Generic;
using System.Threading;
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
    public class MigrationCosmosRepositorySpec
    {
        private readonly IMigrationRepository _migrationRepository;

        [Fact]
        public async Task Able_to_retrieve_stored_entity()
        {
            var migration = await StoreNewMigration();

            var storedMigration = await _migrationRepository.Get(migration.Id);

            storedMigration.DestinationConfig.ShouldBeEquivalentTo(migration.DestinationConfig);
            storedMigration.SourceConfig.ShouldBeEquivalentTo(migration.SourceConfig);
            storedMigration.IsActive.ShouldBe(migration.IsActive);
            storedMigration.Completed.ShouldBe(migration.Completed);
            storedMigration.NotInitialized.ShouldBe(migration.NotInitialized);
            storedMigration.Id.ShouldBe(migration.Id);

            storedMigration.Version.ShouldNotBe(migration.Version);
            storedMigration.Version.ShouldNotBeNull();
        }

        [Fact]
        public async Task Able_to_update_migration()
        {
            var migration = await StoreNewMigration();
            var storedMigration = await _migrationRepository.Get(migration.Id);
            storedMigration.Init();

            await _migrationRepository.Update(storedMigration);

            var updatedMigration = await _migrationRepository.Get(migration.Id);

            updatedMigration.IsActive.ShouldBeTrue();
            updatedMigration.Version.ShouldNotBe(storedMigration.Version);
        }

        [Fact]
        public async Task Able_to_get_all_active_migrations_from_repository()
        {
            var activeMigrationsBefore = await GetActiveMigrations();

            await StoreNewMigration();
            await CreateInitializedMigration();

            var activeMigrations = await GetActiveMigrations();

            activeMigrations.Count.ShouldBe(activeMigrationsBefore.Count + 2);
        }

        private async Task<IReadOnlyCollection<Migration>> GetActiveMigrations()
        {
            var activeMigrations = new List<Migration>();
            await foreach (var activeMigration in _migrationRepository.FindActive(CancellationToken.None))
            {
                activeMigrations.Add(activeMigration);
            }

            return activeMigrations;
        }

        private async Task CreateInitializedMigration()
        {
            var migration = await StoreNewMigration();
            var storedMigration = await _migrationRepository.Get(migration.Id);
            storedMigration.Init();
            await _migrationRepository.Update(storedMigration);
        }

        private async Task<Migration> StoreNewMigration()
        {
            var migration = Migration.Create(
                new CollectionConfig("connectionStringSrc", "dbNameSrc", "collectionNameSrc"),
                new CollectionConfig("connectionStringDst", "dbNameDst", "collectionNameDst"));
            await _migrationRepository.Add(migration);
            return migration;
        }

        public MigrationCosmosRepositorySpec(CosmosMigratorApplicationFactory<Program> factory)
        {
            factory.Server.AllowSynchronousIO = true;
            _migrationRepository = factory.Services.GetRequiredService<IMigrationRepository>();
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