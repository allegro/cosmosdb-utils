using System;
using Allegro.CosmosDb.Migrator.Core.Entities;

namespace Allegro.CosmosDb.Migrator.Core.Migrations
{
    public class Statistics
    {
        public static Statistics Create(AggregateId migrationId)
        {
            return new Statistics(
                migrationId,
                sourceCount: default,
                destinationCount: default,
                currentInsertRatePerSeconds: default,
                startTime: DateTime.MinValue,
                endTime: DateTime.MinValue,
                lastUpdate: DateTime.MinValue,
                version: string.Empty);
        }

        public AggregateId MigrationId { get; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public DateTime LastUpdate { get; private set; }

        public long SourceCount { get; internal set; }
        public long DestinationCount { get; internal set; }
        public double CurrentInsertRatePerSeconds { get; internal set; }

        public string Version { get; }

        private Statistics(
            AggregateId migrationId,
            long sourceCount,
            long destinationCount,
            double currentInsertRatePerSeconds,
            DateTime startTime,
            DateTime endTime,
            DateTime lastUpdate,
            string version)
        {
            MigrationId = migrationId;
            SourceCount = sourceCount;
            DestinationCount = destinationCount;

            CurrentInsertRatePerSeconds = currentInsertRatePerSeconds;
            StartTime = startTime;
            EndTime = endTime;
            LastUpdate = lastUpdate;
            Version = version;
        }

        // ReSharper disable once InconsistentNaming
        private double Eta1 => (SourceCount - DestinationCount) / CurrentInsertRatePerSeconds;

        public TimeSpan Eta =>
            double.IsNaN(Eta1)
                ? TimeSpan.Zero
                : Eta1 * TimeSpan.TicksPerSecond > long.MaxValue
                    ? TimeSpan.MaxValue
                    : TimeSpan.FromSeconds(double.IsNaN(Eta1) ? 0 : Eta1);

        public double AverageInsertRatePerSeconds => DestinationCount /
                                                     ((EndTime == DateTime.MinValue ? LastUpdate : EndTime) -
                                                      StartTime).TotalSeconds;

        public double Percentage => (double)DestinationCount / SourceCount;

        public void Update(long sourceCount, long destinationCount)
        {
            var now = DateTime.UtcNow;
            var timeSpan = now - LastUpdate;
            var inserted = destinationCount - DestinationCount;

            CurrentInsertRatePerSeconds = (inserted / timeSpan.TotalMilliseconds) * 1000;
            SourceCount = sourceCount;
            DestinationCount = destinationCount;
            LastUpdate = now;
        }

        public void Start()
        {
            StartTime = DateTime.UtcNow;
        }

        public void Complete()
        {
            EndTime = DateTime.UtcNow;
        }

        public StatisticsSnapshot ToSnapshot()
        {
            return new(MigrationId, StartTime, EndTime, LastUpdate, SourceCount, DestinationCount,
                CurrentInsertRatePerSeconds, Eta, AverageInsertRatePerSeconds, Percentage);
        }

        public static Statistics FromSnapshot(StatisticsSnapshot statisticsSnapshot, string version)
        {
            return new(statisticsSnapshot.MigrationId, statisticsSnapshot.SourceCount,
                statisticsSnapshot.DestinationCount, statisticsSnapshot.CurrentInsertRatePerSeconds,
                statisticsSnapshot.StartTime, statisticsSnapshot.EndTime, statisticsSnapshot.LastUpdate, version);
        }
    }

    public record StatisticsSnapshot(
        Guid MigrationId,
        DateTime StartTime,
        DateTime EndTime,
        DateTime LastUpdate,
        long SourceCount,
        long DestinationCount,
        double CurrentInsertRatePerSeconds,
        TimeSpan Eta,
        double AverageInsertRatePerSeconds,
        double Percentage);
}