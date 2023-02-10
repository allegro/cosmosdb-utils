using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Application.Migration
{
    public class MigrationProcessorManagerSpec
    {
        private readonly ApplicationFixture _fixture;
        private readonly IMigrationProcessor _migrationProcessorMock;

        public MigrationProcessorManagerSpec()
        {
            _fixture = new ApplicationFixture();
            _migrationProcessorMock = Substitute.For<IMigrationProcessor>();

            _fixture
                .WithMigrationProcessor(_migrationProcessorMock)
                .Build();
        }

        [Fact]
        public async Task No_active_migration()
        {
            //given:
            var sut = _fixture
                .GetService<IMigrationProcessorManager>();

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            VerifyMigrationProcessorStarted(times: 0);
        }

        [Fact]
        public async Task Successfully_started_active_migrations()
        {
            //given:
            WithStartedMigrationResult(MigrationProcessorResult.Success());

            await _fixture.WithMigration("first");
            await _fixture.WithMigration("second");

            var sut = _fixture
                .GetService<IMigrationProcessorManager>();

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            VerifyMigrationProcessorStarted(times: 2);
        }

        [Fact]
        public async Task When_migration_initialized_event_should_be_processed()
        {
            //given:
            WithStartedMigrationResult(MigrationProcessorResult.Success());

            var migration = await _fixture.WithMigration("first");

            var sut = _fixture
                .GetService<IMigrationProcessorManager>();

            //when:
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            var processedEvents = GetProcessedEvents();

            processedEvents.ShouldContain(e => e is MigrationInitialized);
        }

        [Fact]
        public async Task Successfully_closed_completed_migration()
        {
            //given:
            WithStartedMigrationResult(MigrationProcessorResult.Success());

            var migrationToComplete = await _fixture.WithMigration("first");
            await _fixture.WithMigration("second");

            var sut = _fixture
                .GetService<IMigrationProcessorManager>();
            await sut.ExecuteAsync(CancellationToken.None);

            //when:
            migrationToComplete.Complete();
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            VerifyMigrationProcessorStarted(times: 2);
            VerifyMigrationProcessorCompleted(times: 1);
            VerifyMigrationProcessorDisposed(times: 1);
        }

        [Fact]
        public async Task Successfully_pause_and_resume_migration()
        {
            //given:
            WithStartedMigrationResult(MigrationProcessorResult.Success());

            var migration = await _fixture.WithMigration("first");

            var sut = _fixture
                .GetService<IMigrationProcessorManager>();
            await sut.ExecuteAsync(CancellationToken.None);

            //when:
            migration.Pause();
            await sut.ExecuteAsync(CancellationToken.None);

            //and when:
            migration.Resume();
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            VerifyMigrationProcessorStarted(times: 2);
            VerifyMigrationProcessorStopped(times: 1);
            VerifyMigrationProcessorCompleted(times: 0);
            VerifyMigrationProcessorDisposed(times: 1);
        }

        [Fact]
        public async Task Failed_to_start_active_migrations()
        {
            //given:
            WithStartedMigrationResult(MigrationProcessorResult.Failure("message"));

            await _fixture.WithMigration("first");

            var sut = _fixture
                .GetService<IMigrationProcessorManager>();

            //when:
            await sut.ExecuteAsync(CancellationToken.None);
            await sut.ExecuteAsync(CancellationToken.None);

            //then:
            VerifyMigrationProcessorStarted(times: 1);
            VerifyMigrationProcessorCompleted(times: 1);
            VerifyMigrationProcessorDisposed(times: 1);

            var processedEvents = GetProcessedEvents();
            processedEvents.ShouldContain(e => e is MigrationCompleted);
        }

        private void WithStartedMigrationResult(MigrationProcessorResult result)
        {
            _migrationProcessorMock.StartAsync(CreateMigration()).ReturnsForAnyArgs(result);
        }

        private static MigrationSnapshot CreateMigration()
        {
            return new MigrationSnapshot(Guid.NewGuid(),
                new CollectionConfig("connstring", "dbname", "collection", 0),
                new CollectionConfig("connstring", "dbname", "collection", 0),
                DateTime.UtcNow, false, false, false, string.Empty, false);
        }

        private void VerifyMigrationProcessorStarted(int times)
        {
            _migrationProcessorMock.ReceivedWithAnyArgs(times).StartAsync(CreateMigration());
        }

        private void VerifyMigrationProcessorStopped(int times)
        {
            _migrationProcessorMock.ReceivedWithAnyArgs(times).CloseAsync();
        }

        private void VerifyMigrationProcessorCompleted(int times)
        {
            _migrationProcessorMock.ReceivedWithAnyArgs(times).CompleteAsync();
        }

        private void VerifyMigrationProcessorDisposed(int times)
        {
            _migrationProcessorMock.ReceivedWithAnyArgs(times).Dispose();
        }

        private IReadOnlyCollection<IDomainEvent> GetProcessedEvents()
        {
            return ((LogOnlyEventProcessor)_fixture
                .GetService<IEventProcessor>()).ReceivedEvents;
        }
    }
}