using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.DocumentClient
{
    internal class CosmosDbDocumentCollectionClientFactory : IDocumentCollectionClientFactory
    {
        public IDocumentCollectionClient Create(CollectionConfig config)
        {
            return new CosmosDbDocumentCollectionClient(config);
        }
    }
}