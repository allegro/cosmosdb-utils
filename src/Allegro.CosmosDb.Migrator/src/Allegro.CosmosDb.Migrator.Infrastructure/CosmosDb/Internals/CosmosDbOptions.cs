namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals
{
    internal sealed class CosmosDbOptions
    {
        public string ConnectionString { get; set; } = string.Empty;

        public string Database { get; set; } = string.Empty;

        public int RequestTimeoutInSeconds { get; set; }
    }
}