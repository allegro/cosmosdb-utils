namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals
{
    internal interface ICosmosDocument
    {
        public string Id { get; }
        public string PartitionKey { get; }
        public string EntityType { get; }
        public string Etag { get; }
    }

    internal class CosmosId
    {
        public string Id { get; }

        public string PartitionKey { get; }

        public CosmosId(string id, string partitionKey)
        {
            Id = id;
            PartitionKey = partitionKey;
        }
    }
}