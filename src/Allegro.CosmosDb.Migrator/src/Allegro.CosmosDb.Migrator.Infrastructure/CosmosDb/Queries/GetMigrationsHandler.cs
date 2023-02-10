using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Allegro.CosmosDb.Migrator.Application.Migrations.Queries;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Convey.CQRS.Queries;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Queries
{
    internal sealed class GetMigrationsHandler : IQueryHandler<GetMigrations, IEnumerable<MigrationDto>>
    {
        private readonly Container _container;

        public GetMigrationsHandler(ICosmosStorage cosmosStorage)
        {
            _container = cosmosStorage.GetContainerForDocument<MigrationDocument>();
        }

        public async Task<IEnumerable<MigrationDto>> HandleAsync(GetMigrations query, CancellationToken cancellationToken = default)
        {
            var documents = _container.GetItemLinqQueryable<MigrationDocument>(requestOptions: new QueryRequestOptions()
            {
                MaxItemCount = 100
            })
                .Where(m => m.EntityType == nameof(MigrationDocument))
                .ToFeedIterator()
                .ReadAll();

            var items = new List<MigrationDto>();

            // PPTODO: support paging with continuation token
            await foreach (var document in documents)
            {
                items.Add(document.AsDto());
            }

            return items;
        }
    }
}