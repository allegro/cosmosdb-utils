using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Helpers;
using Allegro.CosmosDb.Migrator.Core.Migrations.Exceptions;

namespace Allegro.CosmosDb.Migrator.Core.Migrations
{
    public class Migration : AggregateRoot
    {
        public static Migration Create(CollectionConfig sourceConfig, CollectionConfig destinationConfig)
        {
            return Create(sourceConfig, destinationConfig, startFrom: DateTime.MinValue);
        }

        public static Migration Create(CollectionConfig sourceConfig, CollectionConfig destinationConfig, DateTime startFrom)
        {
            return new Migration(
                Guid.NewGuid(),
                sourceConfig,
                destinationConfig,
                startFrom,
                completed: false,
                paused: false,
                initialized: false,
                version: string.Empty,
                reason: string.Empty);
        }

        public CollectionConfig SourceConfig { get; }

        public CollectionConfig DestinationConfig { get; }

        private DateTime StartFrom { get; }
        public bool Completed { get; private set; }
        private bool Paused { get; set; }

        private string Reason { get; set; }

        private bool Initialized { get; set; }

        private Migration(
            AggregateId id,
            CollectionConfig sourceConfig,
            CollectionConfig destinationConfig,
            DateTime startFrom,
            bool completed,
            bool paused,
            bool initialized,
            string version,
            string reason) : base(
            id,
            version)
        {
            Id = id;
            SourceConfig = sourceConfig;
            DestinationConfig = destinationConfig;
            StartFrom = startFrom;
            Completed = completed;
            Paused = paused;

            Initialized = initialized;
            Reason = reason;

            Version = version;
        }

        public void Complete(string reason = "")
        {
            if (Completed)
            {
                return;
            }

            Reason = reason;
            Completed = true;
            AddEvent(new MigrationCompleted(this));
        }

        public void Pause()
        {
            if (!IsActive)
            {
                throw new NotActiveMigrationException(Id);
            }

            Paused = true;

            AddEvent(new MigrationPaused(this));
        }

        public void Resume()
        {
            if (Completed || IsActive || !Paused)
            {
                throw new InvalidMigrationStateException(Id, nameof(Resume));
            }

            Paused = false;

            AddEvent(new MigrationResumed(this));
        }

        public bool IsActive => !(Completed || Paused) && Initialized;

        public bool NotInitialized => !Initialized;

        public void Init()
        {
            if (Initialized)
            {
                return;
            }

            Completed = false;
            Paused = false;
            Initialized = true;
            AddEvent(new MigrationInitialized(this));
        }

        public IReadOnlyCollection<IDomainEvent> FlushEvents()
        {
            var events = Events.ToImmutableArray();
            ClearEvents();

            return events;
        }

        public MigrationSnapshot ToSnapshot()
        {
            return new(Id, SourceConfig, DestinationConfig, StartFrom, Initialized,
                Completed, Paused, Reason, IsActive);
        }

        public static Migration FromSnapshot(MigrationSnapshot migrationSnapshot, string version)
        {
            return new Migration(
                migrationSnapshot.Id,
                migrationSnapshot.SourceConfig,
                migrationSnapshot.DestinationConfig,
                migrationSnapshot.StartFrom,
                completed: migrationSnapshot.Completed,
                paused: migrationSnapshot.Paused,
                initialized: migrationSnapshot.Initialized,
                version: version,
                reason: migrationSnapshot.Reason);
        }

        public void SetVersion(string version)
        {
            Version = version;
        }
    }

    public record MigrationSnapshot(
        Guid Id,
        CollectionConfig SourceConfig,
        CollectionConfig DestinationConfig,
        DateTime StartFrom,
        bool Initialized,
        bool Completed,
        bool Paused,
        string Reason,
        bool IsActive);

    public class CollectionConfig
    {
        public string ConnectionString { get; }
        public string DbName { get; }
        public string CollectionName { get; }

        /// <summary>
        /// Maximum RU consumption used by migrator.
        /// <value>0 = unlimited</value>
        /// </summary>
        public int MaxRuConsumption { get; }

        public CollectionConfig(
            string connectionString,
            string dbName,
            string collectionName,
            int maxRuConsumption = default)
        {
            connectionString.EnsureNotEmpty(nameof(connectionString));
            dbName.EnsureNotEmpty(nameof(dbName));
            collectionName.EnsureNotEmpty(nameof(collectionName));

            ConnectionString = connectionString;
            DbName = dbName;
            CollectionName = collectionName;
            MaxRuConsumption = maxRuConsumption;
        }
    }
}