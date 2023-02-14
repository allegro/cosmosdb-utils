using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Api;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Integration.Infrastructure.CosmosDb
{
    [Collection("Cosmos DB collection")]
    public class DocumentCollectionClientSpec : IDisposable
    {
        private readonly IDocumentCollectionClientFactory _documentCollectionClientFactory;
        private readonly CosmosDbFixture _cosmosDbFixture;
        private readonly CollectionConfig _testCollectionConfig;

        [Fact]
        public async Task Count_should_return_document_count_in_collection()
        {
            var expectedCount = 10;
            await AddDocuments(expectedCount);
            var documentCollectionClient = _documentCollectionClientFactory.Create(_testCollectionConfig);

            var count = await documentCollectionClient.Count();

            count.ShouldBe(expectedCount);
        }

        [Fact]
        public async Task Should_be_able_to_get_all_documents()
        {
            var expectedCount = 10;
            await AddDocuments(expectedCount);
            var documentCollectionClient = _documentCollectionClientFactory.Create(_testCollectionConfig);

            var documents = documentCollectionClient.GetAllDocuments(expectedCount);

            var count = 0;
            var pages = 0;
            await foreach (var page in documents)
            {
                count += page.Documents.Count;
                pages++;

                foreach (var document in page.Documents)
                {
                    document.Id.ShouldNotBeNull();
                }
            }

            count.ShouldBe(expectedCount);
            pages.ShouldBe(1);
        }

        [Fact]
        public async Task Should_be_able_to_get_all_documents_with_specified_properties()
        {
            var expectedCount = 1;
            await AddDocuments(expectedCount);
            var documentCollectionClient = _documentCollectionClientFactory.Create(_testCollectionConfig);

            const string customProperty = "SomeData";
            var documents = documentCollectionClient.GetAllDocuments(expectedCount, customProperty);

            var count = 0;
            var pages = 0;
            await foreach (var page in documents)
            {
                count += page.Documents.Count;
                pages++;

                foreach (var document in page.Documents)
                {
                    document.Id.ShouldNotBeNull();
                    document.GetValueAsString(customProperty).ShouldNotBeNull();
                }
            }

            count.ShouldBe(expectedCount);
            pages.ShouldBe(1);
        }

        [Fact]
        public async Task Should_be_able_to_get_all_documents_by_id()
        {
            var documentsCount = 10;
            var expectedCount = 5;
            var ids = await AddDocuments(documentsCount);
            var documentCollectionClient = _documentCollectionClientFactory.Create(_testCollectionConfig);

            var documents = documentCollectionClient.GetAllDocumentsById(documentsCount,
                ids.Select(id => id.ToString()).Take(expectedCount).ToImmutableArray());

            var count = 0;
            var pages = 0;
            await foreach (var page in documents)
            {
                count += page.Documents.Count;
                pages++;

                foreach (var document in page.Documents)
                {
                    document.Id.ShouldNotBeNull();
                }
            }

            count.ShouldBe(expectedCount);
            pages.ShouldBe(1);
        }

        private async Task<IReadOnlyCollection<Guid>> AddDocuments(int count)
        {
            var ids = new List<Guid>();
            foreach (var item in Enumerable.Range(1, count))
            {
                var id = Guid.NewGuid();
                await _cosmosDbFixture.AddDocument(new TestDocumentBase(id, "some data {item}",
                    _testCollectionConfig.CollectionName));

                ids.Add(id);
            }

            return ids;
        }

        public DocumentCollectionClientSpec(CosmosMigratorApplicationFactory<Program> factory)
        {
            factory.Server.AllowSynchronousIO = true;

            _cosmosDbFixture = factory.CosmosDbFixture!;
            _documentCollectionClientFactory =
                factory.Server.Services.GetRequiredService<IDocumentCollectionClientFactory>();

            _testCollectionConfig = _cosmosDbFixture.Config;
        }

        private class TestDocumentBase : DocumentBase
        {
            public override string ContainerName { get; }
            public override string PartitionKey => Id;
            public override string Id { get; }

            public string SomeData { get; }

            public TestDocumentBase(Guid id, string someData, string containerName)
            {
                ContainerName = containerName;

                Id = id.ToString();
                SomeData = someData;
            }
        }

        public void Dispose()
        {
            _cosmosDbFixture.DropCollection(_testCollectionConfig.CollectionName).Wait();
        }
    }
}