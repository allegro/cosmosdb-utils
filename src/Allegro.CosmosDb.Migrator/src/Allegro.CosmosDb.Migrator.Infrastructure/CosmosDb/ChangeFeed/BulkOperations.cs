using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal class BulkOperations<T>
    {
        public readonly List<Task<OperationResponse<T>>> Tasks;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public BulkOperations(int operationCount)
        {
            Tasks = new List<Task<OperationResponse<T>>>(operationCount);
        }

        public async Task<BulkOperationResponse<T>> ExecuteAsync()
        {
            await Task.WhenAll(Tasks);
            var tasksResults = new List<OperationResponse<T>>();

            foreach (var task in Tasks)
            {
                tasksResults.Add(await task);
            }

            _stopwatch.Stop();
            return new BulkOperationResponse<T>()
            {
                TotalTimeTaken = _stopwatch.Elapsed,
                TotalRequestUnitsConsumed = tasksResults.Sum(task => task.RequestUnitsConsumed),
                SuccessfulDocuments = tasksResults.Count(task => task.IsSuccessful),
                Failures = tasksResults.Where(task => !task.IsSuccessful)
                    .Select(task => (task.Item, task.CosmosException)).ToList()
            };
        }
    }
}