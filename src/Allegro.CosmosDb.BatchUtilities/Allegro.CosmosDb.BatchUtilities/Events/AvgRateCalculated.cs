using System;

namespace Allegro.CosmosDb.BatchUtilities.Events
{
    public delegate void AvgRateCalculatedEventHandler(object sender, AvgRateCalculatedEventArgs e);

    public class AvgRateCalculatedEventArgs : EventArgs
    {
        public AvgRateCalculatedEventArgs(
            double avgRate,
            TimeSpan period)
        {
            AvgRate = avgRate;
            Period = period;
        }

        public double AvgRate { get; }
        public TimeSpan Period { get; }
    }
}