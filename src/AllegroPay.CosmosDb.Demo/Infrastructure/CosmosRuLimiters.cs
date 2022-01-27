using System;
using AllegroPay.CosmosDb.BatchUtilities;

namespace AllegroPay.CosmosDb.Demo.Infrastructure
{
    public static class CosmosRuLimiters
    {
        public static readonly RateLimiter CosmosDocumentRuLimiter = RateLimiter.WithMaxRps(100);

        static CosmosRuLimiters()
        {
            CosmosDocumentRuLimiter.MaxRateExceeded += (_, args) =>
            {
                Console.WriteLine(
                    $"{nameof(CosmosDocumentRuLimiter)} max RU ({args.MaxRate}) exceeded ({args.AccumulatedOps} + {args.AttemptedOpWeight}). Waiting {args.DelayMilliseconds} ms.");
            };

            CosmosDocumentRuLimiter.MaxRateChanged += (_, args) =>
            {
                Console.WriteLine(
                    $"{nameof(CosmosDocumentRuLimiter)} max RU changed from {args.PreviousMaxRate} to {args.NewMaxRate}.");
            };

            CosmosDocumentRuLimiter.AvgRateCalculated += (_, args) =>
            {
                Console.WriteLine(
                    $"{nameof(CosmosDocumentRuLimiter)} avg RU calculated {args.AvgRage} over {args.Period.TotalSeconds}s.");
            };
        }
    }
}