using Allegro.CosmosDb.BatchUtilities.Configuration;

namespace Allegro.CosmosDb.BatchUtilities
{
    public class BatchUtilitiesRegistration
    {
        public string DatabaseName { get; }

        public string? ContainerName { get; }

        public IRateLimiterWithVariableRate RuLimiter { get; }

        public CosmosAutoScalerConfiguration? CosmosAutoScalerConfiguration { get; }

        public bool IsDatabaseRegistration => ContainerName == null;

        private BatchUtilitiesRegistration(
            string databaseName,
            string? containerName,
            IRateLimiterWithVariableRate ruLimiter,
            CosmosAutoScalerConfiguration? cosmosAutoScalerConfiguration)
        {
            DatabaseName = databaseName;
            ContainerName = containerName;
            RuLimiter = ruLimiter;
            CosmosAutoScalerConfiguration = cosmosAutoScalerConfiguration;
        }

        public static BatchUtilitiesRegistration ForContainer(
            string databaseName,
            string containerName,
            IRateLimiterWithVariableRate ruLimiter,
            CosmosAutoScalerConfiguration? cosmosAutoScalerConfiguration = null)
        {
            return new(databaseName, containerName, ruLimiter, cosmosAutoScalerConfiguration);
        }

        public static BatchUtilitiesRegistration ForDatabase(
            string databaseName,
            IRateLimiterWithVariableRate ruLimiter,
            CosmosAutoScalerConfiguration? cosmosAutoScalerConfiguration = null)
        {
            return new(databaseName, null, ruLimiter, cosmosAutoScalerConfiguration);
        }
    }
}