using System;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities.Configuration;
using Microsoft.Azure.Cosmos;

namespace AllegroPay.CosmosDb.BatchUtilities
{
    /// <summary>
    /// Interface representing Cosmos resource that is scalable, such as a container or a database.
    /// </summary>
    public interface ICosmosScalableObject
    {
        Task Scale(int targetThroughput, CosmosProvisioningMode provisioningMode);
        Task<ThroughputResponse> GetCurrentThroughput();
    }

    public class CosmosScalableObjectWrapper : ICosmosScalableObject
    {
        private readonly Database? _database;
        private readonly Container? _container;

        public CosmosScalableObjectWrapper(Database database)
        {
            _database = database;
        }

        public CosmosScalableObjectWrapper(Container container)
        {
            _container = container;
        }

        public Task Scale(int targetThroughput, CosmosProvisioningMode provisioningMode)
        {
            var throughputProperties = provisioningMode switch
            {
                CosmosProvisioningMode.Manual =>
                    ThroughputProperties.CreateManualThroughput(targetThroughput),
                CosmosProvisioningMode.AutoScale =>
                    ThroughputProperties.CreateAutoscaleThroughput(targetThroughput),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(provisioningMode),
                    provisioningMode,
                    "Unknown provisioning mode")
            };

            return _database != null
                ? _database.ReplaceThroughputAsync(throughputProperties)
                : _container!.ReplaceThroughputAsync(throughputProperties);
        }

        public Task<ThroughputResponse> GetCurrentThroughput()
        {
            return _database != null
                ? _database.ReadThroughputAsync(new RequestOptions())
                : _container!.ReadThroughputAsync(new RequestOptions());
        }
    }
}