using System.Collections.Generic;

namespace Allegro.CosmosDb.Migrator.Infrastructure
{
    public static class QueueExtensions
    {
        public static IEnumerable<T> Dequeue<T>(this Queue<T> queue, int chunkSize)
        {
            for (var i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }
    }
}