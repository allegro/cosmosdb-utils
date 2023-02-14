using System;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.DTO
{
    [Contract]
    public class MigrationDto
    {
        public string Id { get; }
        public CollectionConfig SourceConfig { get; }
        public CollectionConfig DestinationConfig { get; }
        public DateTime StartFrom { get; }
        public bool Initialized { get; }
        public bool Completed { get; }
        public bool Paused { get; }
        public string Version { get; }

        public MigrationDto(string id, CollectionConfig sourceConfig, CollectionConfig destinationConfig, DateTime startFrom, bool initialized, bool completed, bool paused, string version)
        {
            Id = id;
            SourceConfig = sourceConfig;
            DestinationConfig = destinationConfig;
            StartFrom = startFrom;
            Initialized = initialized;
            Completed = completed;
            Paused = paused;
            Version = version;
        }
    }
}