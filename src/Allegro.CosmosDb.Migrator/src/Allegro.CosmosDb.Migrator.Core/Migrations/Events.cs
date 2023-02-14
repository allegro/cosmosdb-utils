using System;

namespace Allegro.CosmosDb.Migrator.Core.Migrations
{
    public class MigrationInitialized : IDomainEvent
    {
        public Migration Migration { get; }

        public DateTime TimeStamp { get; }

        public MigrationInitialized(Migration migration)
        {
            Migration = migration;
            TimeStamp = DateTime.UtcNow;
        }
    }

    public class MigrationPaused : IDomainEvent
    {
        public Migration Migration { get; }

        public DateTime TimeStamp { get; }

        public MigrationPaused(Migration migration)
        {
            Migration = migration;
            TimeStamp = DateTime.UtcNow;
        }
    }

    public class MigrationResumed : IDomainEvent
    {
        public Migration Migration { get; }

        public DateTime TimeStamp { get; }

        public MigrationResumed(Migration migration)
        {
            Migration = migration;
            TimeStamp = DateTime.UtcNow;
        }
    }

    public class MigrationCompleted : IDomainEvent
    {
        public Migration Migration { get; }

        public DateTime TimeStamp { get; }

        public MigrationCompleted(Migration migration)
        {
            Migration = migration;
            TimeStamp = DateTime.UtcNow;
        }
    }
}