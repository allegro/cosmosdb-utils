using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.BatchUtilities
{
    public interface ICosmosClientProvider
    {
        CosmosClient GetRegularClient();
        CosmosClient GetBatchClient();
    }

    public class CosmosClientProvider : ICosmosClientProvider
    {
        private readonly CosmosClient _regularCosmosClient;
        private readonly CosmosClient _batchCosmosClient;

        public CosmosClientProvider(
            CosmosClient regularCosmosClient,
            CosmosClient batchCosmosClient)
        {
            _regularCosmosClient = regularCosmosClient;
            _batchCosmosClient = batchCosmosClient;
        }

        public CosmosClient GetRegularClient() => _regularCosmosClient;
        public CosmosClient GetBatchClient() => _batchCosmosClient;
    }
}