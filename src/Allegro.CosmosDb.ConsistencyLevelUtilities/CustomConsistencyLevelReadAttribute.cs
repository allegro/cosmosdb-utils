using System;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.ConsistencyLevelUtilities
{
    /// <summary><para>
    /// Consistency can only be <b>relaxed</b> at the SDK instance or request level.
    /// If you set custom level higher than the current database consistency, you will receive a BadRequest response.
    /// </para></summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CustomConsistencyLevelReadAttribute : Attribute
    {
        internal ConsistencyLevel ConsistencyLevel { get; }

        public CustomConsistencyLevelReadAttribute(ConsistencyLevel consistencyLevel) =>
            ConsistencyLevel = consistencyLevel;
    }
}