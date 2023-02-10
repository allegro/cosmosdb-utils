using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Processor
{
    public interface IMigrationProcessorManager
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

    internal class MigrationProcessorManager : IMigrationProcessorManager
    {
        private readonly IMigrationRepository _migrationRepository;
        private readonly IMigrationProcessorFactory _migrationProcessorFactory;
        private readonly IEventProcessor _eventProcessor;
        private readonly ILogger _logger;

        private static readonly ConcurrentDictionary<string, IMigrationProcessor> MigrationProcessors =
            new();

        public MigrationProcessorManager(
            IMigrationRepository migrationRepository,
            IMigrationProcessorFactory migrationProcessorFactory,
            IEventProcessor eventProcessor,
            ILogger<IMigrationProcessorManager> logger)
        {
            _migrationRepository = migrationRepository;
            _migrationProcessorFactory = migrationProcessorFactory;
            _eventProcessor = eventProcessor;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await CloseCompleted();

            await TryStartActive(cancellationToken);
        }

        private async Task TryStartActive(CancellationToken cancellationToken)
        {
            await foreach (var activeMigration in _migrationRepository.FindActive(cancellationToken))
            {
                if (MigrationProcessors.ContainsKey(activeMigration.Id))
                {
                    continue;
                }

                try
                {
                    await StartMigrationProcessor(cancellationToken, activeMigration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while trying to start migration {activeMigration.Id}");
                }
            }
        }

        private async Task StartMigrationProcessor(CancellationToken cancellationToken, Migration activeMigration)
        {
            var migrationProcessor = _migrationProcessorFactory.Create(cancellationToken);
            MigrationProcessors.TryAdd(activeMigration.Id, migrationProcessor);

            if (activeMigration.NotInitialized)
            {
                await TryInitMigration(activeMigration); // TODO: test it
            }

            var migrationProcessorStartResult =
                await migrationProcessor.StartAsync(activeMigration.ToSnapshot());
            if (migrationProcessorStartResult.Status == false)
            {
                await CompleteMigration(activeMigration, migrationProcessorStartResult);

                MigrationProcessors.Remove(activeMigration.Id, out _);
                await migrationProcessor.CompleteAsync();
                migrationProcessor.Dispose();
            }
        }

        private async Task TryInitMigration(Migration migration)
        {
            migration.Init();
            await _migrationRepository.Update(migration);
            await _eventProcessor.ProcessAsync(migration.FlushEvents());

            // TODO: in cosmos we cant control version on our side, etag is generated by SDK/api so we need to get it back from cosmos get it back in update method
            var persistedMigration = await _migrationRepository.Get(migration.Id);
            migration.SetVersion(persistedMigration.Version);
        }

        private async Task CompleteMigration(
            Migration activeMigration,
            MigrationProcessorResult migrationProcessorStartResult)
        {
            activeMigration.Complete(migrationProcessorStartResult.Details);
            await _migrationRepository.Update(activeMigration);
            await _eventProcessor.ProcessAsync(activeMigration.FlushEvents());
        }

        private async Task CloseCompleted()
        {
            if (MigrationProcessors.Any())
            {
                foreach (var migrationId in MigrationProcessors.Keys)
                {
                    var migration = await TryFindMigrationById(migrationId);

                    if (migration is null || !migration.IsActive)
                    {
                        var migrationProcessor = MigrationProcessors[migrationId];

                        if (migration?.Completed == true)
                        {
                            await migrationProcessor.CompleteAsync();
                        }
                        else
                        {
                            await migrationProcessor.CloseAsync();
                        }

                        MigrationProcessors.Remove(migrationId, out _);
                        migrationProcessor.Dispose();
                    }
                }
            }
        }

        private async Task<Migration?> TryFindMigrationById(string migrationId)
        {
            try
            {
                return await _migrationRepository.Get(migrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An attempt to get {migrationId} failed", migrationId);
            }

            return null;
        }
    }
}