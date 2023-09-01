namespace Allegro.CosmosDb.BatchUtilities;

public interface ICosmosAutoScalerFactoryProvider
{
    public ICosmosAutoScalerFactory GetFactory(
        string clientName);
}