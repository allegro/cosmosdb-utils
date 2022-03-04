using System;
using Allegro.CosmosDb.ConsistencyLevelUtilities;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Allegro.CosmosDb.Demo.Entities
{
    [CustomConsistencyLevelRead(ConsistencyLevel.Eventual)]
    public class CosmosDocument
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; } = Guid.NewGuid().ToString();

        public WeatherForecast[] Array { get; set; } = System.Array.Empty<WeatherForecast>();
    }
}