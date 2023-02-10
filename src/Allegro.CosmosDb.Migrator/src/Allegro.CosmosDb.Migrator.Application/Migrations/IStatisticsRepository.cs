using System.Collections.Concurrent;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations
{
    public interface IStatisticsRepository
    {
        Task Add(Statistics statistics);
        Task Update(Statistics statistics);
        Task<Option<Statistics>> Get(AggregateId id);
    }

    internal sealed class InMemoryStatisticsRepository : IStatisticsRepository
    {
        private readonly ConcurrentDictionary<string, Statistics> _repository = new();

        public Task Add(Statistics statistics)
        {
            _repository.TryAdd(statistics.MigrationId, statistics);

            return Task.CompletedTask;
        }

        public Task Update(Statistics statistics)
        {
            _repository.AddOrUpdate(statistics.MigrationId, statistics, (s, details) => statistics);
            return Task.CompletedTask;
        }

        public Task<Option<Statistics>> Get(AggregateId id)
        {
            return Task.FromResult(new Option<Statistics>(_repository[id]));
        }
    }
}