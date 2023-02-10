using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal class ChangeFeedProcessorFactory : IMigrationProcessorFactory
    {
        private readonly ILogger<ChangeFeedMigrationProcessor> _logger;
        private readonly CosmosDbOptions _configuration;

        public ChangeFeedProcessorFactory(
            ILogger<ChangeFeedMigrationProcessor> logger,
            CosmosDbOptions configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IMigrationProcessor Create(CancellationToken cancellationToken)
        {
            return new ChangeFeedMigrationProcessor(_logger, cancellationToken, _configuration);
        }
    }

    internal class ChangeFeedMigrationProcessor : IMigrationProcessor
    {
        private const string AppName = "CosmosMigrator";

        private readonly int maxItems = 1000;
        private readonly TimeSpan _leaseAcquireInterval = TimeSpan.FromSeconds(30);

        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private readonly CosmosClient _migrationCosmosClient;
        private readonly Database _migrationDb;
        private MigrationSnapshot? _migrationDetails;

        private ChangeFeedProcessor? _changeFeedProcessor;
        private CosmosClient? _destinationCollectionClient;
        private CosmosClient? _sourceCollectionClient;
        private Container? _containerToStoreDocuments;

        private RuConsumptionCalculator? _ruConsumptionCalculator;

        public ChangeFeedMigrationProcessor(
            ILogger<ChangeFeedMigrationProcessor> logger,
            CancellationToken cancellationToken,
            CosmosDbOptions configuration
        )
        {
            _logger = logger;
            _cancellationToken = cancellationToken;

            _migrationCosmosClient = CreateClient(configuration.ConnectionString, allowBulkExecution: false);
            _migrationDb = _migrationCosmosClient.GetDatabase(configuration.Database);
        }

        public async Task<MigrationProcessorResult> StartAsync(MigrationSnapshot migration)
        {
            _migrationDetails = migration;

            try
            {
                _sourceCollectionClient = CreateClient(_migrationDetails.SourceConfig.ConnectionString, allowBulkExecution: false);

                await VerifyAccessCollection(
                    _sourceCollectionClient,
                    _migrationDetails.SourceConfig.DbName,
                    _migrationDetails.SourceConfig.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Problem with access to monitored database");
                return MigrationProcessorResult.Failure("ERROR: Problem with access to monitored database");
            }

            try
            {
                _destinationCollectionClient = CreateClient(_migrationDetails.DestinationConfig.ConnectionString, allowBulkExecution: true);
                await VerifyAccessCollection(
                    _destinationCollectionClient,
                    _migrationDetails.DestinationConfig.DbName,
                    _migrationDetails.DestinationConfig.CollectionName);

                _containerToStoreDocuments = _destinationCollectionClient.GetContainer(
                    _migrationDetails.DestinationConfig.DbName,
                    _migrationDetails.DestinationConfig.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Problem with access to destination database");
                return MigrationProcessorResult.Failure("ERROR: Problem with access to destination database");
            }

            Container leaseContainer;

            try
            {
                leaseContainer = await GetLeaseContainer();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Problem with access to lease container");
                return MigrationProcessorResult.Failure("ERROR: Problem with access to lease container");
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                return MigrationProcessorResult.Success();
            }

            _logger.LogInformation(
                @"Run migration {Id},
                monitored collection: {MonitoredDbName}/{MonitoredCollectionName},
                destination collection: {DestDbName}/{DestCollectionName}",
                _migrationDetails.Id,
                _migrationDetails.SourceConfig.DbName,
                _migrationDetails.SourceConfig.CollectionName,
                _migrationDetails.DestinationConfig.DbName,
                _migrationDetails.DestinationConfig.CollectionName);

            var isSuccess = await RunChangeFeedHostAsync(leaseContainer);

            if (isSuccess == false)
            {
                return MigrationProcessorResult.Failure(
                    $"ERROR: ChangeFeedMigrationProcessor initialization failed for migration {_migrationDetails.Id}");
            }

            return MigrationProcessorResult.Success();
        }

        private static CosmosClient CreateClient(string connectionString, bool allowBulkExecution)
        {
            var cosmosClientOptions = new CosmosClientOptions()
            {
                Serializer = new CustomSerializer(), AllowBulkExecution = allowBulkExecution
            };

            if (connectionString.Contains("AccountEndpoint=https://localhost:8081/"))
            {
                cosmosClientOptions.HttpClientFactory = () =>
                {
                    HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                    {
#pragma warning disable MA0039
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#pragma warning restore MA0039
                    };

                    return new HttpClient(httpMessageHandler);
                };
                cosmosClientOptions.ConnectionMode = ConnectionMode.Gateway;
            }

            return new CosmosClient(
                connectionString,
                cosmosClientOptions
                );
        }

        public async Task CloseAsync()
        {
            await TryStopChangeFeedProcessor();
        }

        public async Task CompleteAsync()
        {
            await CloseAsync();
        }

        private async Task<Container> GetLeaseContainer()
        {
            const string leaseCollectionName = "migration-leases";
            return await GetContainer(leaseCollectionName);
        }

        private async Task<Container> GetContainer(string containerName)
        {
            return await _migrationDb.CreateContainerIfNotExistsAsync(
                new ContainerProperties(containerName, "/id"),
                cancellationToken: _cancellationToken);
        }

        private async Task VerifyAccessCollection(CosmosClient client, string dbName, string collectionName)
        {
            var container = client.GetContainer(dbName, collectionName);
            var containerResponse = await container.ReadContainerAsync(cancellationToken: _cancellationToken);
            if (containerResponse.Resource == null)
                throw new Exception("An attempt to read ContainerProperties failed");
        }

        private async Task<bool> RunChangeFeedHostAsync(Container leaseContainer)
        {
            try
            {
                if (_migrationDetails is null)
                {
                    throw new ArgumentException("Empty migration details", nameof(_migrationDetails));
                }

                if (_sourceCollectionClient is null)
                {
                    throw new ArgumentException("Empty source collection client", nameof(_sourceCollectionClient));
                }

                var processorName = AppName + _migrationDetails.Id;

                _cancellationToken.Register(() => TryStopChangeFeedProcessor().RunSynchronously(TaskScheduler.Current));

                _changeFeedProcessor = _sourceCollectionClient.GetContainer(
                        _migrationDetails.SourceConfig.DbName,
                        _migrationDetails.SourceConfig.CollectionName)
                    .GetChangeFeedProcessorBuilder<StreamDocumentWrapper>(processorName, ProcessChangesAsync)
                    .WithInstanceName(Environment.MachineName)
                    .WithLeaseContainer(leaseContainer)
                    .WithLeaseConfiguration(_leaseAcquireInterval)
                    .WithStartTime(_migrationDetails.StartFrom.ToUniversalTime())
                    .WithMaxItems(maxItems)
                    .Build();

                _ruConsumptionCalculator = new RuConsumptionCalculator(
                    _migrationDetails.DestinationConfig.MaxRuConsumption,
                    initialDocumentCountInBatch: _migrationDetails.DestinationConfig.MaxRuConsumption > 0 ? 1 : maxItems
                );

                await _changeFeedProcessor.StartAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ChangeFeedMigrationProcessor initialization failed for migration id {Id}",
                    _migrationDetails?.Id);
                return false;
            }
        }

        private async Task TryStopChangeFeedProcessor()
        {
            try
            {
                if (_changeFeedProcessor != null)
                {
                    await _changeFeedProcessor.StopAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ChangeFeedMigrationProcessor stop process");
            }
        }

        private async Task ProcessChangesAsync(
            IReadOnlyCollection<StreamDocumentWrapper> docs,
            CancellationToken cancellationToken)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (_ruConsumptionCalculator is null)
            {
                throw new ArgumentException("Not initialized ru consumption calc", nameof(_ruConsumptionCalculator));
            }

            var queue = new Queue<StreamDocumentWrapper>(docs);

            while (queue.Count > 0)
            {
                var toProcess = PrepareBatch(queue, _ruConsumptionCalculator);

                var bulkOperationResponse = await ExecuteBulkImport(toProcess, cancellationToken);

                _ruConsumptionCalculator.SetCurrentRuConsumption(
                    toProcess.Count,
                    bulkOperationResponse.TotalRequestUnitsConsumed);

                var failures = await TryHandleFailures(cancellationToken, toProcess, bulkOperationResponse);

                if (failures > 0)
                {
                    await CloseAsync();
                    _logger.LogError(
                        $"Not able to import data to target table. Change feed bulk import failed for some document. Retries failed. Count {failures}");
                }

                if (queue.Count > 0)
                {
                    await Task.Delay(1000, cancellationToken); // slow down upload to prevent MaxRu consumption exceeded
                }
            }
        }

        private async Task<int> TryHandleFailures(
            CancellationToken cancellationToken,
            IReadOnlyCollection<StreamDocumentWrapper> toProcess,
            BulkOperationResponse<StreamDocumentWrapper> bulkOperationResponse)
        {
            var retry = 0;
            while (bulkOperationResponse.Failures?.Count > 0 && retry < 5)
            {
                _logger.LogWarning(
                    $"Change feed bulk import failed for some document. Count {bulkOperationResponse.Failures.Count}. Retry {retry}");
                var failures = bulkOperationResponse.Failures.ToList();

                var failedDocs = toProcess.Where(
                        d => failures.Any(
                            f =>
                                f.Item1?.Id == d.Id

                            // and maybe some exception check? only throughput issue?
                        ))
                    .ToImmutableArray();

                await Task.Delay(1000, cancellationToken);

                bulkOperationResponse = await ExecuteBulkImport(failedDocs, cancellationToken);
                retry++;
            }

            return bulkOperationResponse.Failures?.Count ?? 0;
        }

        private static IReadOnlyCollection<StreamDocumentWrapper> PrepareBatch(
            Queue<StreamDocumentWrapper> queue,
            RuConsumptionCalculator ruConsumptionCalculator)
        {
            // PPTODO: we should add wraper over container or use Queue. this method has side effect and is designed poorly
            var maxDocumentsPerSec = ruConsumptionCalculator.CalculateMaxDocumentPerSecond();

            var documentToTake = maxDocumentsPerSec > queue.Count ? queue.Count : maxDocumentsPerSec;

            return queue.Dequeue(documentToTake).ToImmutableArray();
        }

        private async Task<BulkOperationResponse<T>> ExecuteBulkImport<T>(
            IReadOnlyCollection<T> docs,
            CancellationToken cancellationToken)
        {
            BulkOperations<T> bulkOperations = new(docs.Count);

            if (_containerToStoreDocuments is null)
            {
                throw new ArgumentException("Not initialized destination container", nameof(_containerToStoreDocuments));
            }

            foreach (var doc in docs)
            {
                bulkOperations.Tasks.Add(
                    _containerToStoreDocuments.UpsertItemAsync(item: doc, cancellationToken: cancellationToken)
                        .CaptureOperationResponse(doc));
            }

            var bulkOperationResponse = await bulkOperations.ExecuteAsync();
            return bulkOperationResponse;
        }

        public void Dispose()
        {
            _destinationCollectionClient?.Dispose();
            _destinationCollectionClient = null;

            _sourceCollectionClient?.Dispose();
            _sourceCollectionClient = null;

            _migrationCosmosClient.Dispose();
        }
    }
}