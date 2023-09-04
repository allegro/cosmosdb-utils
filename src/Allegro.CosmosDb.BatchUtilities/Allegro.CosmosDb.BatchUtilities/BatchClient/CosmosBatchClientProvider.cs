using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.BatchUtilities
{
    internal class CosmosBatchClientProvider : ICosmosBatchClientProvider
    {
        internal ConcurrentDictionary<string, CosmosBatchClientBuilderWrapper> CosmosClientBuilders { get; }

        public CosmosBatchClientProvider(IEnumerable<CosmosBatchClientBuilderWrapper> wrappers)
        {
            CosmosClientBuilders = new ConcurrentDictionary<string, CosmosBatchClientBuilderWrapper>(
                wrappers.Select(p => new KeyValuePair<string, CosmosBatchClientBuilderWrapper>(p.ClientName.ToLowerInvariant(), p)));
        }

        public CosmosClient GetBatchClient(string clientName)
        {
            return CosmosClientBuilders[clientName.ToLowerInvariant()].GetClient();
        }
    }
}