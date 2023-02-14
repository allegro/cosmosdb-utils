using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb
{
    public static class FeedIteratorExtensions
    {
        public static async IAsyncEnumerable<T> ReadAll<T>(this FeedIterator<T> iterator)
        {
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    yield return item;
                }
            }
        }
    }
}