using System.Threading;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities;
using AllegroPay.CosmosDb.Demo.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;

namespace AllegroPay.CosmosDb.Demo.Infrastructure
{
    public class InitializeCosmosHostedService : IHostedService
    {
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly CosmosDbConfiguration _cosmosDbConfiguration;

        public InitializeCosmosHostedService(
            ICosmosClientProvider cosmosClientProvider,
            CosmosDbConfiguration cosmosDbConfiguration)
        {
            _cosmosClientProvider = cosmosClientProvider;
            _cosmosDbConfiguration = cosmosDbConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var cosmosClient = _cosmosClientProvider.GetRegularClient();
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                _cosmosDbConfiguration.DatabaseName,
                cancellationToken: cancellationToken);

            var containerProperties = new ContainerProperties
            {
                Id = _cosmosDbConfiguration.ContainerName,
                PartitionKeyPath = "/id",
                IndexingPolicy = new IndexingPolicy
                {
                    Automatic = false,
                    IndexingMode = IndexingMode.None
                }
            };

            await database.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}