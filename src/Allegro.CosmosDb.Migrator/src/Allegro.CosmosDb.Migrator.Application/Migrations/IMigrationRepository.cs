using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations
{
    public interface IMigrationRepository
    {
        Task Add(Migration migration);
        Task Update(Migration migration);
        Task<Migration> Get(AggregateId id);
        IAsyncEnumerable<Migration> FindActive(CancellationToken cancellationToken);
    }

    internal sealed class InMemoryMigrationRepository : IMigrationRepository
    {
        private readonly ConcurrentDictionary<string, Migration> _repository = new();

        public Task Add(Migration migration)
        {
            _repository.TryAdd(migration.Id, migration);

            return Task.CompletedTask;
        }

        public Task Update(Migration migration)
        {
            _repository.AddOrUpdate(migration.Id, migration, (s, details) => migration);
            return Task.CompletedTask;
        }

        public Task<Migration> Get(AggregateId id)
        {
            return Task.FromResult(_repository[id]);
        }

        public async IAsyncEnumerable<Migration> FindActive([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var migrations = _repository.Values
                .Where(m => m.IsActive || m.NotInitialized);

            foreach (var migration in migrations)
            {
                yield return migration;
            }

            await Task.CompletedTask;
        }
    }
}