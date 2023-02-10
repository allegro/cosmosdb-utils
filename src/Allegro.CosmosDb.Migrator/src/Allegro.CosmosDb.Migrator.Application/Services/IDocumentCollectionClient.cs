using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Services
{
    public interface IDocumentCollectionClient : IDisposable
    {
        Task<long> Count();
        IAsyncEnumerable<DocumentPage> GetAllDocuments(int pageSize, string? propertiesToLoad = null, string? continuationToken = null);

        IAsyncEnumerable<DocumentPage> GetAllDocumentsById(
            int pageSize,
            IReadOnlyCollection<string> idsToLoad,
            string? propertiesToLoad = null);
    }

    public class DocumentPage
    {
        public DocumentPage(IReadOnlyCollection<IDocument> documents, string continuationToken)
        {
            Documents = documents;
            ContinuationToken = continuationToken;
        }

        public IReadOnlyCollection<IDocument> Documents { get; }
        public string ContinuationToken { get; }
    }

    public interface IDocument
    {
        string Id { get; }
        DateTime Timestamp { get; }
        T? GetValue<T>(string propertyName);
        string GetValueAsString(string propertyName);
    }

    public interface IDocumentCollectionClientFactory
    {
        IDocumentCollectionClient Create(CollectionConfig config);
    }

    #region Testing purposes

    internal interface IDocumentCollectionCrudClient
    {
        Task Add(IDocument document);
    }

    internal sealed class InMemoryDocumentCollectionClientFactory : IDocumentCollectionClientFactory
    {
        private readonly ConcurrentDictionary<string, IDocumentCollectionClient> _clients =
            new();

        public IDocumentCollectionClient Create(CollectionConfig config)
        {
            return _clients.GetOrAdd(config.ConnectionString + config.DbName + config.CollectionName, new InMemoryDocumentCollectionClient());
        }
    }

    internal sealed class InMemoryDocumentCollectionClient : IDocumentCollectionClient, IDocumentCollectionCrudClient
    {
        private readonly ConcurrentDictionary<string, IDocument> _repository =
            new();

        public Task<long> Count()
        {
            return Task.FromResult((long)_repository.Count);
        }

        public IAsyncEnumerable<DocumentPage> GetAllDocuments(
            int pageSize,
            string? propertiesToLoad = null,
            string? continuationToken = null)
        {
            return GetDocuments(pageSize, propertiesToLoad, continuationToken, AllRepositoryValuesSelector);
        }

        private static async IAsyncEnumerable<DocumentPage> GetDocuments(
            int pageSize,
            string? propertiesToLoad,
            string? continuationToken,
            Func<ICollection<IDocument>> valuesSelector)
        {
            var pageDocuments = new List<IDocument>();
            var page = 1;
            foreach (var document in valuesSelector())
            {
                pageDocuments.Add(document);

                if (pageDocuments.Count() == pageSize)
                {
                    yield return new DocumentPage(pageDocuments.ToImmutableArray(), page.ToString());

                    await Task.Delay(1);
                    page++;
                    pageDocuments.Clear();
                }
            }

            yield return new DocumentPage(pageDocuments.ToImmutableArray(), page.ToString());
        }

        private ICollection<IDocument> AllRepositoryValuesSelector()
        {
            return _repository.Values;
        }

        public IAsyncEnumerable<DocumentPage> GetAllDocumentsById(
            int pageSize,
            IReadOnlyCollection<string> idsToLoad,
            string? propertiesToLoad = null)
        {
            return GetDocuments(
                pageSize,
                propertiesToLoad,
                string.Empty,
                () => FilteredByIdValuesSelector(idsToLoad));
        }

        private ICollection<IDocument> FilteredByIdValuesSelector(IReadOnlyCollection<string> idsToLoad)
        {
            return _repository.Values.Where(doc => idsToLoad.Contains(doc.Id)).ToImmutableArray();
        }

        public Task Add(IDocument document)
        {
            _repository.TryAdd(document.Id, document);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    #endregion
}