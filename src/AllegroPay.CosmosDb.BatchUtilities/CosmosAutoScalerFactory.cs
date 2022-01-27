using System;
using System.Collections.Generic;
using System.Linq;
using AllegroPay.CosmosDb.BatchUtilities.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AllegroPay.CosmosDb.BatchUtilities
{
    public interface ICosmosAutoScalerFactory
    {
        ICosmosAutoScaler ForContainer(
            string databaseName,
            string containerName);

        ICosmosAutoScaler ForDatabase(
            string databaseName);
    }

    public class CosmosAutoScalerFactory : ICosmosAutoScalerFactory
    {
        private readonly Dictionary<string, CosmosAutoScaler> _autoScalers;

        public CosmosAutoScalerFactory(
            CosmosClient cosmosClient,
            ILoggerFactory loggerFactory,
            params BatchUtilitiesRegistration[] batchUtilitiesRegistrations)
        {
            _autoScalers = batchUtilitiesRegistrations.ToDictionary(
                x => FormatKey(x.DatabaseName, x.ContainerName),
                x => new CosmosAutoScaler(
                    loggerFactory.CreateLogger<CosmosAutoScaler>(),
                    GetScalableObject(cosmosClient, x),
                    x.RuLimiter,
                    new CosmosBatchUtilitiesConfiguration
                    {
                        MaxRu = x.RuLimiter.MaxRate,
                        AutoScaler = x.CosmosAutoScalerConfiguration
                    }));
        }

        public ICosmosAutoScaler ForContainer(string databaseName, string containerName)
        {
            if (!_autoScalers.TryGetValue(FormatKey(databaseName, null), out var autoScaler) &&
                !_autoScalers.TryGetValue(FormatKey(databaseName, containerName), out autoScaler))
            {
                throw new ArgumentException(
                    $"No auto scaler registered for container '{containerName}' in database {databaseName}.",
                    nameof(containerName));
            }

            return autoScaler;
        }

        public ICosmosAutoScaler ForDatabase(string databaseName)
        {
            if (!_autoScalers.TryGetValue(FormatKey(databaseName, null), out var autoScaler))
            {
                throw new ArgumentException(
                    $"No auto scaler registered for database {databaseName}.",
                    nameof(databaseName));
            }

            return autoScaler;
        }

        private static string FormatKey(string databaseName, string? containerName)
        {
            return containerName == null
                ? databaseName.ToLowerInvariant()
                : $"{databaseName.ToLowerInvariant()}-{containerName.ToLowerInvariant()}";
        }

        private static ICosmosScalableObject GetScalableObject(
            CosmosClient cosmosClient,
            BatchUtilitiesRegistration registration)
        {
            return registration.IsDatabaseRegistration
                ? new CosmosScalableObjectWrapper(cosmosClient.GetDatabase(registration.DatabaseName))
                : new CosmosScalableObjectWrapper(cosmosClient.GetContainer(
                    registration.DatabaseName,
                    registration.ContainerName));
        }
    }
}