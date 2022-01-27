# AllegroPay.CosmosDb.ConsistencyLevelUtilities

This library contains utilities helpful in handling CosmosDb Consistency Levels.

## CustomConsistencyLevelReadAttribute

CosmosDb database has always some consistency level set (by default it's the Session Level - see more details
[here](https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels)). This attribute should be used alongside
`ContainerExtensions.ReadItemWithCustomConsistencyAsync` method to mark entities that can use a relaxed consistency
settings to optimize RU cost or read latency.\
To prevent cold start issues, please invoke `ContainerExtensions.WarmUp()` method in your program's entrypoint.\


### Example

Usage example can be found in the `AllegroPay.CosmosDb.Demo` project.

Entity definition:

```{csharp}
[CustomConsistencyLevelRead(ConsistencyLevel.Eventual)]
public class CosmosDocument
{
    // some properties
}
```

Reading the entity with lowered consistency:

```{csharp}
using AllegroPay.CosmosDb.ConsistencyLevelHelpers;

// ...
    
var item = await container.ReadItemWithCustomConsistencyAsync<CosmosDocument>(
    documentId,
    new PartitionKey(documentId),
    cancellationToken: HttpContext.RequestAborted);

}

```

## More info

More information about overriding database's consistency levels can be found [here](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/how-to-manage-consistency).
