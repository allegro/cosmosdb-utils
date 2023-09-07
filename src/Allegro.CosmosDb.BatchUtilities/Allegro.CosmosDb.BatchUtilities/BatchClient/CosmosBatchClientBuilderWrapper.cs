using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Allegro.CosmosDb.BatchUtilities;

internal class CosmosBatchClientBuilderWrapper
{
    public string ClientName { get; }

    public CosmosClientBuilder Builder { get; }

    private CosmosClient? _client;

    public CosmosBatchClientBuilderWrapper(string clientName, CosmosClientBuilder builder)
    {
        ClientName = clientName.ToLowerInvariant();
        Builder = builder;
    }

    public CosmosClient GetClient()
    {
        if (_client == null)
        {
            _client = Builder.Build();
        }

        return _client;
    }
}