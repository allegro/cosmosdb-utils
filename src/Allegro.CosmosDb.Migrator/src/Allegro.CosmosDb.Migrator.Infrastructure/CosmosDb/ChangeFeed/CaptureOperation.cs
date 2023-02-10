using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal static class CaptureOperation
    {
        public static Task<OperationResponse<T>> CaptureOperationResponse<T>(this Task<ItemResponse<T>> task, T item)
        {
            return task.ContinueWith(
                itemResponse =>
                {
                    if (itemResponse.IsCanceled)
                    {
                        return new OperationResponse<T>()
                        {
                            Item = item,
                            IsSuccessful = false,
                            RequestUnitsConsumed = 0// TODO: maybe in this case exclude from counting ru usage for batch to have better average
                        };
                    }

                    if (itemResponse.IsCompleted &&
                        itemResponse.IsFaulted == false)
                    {
                        return new OperationResponse<T>()
                        {
                            Item = item,
                            IsSuccessful = true,
                            RequestUnitsConsumed = task.Result.RequestCharge
                        };
                    }

                    var innerExceptions = itemResponse.Exception?.Flatten();
                    if (innerExceptions?.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is
                        CosmosException cosmosException)
                    {
                        return new OperationResponse<T>()
                        {
                            Item = item,
                            RequestUnitsConsumed = cosmosException.RequestCharge,
                            IsSuccessful = false,
                            CosmosException = cosmosException
                        };
                    }

                    return new OperationResponse<T>()
                    {
                        Item = item,
                        IsSuccessful = false,
                        CosmosException = innerExceptions?.InnerExceptions.FirstOrDefault()
                    };
                },
                TaskScheduler.Current);
        }
    }
}