using System;

namespace AllegroPay.CosmosDb.BatchUtilities.Events
{
    public delegate void MaxRateExceededEventHandler(object sender, MaxRateExceededEventArgs e);

    public class MaxRateExceededEventArgs : EventArgs
    {
        public MaxRateExceededEventArgs(
            double maxRate,
            double accumulatedOps,
            double attemptedOpWeight,
            double delayMilliseconds)
        {
            MaxRate = maxRate;
            AccumulatedOps = accumulatedOps;
            AttemptedOpWeight = attemptedOpWeight;
            DelayMilliseconds = delayMilliseconds;
        }

        public double MaxRate { get; }
        public double AccumulatedOps { get; }
        public double AttemptedOpWeight { get; }
        public double DelayMilliseconds { get; }
    }
}