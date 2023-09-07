using System.Collections.Generic;
using System.Linq;

namespace Allegro.CosmosDb.BatchUtilities;

internal class CosmosAutoScalerFactoryProvider : ICosmosAutoScalerFactoryProvider
{
    internal Dictionary<string, CosmosAutoScalerFactoryWrapper> CosmosAutoScalerFactoryWrappers { get; }

    public CosmosAutoScalerFactoryProvider(IEnumerable<CosmosAutoScalerFactoryWrapper> wrappers)
    {
        CosmosAutoScalerFactoryWrappers = new Dictionary<string, CosmosAutoScalerFactoryWrapper>(
            wrappers.Select(p => new KeyValuePair<string, CosmosAutoScalerFactoryWrapper>(p.ClientName.ToLowerInvariant(), p)));
    }

    public ICosmosAutoScalerFactory GetFactory(string clientName)
    {
        return CosmosAutoScalerFactoryWrappers[clientName.ToLowerInvariant()].GetFactory();
    }
}