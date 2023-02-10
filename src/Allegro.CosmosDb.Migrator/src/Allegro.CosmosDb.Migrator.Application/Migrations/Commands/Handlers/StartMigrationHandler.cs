using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Convey.CQRS.Commands;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Commands.Handlers
{
    internal sealed class StartMigrationHandler : ICommandHandler<StartMigration>
    {
        private readonly IMigrationRepository _migrationRepository;
        private readonly IEventProcessor _eventProcessor;

        public StartMigrationHandler(IMigrationRepository migrationRepository, IEventProcessor eventProcessor)
        {
            _migrationRepository = migrationRepository;
            _eventProcessor = eventProcessor;
        }

        public async Task HandleAsync(StartMigration command, CancellationToken cancellationToken = default)
        {
            var migration = Migration.Create(
                new CollectionConfig(
                    command.SourceConnectionString,
                    command.SourceDbName,
                    command.SourceCollectionName,
                    command.SourceMaxRuConsumption),
                new CollectionConfig(
                    command.DestinationConnectionString,
                    command.DestinationDbName,
                    command.DestinationCollectionName,
                    command.DestinationMaxRuConsumption),
                command.StartFromUtc ?? DateTime.MinValue
            );

            await _migrationRepository.Add(migration);
            await _eventProcessor.ProcessAsync(migration.FlushEvents());

            command.MigrationId = migration.Id;
        }
    }
}