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
    internal sealed class GetStatisticsHandler : IQueryHandler<GetStatistics, StatisticsDto>
    {
        private readonly Container _container;

        public GetStatisticsHandler(ICosmosStorage cosmosStorage)
        {
            _container = cosmosStorage.GetContainerForDocument<MigrationDocument>();
        }

        public async Task<StatisticsDto> HandleAsync(GetStatistics query, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = await _container.ReadItemAsync<StatisticsDocument>(
                    StatisticsDocument.BuildId(query.MigrationId),
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