using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.Exceptions;
using Allegro.CosmosDb.Migrator.Application.Services;
using Convey.CQRS.Commands;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Commands.Handlers
{
    internal sealed class CompleteMigrationHandler : ICommandHandler<CompleteMigration>
    {
        private readonly IMigrationRepository _migrationRepository;
        private readonly IEventProcessor _eventProcessor;

        public CompleteMigrationHandler(IMigrationRepository migrationRepository, IEventProcessor eventProcessor)
        {
            _migrationRepository = migrationRepository;
            _eventProcessor = eventProcessor;
        }

        public async Task HandleAsync(CompleteMigration command, CancellationToken cancellationToken = default)
        {
            var migration = await _migrationRepository.Get(command.MigrationId);

            if (migration is null)
            {
                throw new MigrationNotExists(command.MigrationId);
            }

            migration.Complete();

            await _migrationRepository.Update(migration);
            await _eventProcessor.ProcessAsync(migration.FlushEvents());
        }
    }
}