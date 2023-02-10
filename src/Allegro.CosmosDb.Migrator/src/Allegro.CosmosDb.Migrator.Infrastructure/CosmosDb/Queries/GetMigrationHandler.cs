using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Allegro.CosmosDb.Migrator.Application.Migrations.Queries;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Convey.CQRS.Queries;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Queries
{
    internal sealed class GetMigrationHandler : IQueryHandler<GetMigration, MigrationDto>
    {
        private readonly Container _container;

        public GetMigrationHandler(ICosmosStorage cosmosStorage)
        {
            _container = cosmosStorage.GetContainerForDocument<MigrationDocument>();
        }

        public async Task<MigrationDto> HandleAsync(GetMigration query, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = await _container.ReadItemAsync<MigrationDocument>(
                    query.MigrationId.ToString(),
                    new PartitionKey(query.MigrationId.ToString()));

                return document.Resource.AsDto();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null!;
            }
        }
    }
}