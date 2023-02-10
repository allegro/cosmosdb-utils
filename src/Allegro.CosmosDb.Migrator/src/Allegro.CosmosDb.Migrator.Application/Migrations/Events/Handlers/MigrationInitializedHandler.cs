using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Events.Handlers
{
    internal sealed class MigrationInitializedHandler : IDomainEventHandler<MigrationInitialized>
    {
        private readonly IStatisticsRepository _statisticsRepository;

        public MigrationInitializedHandler(IStatisticsRepository statisticsRepository)
        {
            _statisticsRepository = statisticsRepository;
        }

        public Task HandleAsync(MigrationInitialized @event)
        {
            var statistics = Statistics.Create(@event.Migration.Id);
            statistics.Start();
            return _statisticsRepository.Add(statistics);
        }
    }
}