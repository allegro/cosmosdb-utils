using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.BatchUtilities.Events;

namespace Allegro.CosmosDb.BatchUtilities
{
    public interface IRateLimiter
    {
        /// <summary>
        /// Executes operation with rate limiting.
        /// This method is equivalent of ExecuteWeighted with weight = 1.
        /// </summary>
        Task<T> Execute<T>(Func<Task<T>> operation) => ExecuteWeighted(operation, 1);

        /// <summary>
        /// Executes operation with rate limiting.
        /// This method is equivalent of ExecuteWeighted with weight = 1.
        /// </summary>
        Task Execute(Func<Task> operation) => ExecuteWeighted(operation, 1);

        /// <summary>
        /// Executes operation with rate limiting, given the operation weight.
        /// Consumes "weight" from the defined limit.
        /// </summary>
        Task<T> ExecuteWeighted<T>(Func<Task<T>> operation, double weight);

        /// <summary>
        /// Executes operation with rate limiting, given the operation weight.
        /// Consumes "weight" from the defined limit.
        /// </summary>
        Task ExecuteWeighted(Func<Task> operation, double weight);

        /// <summary>
        /// Executes operation with rate limiting, estimating its weight based on previous executions of similar operation.
        /// </summary>
        /// <param name="operationName">Name of the operation (it will be used to estimate weight based on previous executions)</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="weightCalculator">Callback function to calculate (post-factum) the weight of the executed operation.</param>
        Task<T> ExecuteWithEstimatedWeight<T>(
            string operationName,
            Func<Task<T>> operation,
            Func<T, double?> weightCalculator);
    }

    public interface IRateLimiterWithVariableRate : IRateLimiter
    {
        double AvgRate { get; }

        double MaxRate { get; }

        TimeSpan RateInterval { get; }

        void ChangeMaxRate(double maxRate);
    }

    /// <summary>
    /// Simple <see cref="IRateLimiter"/> implementation, allowing to define max rate during fixed time intervals.
    /// </summary>
    /// <remarks>
    /// Weight estimation in <see cref="ExecuteWithEstimatedWeight{T}"/> simply takes the max ever calculated weight of
    /// given operation. It could be refactored to receive some kind of estimation strategy for more advanced scenarios.
    /// Also first execution of each unique operationName will be synchronous until its weight is calculated, to prevent
    /// any chance of exceeding the MaxRate.
    /// </remarks>
    public class RateLimiter : IRateLimiterWithVariableRate
    {
        private readonly SemaphoreSlim _delaySemaphore = new(1, 1);
        private readonly SemaphoreSlim _logUsageSemaphore = new(1, 1);
        private readonly ConcurrentDictionary<string, double> _operationsUsage = new();

        private double _accumulatedOps;
        private DateTimeOffset _accumulatedSince = DateTimeOffset.UtcNow;
        private double _opsConsumed;
        private DateTimeOffset _opsConsumedSince = DateTimeOffset.MinValue;

        public double AvgRate { get; private set; }
        public double MaxRate { get; private set; }
        public TimeSpan RateInterval { get; }

        /// <summary>
        /// Emitted when <see cref="MaxRate"/> is exceeded.
        /// </summary>
        public event MaxRateExceededEventHandler? MaxRateExceeded;

        /// <summary>
        /// Emitted when <see cref="MaxRate"/> is changed.
        /// </summary>
        public event MaxRateChangedEventHandler? MaxRateChanged;

        /// <summary>
        /// Emitted when <see cref="AvgRate"/> is calculated.
        /// </summary>
        public event AvgRateCalculatedEventHandler? AvgRateCalculated;

        public RateLimiter(double maxRate, TimeSpan rateInterval)
        {
            MaxRate = maxRate;
            RateInterval = rateInterval;
        }

        public static RateLimiter WithMaxRps(double maxRps)
        {
            return new(maxRps, TimeSpan.FromSeconds(1));
        }

        public void ChangeMaxRate(double maxRate)
        {
            var eventArgs = new MaxRateChangedEventArgs(maxRate, MaxRate);

            MaxRate = maxRate;

            if (Math.Abs(eventArgs.PreviousMaxRate - eventArgs.NewMaxRate) > double.Epsilon)
            {
                MaxRateChanged?.Invoke(this, eventArgs);
            }
        }

        public async Task<T> ExecuteWeighted<T>(Func<Task<T>> operation, double weight)
        {
            await _delaySemaphore.WaitAsync();
            try
            {
                await WaitBeforeOperationIfNeeded(weight);
            }
            finally
            {
                _delaySemaphore.Release();
            }

            var result = await operation();

            await LogUsage(weight);

            return result;
        }

        public async Task ExecuteWeighted(Func<Task> operation, double weight)
        {
            await _delaySemaphore.WaitAsync();
            try
            {
                await WaitBeforeOperationIfNeeded(weight);
            }
            finally
            {
                _delaySemaphore.Release();
            }

            await operation();

            await LogUsage(weight);
        }

        public async Task<T> ExecuteWithEstimatedWeight<T>(
            string operationName,
            Func<Task<T>> operation,
            Func<T, double?> weightCalculator)
        {
            T result;

            await _delaySemaphore.WaitAsync();

            try
            {
                // Try get estimated weight for this operation.
                if (!_operationsUsage.TryGetValue(operationName, out var estimatedWeight))
                {
                    // If there is no estimate (first time running this operation) keep lock until estimate is saved.

                    result = await operation();

                    await CalculateWeightAndLogUsage(operationName, result, weightCalculator);

                    return result;
                }

                await WaitBeforeOperationIfNeeded(estimatedWeight);
            }
            finally
            {
                _delaySemaphore.Release();
            }

            result = await operation();

            await CalculateWeightAndLogUsage(operationName, result, weightCalculator);

            return result;
        }

        private async Task CalculateWeightAndLogUsage<T>(
            string operationName,
            T result,
            Func<T, double?> weightCalculator)
        {
            var calculatedWeight = weightCalculator(result);
            if (calculatedWeight.HasValue)
            {
                await LogUsage(calculatedWeight.Value);

                _operationsUsage.AddOrUpdate(
                    operationName,
                    calculatedWeight.Value,
                    (_, existingWeight) => Math.Max(existingWeight, calculatedWeight.Value));
            }
        }

        private async Task WaitBeforeOperationIfNeeded(double weight)
        {
            var now = DateTimeOffset.UtcNow;

            if (now - _accumulatedSince > RateInterval)
            {
                _accumulatedOps = weight;
                _accumulatedSince = now;
            }
            else if (_accumulatedOps + weight <= MaxRate || _accumulatedOps == 0)
            {
                _accumulatedOps += weight;
            }
            else
            {
                var delayMs = Math.Round(
                    RateInterval.TotalMilliseconds - (now - _accumulatedSince).TotalMilliseconds);

                MaxRateExceeded?.Invoke(this, new MaxRateExceededEventArgs(
                    MaxRate,
                    _accumulatedOps,
                    weight,
                    delayMs));

                _accumulatedOps = weight;
                _accumulatedSince = now.AddMilliseconds(delayMs);

                if (delayMs > 0)
                {
                    await Task.Delay((int)delayMs);
                }
            }
        }

        private async Task LogUsage(double weight)
        {
            await _logUsageSemaphore.WaitAsync();

            try
            {
                const int aggregationInSeconds = 10;
                var now = DateTimeOffset.UtcNow;
                var period = now - _opsConsumedSince;
                if (period < TimeSpan.FromSeconds(aggregationInSeconds))
                {
                    _opsConsumed += weight;
                }
                else
                {
                    AvgRate = _opsConsumed / period.TotalSeconds;
                    _opsConsumed = weight;
                    _opsConsumedSince = now;

                    AvgRateCalculated?.Invoke(
                        this,
                        new AvgRateCalculatedEventArgs(
                            AvgRate,
                            period));
                }
            }
            finally
            {
                _logUsageSemaphore.Release();
            }
        }
    }
}