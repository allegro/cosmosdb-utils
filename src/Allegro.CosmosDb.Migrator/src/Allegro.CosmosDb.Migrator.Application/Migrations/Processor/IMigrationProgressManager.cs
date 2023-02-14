using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Processor
{
    public interface IMigrationProgressManager
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

    internal class MigrationProgressManager : IMigrationProgressManager
    {
        private readonly IMigrationRepository _migrationRepository;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly IDocumentCollectionClientFactory _documentCollectionClientFactory;
        private readonly ILogger _logger;

        public MigrationProgressManager(
            IMigrationRepository migrationRepository,
            IStatisticsRepository statisticsRepository,
            IDocumentCollectionClientFactory documentCollectionClientFactory,
            ILogger<IMigrationProgressManager> logger)
        {
            _migrationRepository = migrationRepository;
            _statisticsRepository = statisticsRepository;
            _documentCollectionClientFactory = documentCollectionClientFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await foreach (var activeMigration in _migrationRepository.FindActive(cancellationToken))
            {
                if (activeMigration.NotInitialized)
                {
                    continue;
                }

                await TrackMigrationProgressAsync(activeMigration, cancellationToken);
            }
        }

        private async Task TrackMigrationProgressAsync(
            Migration migration,
            CancellationToken cancellationToken)
        {
            try
            {
                await TrackProgressAsync(migration, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An attempt to get active migration failed");
            }
        }

        private async Task TrackProgressAsync(
            Migration migration,
            CancellationToken cancellationToken)
        {
            using var srcDocumentCollectionClient = _documentCollectionClientFactory.Create(
                migration.SourceConfig);

            using var dstDocumentCollectionClient = _documentCollectionClientFactory.Create(
                migration.DestinationConfig);

            var statisticTask = _statisticsRepository.Get(migration.Id);
            var srcCountTask = srcDocumentCollectionClient.Count();
            var dstCountTask = dstDocumentCollectionClient.Count();
            await Task.WhenAll(srcCountTask, dstCountTask, statisticTask);

            var statisticsOption = await statisticTask;

            if (statisticsOption.IsNull)
            {
                // TODO: what in this case?

                return;
            }

            var statistics = statisticsOption.Value;
            statistics.Update(await srcCountTask, await dstCountTask);

            await _statisticsRepository.Update(statistics);
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
        private readonly struct Progress
        {
            public Progress(
                double currentRate,
                double averageRate,
                double eta,
                double currentPercentage,
                long sourceCollectionCount,
                long currentDestinationCollectionCount)
            {
                CurrentRate = currentRate;
                AverageRate = averageRate;
                Eta = eta;
                CurrentPercentage = currentPercentage;
                SourceCollectionCount = sourceCollectionCount;
                CurrentDestinationCollectionCount = currentDestinationCollectionCount;
            }

            public double CurrentRate { get; }
            public double AverageRate { get; }
            public double Eta { get; }
            public double CurrentPercentage { get; }
            public long SourceCollectionCount { get; }
            public long CurrentDestinationCollectionCount { get; }
        }
    }
}