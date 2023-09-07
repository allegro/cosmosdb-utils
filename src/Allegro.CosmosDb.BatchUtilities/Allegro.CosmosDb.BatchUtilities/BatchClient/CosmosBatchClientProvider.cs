using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.BatchUtilities
{
    internal class CosmosBatchClientProvider : ICosmosBatchClientProvider
    {
        internal Dictionary<string, CosmosBatchClientBuilderWrapper> CosmosClientBuilders { get; }

        public CosmosBatchClientProvider(IEnumerable<CosmosBatchClientBuilderWrapper> wrappers)
        {
            CosmosClientBuilders = new Dictionary<string, CosmosBatchClientBuilderWrapper>(
                wrappers.Select(p => new KeyValuePair<string, CosmosBatchClientBuilderWrapper>(p.ClientName.ToLowerInvariant(), p)));
        }

        public CosmosClient GetBatchClient(string clientName)
        {
            return CosmosClientBuilders[clientName.ToLowerInvariant()].GetClient();
        }
    }
}