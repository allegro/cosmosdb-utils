namespace Allegro.CosmosDb.BatchUtilities;

public interface ICosmosAutoScalerFactory
{
    ICosmosAutoScaler ForContainer(
        string databaseName,
        string containerName);

    ICosmosAutoScaler ForDatabase(
        string databaseName);
}