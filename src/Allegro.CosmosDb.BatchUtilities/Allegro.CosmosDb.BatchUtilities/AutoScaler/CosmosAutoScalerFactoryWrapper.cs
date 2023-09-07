namespace Allegro.CosmosDb.BatchUtilities;

internal class CosmosAutoScalerFactoryWrapper
{
    public string ClientName { get; }

    private readonly ICosmosAutoScalerFactory _factory;

    public CosmosAutoScalerFactoryWrapper(string clientName, ICosmosAutoScalerFactory factory)
    {
        ClientName = clientName.ToLowerInvariant();
        _factory = factory;
    }

    public ICosmosAutoScalerFactory GetFactory()
    {
        return _factory;
    }
}