using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals.Exceptions;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals
{
    internal interface ICosmosRepository<T> : ICosmosTransaction where T : ICosmosDocument
    {
        Container Container { get; }
        Task Add(T document);
        Task Update(T document);
        Task<T> Get(CosmosId cosmosId);
        Task Delete(CosmosId id);
        // TODO: maybe upsert?
    }

    internal class CosmosRepository<T> : ICosmosRepository<T> where T : ICosmosDocument
    {
        private readonly Container _container;

        private TransactionalBatch? _transactionalBatch = null;
        private bool _transactionBegan = false;
        private bool _firstAction = false;

        public CosmosRepository(ICosmosStorage cosmosStorage)
        {
            _container = cosmosStorage.GetContainerForDocument<MigrationDocument>();
        }

        public Container Container => _container;

        public Task Add(T document)
        {
            var batch = EnsureBatch(document.PartitionKey);

            batch.CreateItem(document);

            return Execute(batch);
        }

        public Task Update(T document)
        {
            var batch = EnsureBatch(document.PartitionKey);

            // TODO: handle not found
            // TODO: return at least updated version

            batch.ReplaceItem(document.Id, document, new TransactionalBatchItemRequestOptions()
            {
                IfMatchEtag = document.Etag
            });

            return Execute(batch);
        }

        public async Task<T> Get(CosmosId cosmosId)
        {
            // TODO: handle not found
            var item = await _container.ReadItemAsync<T>(cosmosId.Id, new PartitionKey(cosmosId.PartitionKey));
            return item.Resource;
        }

        public Task Delete(CosmosId cosmosId)
        {
            // TODO: handle not found
            var batch = EnsureBatch(cosmosId.PartitionKey);
            batch.DeleteItem(cosmosId.Id);
            return Execute(batch);
        }

        public void BeginTransaction()
        {
            if (_transactionBegan)
            {
                throw new CosmosTransactionAlreadyBeganException(typeof(T).Name);
            }

            _transactionBegan = true;
            _firstAction = true;
        }

        public async Task CommitTransaction()
        {
            EnsureTransactionInitialized();

            var result = await _transactionalBatch!.ExecuteAsync();

            _transactionBegan = false;
            _firstAction = false;

            if (result.IsSuccessStatusCode)
            {
                return;
            }

            throw new CosmosTransactionCommitException(typeof(T).Name, result);
        }

        public void AbortTransaction()
        {
            EnsureTransactionInitialized();

            _transactionBegan = false;
            _transactionalBatch = null;
        }

        private void EnsureTransactionInitialized()
        {
            if (!_transactionBegan)
            {
                throw new CosmosTransactionNotInitializedException(typeof(T).Name);
            }
        }

        private Task Execute(TransactionalBatch batch)
        {
            return _transactionBegan ? Task.CompletedTask : batch.ExecuteAsync();
        }

        private TransactionalBatch EnsureBatch(string partitionKey)
        {
            if (!_transactionBegan)
            {
                return _container.CreateTransactionalBatch(new PartitionKey(partitionKey));
            }

            if (_firstAction)
            {
                _transactionalBatch = _container.CreateTransactionalBatch(new PartitionKey(partitionKey));
                _firstAction = false;
            }

            return _transactionalBatch!;
        }
    }

    internal interface ICosmosTransaction
    {
        void BeginTransaction();
        Task CommitTransaction();
        void AbortTransaction();
    }
}