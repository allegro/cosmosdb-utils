using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Events.Handlers
{
    internal sealed class MigrationCompletedHandler : IDomainEventHandler<MigrationCompleted>
    {
        private readonly IStatisticsRepository _statisticsRepository;

        public MigrationCompletedHandler(IStatisticsRepository statisticsRepository)
        {
            _statisticsRepository = statisticsRepository;
        }

        public async Task HandleAsync(MigrationCompleted @event)
        {
            var statisticsOption = await _statisticsRepository.Get(@event.Migration.Id);

            if (statisticsOption.IsNull)
            {
                return;
            }

            var statistics = statisticsOption.Value;
            statistics.Complete();
            await _statisticsRepository.Update(statistics);
        }
    }
}