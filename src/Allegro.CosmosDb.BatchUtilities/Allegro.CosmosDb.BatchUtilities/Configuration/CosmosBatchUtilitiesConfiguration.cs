using System;
using System.Collections.Generic;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Allegro.CosmosDb.BatchUtilities.Configuration
{
    public enum CosmosProvisioningMode
    {
        Manual,
        AutoScale
    }

    public class CosmosBatchUtilitiesConfigurations
    {
        public Dictionary<string, CosmosBatchUtilitiesDatabaseConfiguration> Databases { get; set; } = new();
    }

    public class CosmosBatchUtilitiesDatabaseConfiguration : CosmosBatchUtilitiesConfiguration
    {
        public Dictionary<string, CosmosBatchUtilitiesConfiguration> Containers { get; set; } = new();
    }

    public class CosmosBatchUtilitiesConfiguration
    {
        /// <summary>
        /// MaxRU for RU limiter
        /// </summary>
        public double MaxRu { get; set; }

        /// <summary>
        /// Auto Scaler configuration
        /// </summary>
        public CosmosAutoScalerConfiguration? AutoScaler { get; set; }
    }

    public class CosmosAutoScalerConfiguration
    {
        /// <summary>
        /// Should register cosmos auto scaler
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Max throughput to set when not processing batches
        /// </summary>
        public int IdleMaxThroughput { get; set; }

        /// <summary>
        /// Max throughput to set when processing batches
        /// </summary>
        public int ProcessingMaxThroughput { get; set; }

        /// <summary>
        /// MaxRU for RU limiter to set when Cosmos is scaled up to ProcessingMaxThroughput
        /// </summary>
        public double ProcessingMaxRu { get; set; }

        /// <summary>
        /// Cosmos provisioning mode
        /// </summary>
        /// <remarks>
        /// Cosmos API is not capable of changing between provisioning modes. This option should be set to actual
        /// database/container provisioning mode, otherwise scaling will fail.
        /// </remarks>
        public CosmosProvisioningMode ProvisioningMode { get; set; }

        /// <summary>
        /// Grace period before scaling down, when no new batches are being reported.
        /// </summary>
        /// <remarks>
        /// This should be longer than max single batch processing time to prevent scaling down during batch processing.
        /// </remarks>
        public TimeSpan DownscaleGracePeriod { get; set; } = TimeSpan.FromMinutes(5);
    }
}