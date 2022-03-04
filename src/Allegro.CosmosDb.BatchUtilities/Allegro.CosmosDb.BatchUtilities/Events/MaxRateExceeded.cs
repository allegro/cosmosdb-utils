using System;

namespace Allegro.CosmosDb.BatchUtilities.Events
{
    public delegate void MaxRateChangedEventHandler(object sender, MaxRateChangedEventArgs e);

    public class MaxRateChangedEventArgs : EventArgs
    {
        public MaxRateChangedEventArgs(
            double newMaxRate,
            double previousMaxRate)
        {
            NewMaxRate = newMaxRate;
            PreviousMaxRate = previousMaxRate;
        }

        public double NewMaxRate { get; }
        public double PreviousMaxRate { get; }
    }
}