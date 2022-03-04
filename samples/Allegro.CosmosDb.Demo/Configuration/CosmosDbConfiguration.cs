namespace Allegro.CosmosDb.Demo.Configuration
{
    public record CosmosDbConfiguration(
        string EndpointUri,
        string Key,
        string DatabaseName,
        string ContainerName)
    {
        // constructor for configuration binder
        public CosmosDbConfiguration()
            : this(default!, default!, default!, default!)
        {
        }
    }
}