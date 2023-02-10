namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal class StreamDocumentWrapper
    {
        public StreamDocumentWrapper(string json, string id)
        {
            Json = json;
            Id = id;
        }

        public string Json { get; }
        public string Id { get; }
    }
}