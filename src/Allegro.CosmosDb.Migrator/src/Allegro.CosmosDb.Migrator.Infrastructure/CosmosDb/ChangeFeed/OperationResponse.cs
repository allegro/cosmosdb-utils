using System;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal class OperationResponse<T>
    {
        public T? Item { get; set; }
        public double RequestUnitsConsumed { get; set; } = 0;
        public bool IsSuccessful { get; set; }
        public Exception? CosmosException { get; set; }
    }
}