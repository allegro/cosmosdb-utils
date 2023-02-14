using System;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.DTO
{
    [Contract]
    public record StatisticsDto
    {
        public string? MigrationId { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public DateTime LastUpdate { get; init; }
        public long SourceCount { get; init; }
        public long DestinationCount { get; init; }
        public double CurrentInsertRatePerSeconds { get; init; }
        public TimeSpan Eta { get; init; }
        public double AverageInsertRatePerSeconds { get; init; }
        public double Percentage { get; init; }
    }
}