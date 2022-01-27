using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace AllegroPay.CosmosDb.ConsistencyLevelHelpers
{
    public static class ContainerExtensions
    {
        private static readonly IReadOnlyDictionary<Type, ConsistencyLevel> CustomConsistencyReadTypes;

        static ContainerExtensions()
        {
            CustomConsistencyReadTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<CustomConsistencyLevelReadAttribute>() is not null)
                .Select(t => (t, t.GetCustomAttribute<CustomConsistencyLevelReadAttribute>()!.ConsistencyLevel))
                .ToDictionary(tuple => tuple.t, tuple => tuple.ConsistencyLevel);
        }

        /// <summary>
        /// Invoke at service's startup in order to prepare <see cref="CustomConsistencyReadTypes"/> cache.
        /// </summary>
        public static void WarmUp()
        {
        }

        /// <summary>
        /// For items marked with <see cref="CustomConsistencyLevelReadAttribute"/>
        /// their declared consistency level is used. Other items are fetched using database's default consistency level.
        /// </summary>
        public static Task<ItemResponse<T>> ReadItemWithCustomConsistencyAsync<T>(
            this Container container,
            string documentId,
            PartitionKey partitionKey,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (CustomConsistencyReadTypes.TryGetValue(typeof(T), out var consistencyLevel))
            {
                if (requestOptions is not null)
                {
                    requestOptions.ConsistencyLevel = consistencyLevel;
                }
                else
                {
                    requestOptions = new ItemRequestOptions { ConsistencyLevel = consistencyLevel };
                }
            }

            return container.ReadItemAsync<T>(documentId, partitionKey, requestOptions, cancellationToken);
        }
    }
}