# Allegro.CosmosDb.BatchUtilities

This library contains utilities for performing batch operations in Azure CosmosDb, such as rate limiting and autoscaling.

## Rate limiter

Rate limiter allows specifying precise limit of RU consumed per second on a container or a database (in case of shared throughput scenario). Use `ICosmosClientFactory.GetBatchClient()` to get CosmosClient with rate limiting turned on.

### Note on distributed scenarios

RU limiter has no distributed state. It will work in following scenarios:

- there is only one instance using RU limiter,
- if there are many instances, each of them will limit the throughput individually (for example with 4 replicas and MaxRU=10k the total MaxRU will be 40k RU/s).

More than one batch processing instance could be used if very high throughput is needed, which is not possible to achieve from one instance.

### Generic rate limiter

The RU limiter is using `RateLimiter` utility under the hood, which can also be helpful during batch processing, for example to rate limit requests outgoing to external services.

## Auto scaler

Cosmos AutoScale feature is capable of scaling container/database which is under load up to 10 times (for example between 400 and 4000 RU/s). What's more, the container/database can have the "Autoscale Max Throughput" increased up to 10 times without consequence (it can be increased to any value, but after that it can only be lowered back to 10 times less than the ever-max value, which may not be desired). This gives the opportunity to scale 100x times and go back to original throughput (for example scale to 40 000 RU/s during batch processing and then go back to 400 RU/s).

The CosmosAutoScaler utility automates this process by exposing method `ICosmosAutoScaler.ReportBatchStart` which will trigger scaling to higher throughput and automatically scaling down after batches are processed (defaults to 1 minute grace period after last `ReportBatchStart` invocation).

### Note on costs

Cost must be considered when using CosmosAutoScaler. Cosmos DB bills for the maximum throughput provisioned during each hour (full wall-clock hour ie. 12:00-13:00). High throughput can speed up batch operations, but scaling too high can lead to excessive costs - consider how fast is enough.

### Note on distributed scenarios

CosmosAutoScaler has no distributed state. It will work in following scenarios:

- there is only one instance that uses CosmosAutoScaler,
- there are many instances and each of them is doing the batch processing work (for example all instances are receiving chunks of batch processing through some kind of service bus &mdash; or any other kind of load balancing the processing work).

It will not work when there are many instances, but only some of them are doing batch processing work &mdash; in this case the idle instances will be scaling the Cosmos down.

## How to use batch utilities

### ConfigureServices

```c#
services.AddCosmosBatchUtilities(
    BatchUtilitiesRegistration.ForContainer(
        "CosmosDemo", // db name
        "Demo",       // container name
        RateLimiter.WithMaxRps(200),
        new CosmosAutoScalerConfiguration
        {
            Enabled = true,
            IdleMaxThroughput = 400,
            ProcessingMaxThroughput = 4000,
            ProcessingMaxRu = 2000,
            UseAutoscaleThroughput = false
        }));
```

or pass the settings through configuration:

```c#
services.AddCosmosBatchUtilitiesFromConfiguration(
    Configuration,
    "CosmosDemo");
```

using configuration such as:

```json
{
  "CosmosDemo": {
    "CosmosBatchUtilities": {
      "Databases": {
        "SomeSharedThroughputDatabase": {
          "MaxRu": 2000
        },
        "SomeDatabase": {
          "Containers": {
            "SomeContainer": {
              "MaxRu": 400,
              "AutoScaler": {
                "Enabled": true,
                "IdleMaxThroughput": 400,
                "ProcessingMaxThroughput": 4000,
                "ProcessingMaxRu": 2000,
                "ProvisioningMode": "Manual" 
              }
            }
          }
        }
      }
    }
  }
}
```

### Use the auto scaler and/or the RU limiter

Here is a simple example of batch processor using RU limiter and auto scaler utilities. For more detailed example take a look at Allegro.CosmosDb.Demo project.

```c#
public class SampleBatchCommandHandler
{
    private const string DatabaseName = "CosmosDemo";
    private const string ContainerName = "Demo";

    private readonly Container _container;
    private readonly ICosmosAutoScaler _autoScaler;

    public SampleBatchCommandHandler(
        ICosmosAutoScalerFactory cosmosAutoScalerFactory,
        ICosmosClientProvider cosmosClientProvider)
    {
        // get auto scaler and container using batch utilities factories
        _autoScaler = cosmosAutoScalerFactory.ForContainer(DatabaseName, ContainerName);
        _container = cosmosClientProvider.GetBatchClient().GetContainer(DatabaseName, ContainerName);
    }

    public async Task Handle(SampleBatchCommand command)
    {
        // report batch started to trigger scale-up if needed
        _autoScaler.ReportBatchStart();

        (...)
        
        // perform Cosmos operations using native SDK's Container object 
        for (var i = 0; i < command.DocumentsToGenerate; i++)
        {
            var document = new CosmosDocument { Array = array };
            tasks.Add(_container.CreateItemAsync(document, new PartitionKey(document.Id)));
        }

        // safely await all tasks without worrying about throttling
        await Task.WhenAll(tasks);
    }
}
```
