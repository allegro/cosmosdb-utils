using System;
using Allegro.CosmosDb.BatchUtilities.Configuration;

namespace Allegro.CosmosDb.BatchUtilities.Events
{
    public delegate void CosmosAutoScalerMetricsCalculatedEventHandler(object sender, CosmosAutoScalerMetricsCalculatedEventArgs e);

    public class CosmosAutoScalerMetricsCalculatedEventArgs : EventArgs
    {
        public CosmosAutoScalerMetricsCalculatedEventArgs(
            CosmosAutoScaler cosmosAutoScaler,
            string? databaseName,
            string? containerName,
            double limiterMaxRate,
            double limiterAvgRate,
            double maxThroughput,
            CosmosProvisioningMode provisioningMode)
        {
            CosmosAutoScaler = cosmosAutoScaler;
            DatabaseName = databaseName;
            ContainerName = containerName;
            LimiterMaxRate = limiterMaxRate;
            LimiterAvgRate = limiterAvgRate;
            MaxThroughput = maxThroughput;
            ProvisioningMode = provisioningMode;
        }

        /// <summary>
        /// <see cref="CosmosAutoScaler"/> that emitted the event.
        /// </summary>
        public CosmosAutoScaler CosmosAutoScaler { get; }

        /// <summary>
        /// Name of the database that is being auto scaled.
        /// </summary>
        public string? DatabaseName { get; }

        /// <summary>
        /// Name of the container that is being auto scaled.
        /// </summary>
        public string? ContainerName { get; }

        /// <summary>
        /// Max RU/s currently set for this <see cref="CosmosAutoScaler"/>.
        /// </summary>
        public double LimiterMaxRate { get; }

        /// <summary>
        /// Avg RU/s currently calculated for this <see cref="CosmosAutoScaler"/>.
        /// </summary>
        public double LimiterAvgRate { get; }

        /// <summary>
        /// Max RU/s configured for this <see cref="CosmosAutoScaler"/>.
        /// </summary>
        public double MaxThroughput { get; }

        /// <summary>
        /// Cosmos provisioning mode configured for this <see cref="CosmosAutoScaler"/>.
        /// </summary>
        public CosmosProvisioningMode ProvisioningMode { get; }
    }
}