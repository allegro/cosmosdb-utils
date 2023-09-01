using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.BatchUtilities;

public interface ICosmosBatchClientProvider
{
    CosmosClient GetBatchClient(string clientName);
}