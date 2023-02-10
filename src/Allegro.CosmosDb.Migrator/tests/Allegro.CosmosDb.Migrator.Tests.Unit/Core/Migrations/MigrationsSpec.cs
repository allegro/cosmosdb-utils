using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Core.Migrations.Exceptions;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Core.Migrations
{
    public class MigrationsSpec
    {
        private readonly CoreFixture _coreFixture;

        public MigrationsSpec()
        {
            _coreFixture = new CoreFixture();
        }

        [Fact]
        public void Able_to_create_new_migration()
        {
            var migration = _coreFixture.CreateMigration();

            migration.ShouldNotBeNull();
            migration.IsActive.ShouldBeFalse();
            migration.FlushEvents().ShouldBeEmpty();
        }

        [Fact]
        public void Able_to_init_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();

            migration.FlushEvents().ShouldContain(@event => @event is MigrationInitialized);
            migration.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Init_initialized_migration_will_do_nothing()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();

            migration.Init();

            var events = migration.FlushEvents();
            events.ShouldContain(@event => @event is MigrationInitialized);
            events.Count.ShouldBe(1);
            migration.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Able_to_pause_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();

            migration.Pause();

            migration.FlushEvents().ShouldContain(@event => @event is MigrationPaused);
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Try_to_pause_paused_migration_will_throw_error()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.FlushEvents();

            var act = () => migration.Pause();

            migration.FlushEvents().ShouldNotContain(@event => @event is MigrationPaused);
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Able_to_resume_paused_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.FlushEvents();

            migration.Resume();

            migration.FlushEvents().ShouldContain(@event => @event is MigrationResumed);
            migration.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Resume_active_migration_will_throw_error()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.FlushEvents();

            var act = () => migration.Resume();

            act.ShouldThrow<InvalidMigrationStateException>();
            migration.FlushEvents().ShouldBeEmpty();
        }

        [Fact]
        public void Resume_not_active_migration_will_throw_error()
        {
            var migration = _coreFixture.CreateMigration();

            var act = () => migration.Resume();

            act.ShouldThrow<InvalidMigrationStateException>();
            migration.FlushEvents().ShouldBeEmpty();
        }

        [Fact]
        public void Able_to_complete_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.FlushEvents();

            migration.Complete();

            migration.FlushEvents().ShouldContain(@event => @event is MigrationCompleted);
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Complete_completed_migration_will_do_nothing()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Complete();
            migration.FlushEvents();

            migration.Complete();

            migration.FlushEvents().ShouldBeEmpty();
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Able_to_complete_paused_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.FlushEvents();

            migration.Complete();

            migration.FlushEvents().ShouldContain(@event => @event is MigrationCompleted);
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Not_able_to_resume_completed_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.Complete();
            migration.FlushEvents();

            var act = () => migration.Resume();

            act.ShouldThrow<InvalidMigrationStateException>();
            migration.FlushEvents().ShouldBeEmpty();
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Not_able_to_pause_completed_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.Complete();
            migration.FlushEvents();

            var act = () => migration.Pause();

            act.ShouldThrow<NotActiveMigrationException>();
            migration.FlushEvents().ShouldBeEmpty();
            migration.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Not_able_to_init_completed_migration()
        {
            var migration = _coreFixture.CreateMigration();
            migration.Init();
            migration.Pause();
            migration.Complete();
            migration.FlushEvents();

            migration.Init();

            migration.FlushEvents().ShouldBeEmpty();
            migration.IsActive.ShouldBeFalse();
        }
    }
}