using System;
using System.Threading.Tasks;
using Allegro.CosmosDb.BatchUtilities.Configuration;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.BatchUtilities
{
    /// <summary>
    /// Interface representing Cosmos resource that is scalable, such as a container or a database.
    /// </summary>
    public interface ICosmosScalableObject
    {
        string? ContainerName { get; }
        string? DatabaseName { get; }

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

        public string? ContainerName => _container?.Id;
        public string? DatabaseName => _database?.Id ?? _container?.Database?.Id;

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