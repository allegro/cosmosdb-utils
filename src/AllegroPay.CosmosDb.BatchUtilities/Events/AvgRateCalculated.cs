using System;

namespace AllegroPay.CosmosDb.BatchUtilities.Events
{
    public delegate void AvgRateCalculatedEventHandler(object sender, AvgRateCalculatedEventArgs e);

    public class AvgRateCalculatedEventArgs : EventArgs
    {
        public AvgRateCalculatedEventArgs(
            double avgRate,
            TimeSpan period)
        {
            AvgRage = avgRate;
            Period = period;
        }

        public double AvgRage { get; }
        public TimeSpan Period { get; }
    }
}