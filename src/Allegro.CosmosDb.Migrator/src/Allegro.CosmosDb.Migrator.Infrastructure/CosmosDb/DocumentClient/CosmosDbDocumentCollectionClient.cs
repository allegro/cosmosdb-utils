using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.DocumentClient
{
    internal class CosmosDbDocumentCollectionClient : IDocumentCollectionClient
    {
        private const string DefaultQuery = "select c.id from c";
        private const string ExpressionTemplate = "select c.id, {0} from c";

        private readonly CosmosClient _client;
        private readonly Container _container;

        public CosmosDbDocumentCollectionClient(CollectionConfig config)
        {
            var cosmosClientOptions = new CosmosClientOptions()
            {
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(100)
            };

            // for local development
            if (config.ConnectionString.Contains("AccountEndpoint=https://localhost:8081/"))
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

            _client = new CosmosClient(config.ConnectionString, cosmosClientOptions);

            _container = _client.GetContainer(config.DbName, config.CollectionName);
        }

        public async Task<long> Count()
        {
            var options = new ContainerRequestOptions()
            {
                PopulateQuotaInfo = true
            };

            var containerResponse = await _container
                .ReadContainerAsync(options);

            return TryGetCountQuota(containerResponse);
        }

        private static long TryGetCountQuota(ContainerResponse containerResponse)
        {
            try
            {
                var resourceUsages = ParseResourceUsages(containerResponse);
                return resourceUsages["documentsCount"];
            }
            catch
            {
                return long.MinValue;
            }
        }

        private static Dictionary<string, long> ParseResourceUsages(ContainerResponse containerResponse)
        {
            var resourceUsages =
                containerResponse.Headers["x-ms-resource-usage"]
                    .Split(";")
                    .Select(usages => usages.Split("="))
                    .ToDictionary(
                        usage => usage[0],
                        usage => long.Parse(usage[1])
                    );
            return resourceUsages;
        }

        public async IAsyncEnumerable<DocumentPage> GetAllDocuments(
            int pageSize,
            string? propertiesToLoad = null,
            string? continuationToken = null)
        {
            var query = BuildQuery(propertiesToLoad);

            var queryDefinition = new QueryDefinition(query);

            var documentQuery = _container.GetItemQueryIterator<dynamic>(
                queryDefinition,
                continuationToken,
                new QueryRequestOptions()
                {
                    MaxItemCount = pageSize
                });

            while (documentQuery.HasMoreResults)
            {
                var response = await documentQuery.ReadNextAsync();

                if (response.Count == 0)
                {
                    yield break;
                }

                yield return new DocumentPage(
                    response.ToList()
                        .Select(d => new DocumentWrapper(d)).ToImmutableArray(),
                    response.ContinuationToken);
            }
        }

        public async IAsyncEnumerable<DocumentPage> GetAllDocumentsById(
            int pageSize,
            IReadOnlyCollection<string> idsToLoad,
            string? propertiesToLoad = null)
        {
            var query = BuildQuery(propertiesToLoad);

            var queryDefinition = new QueryDefinition(
                query + " where ARRAY_CONTAINS(@srcId, c.id)")
                .WithParameter("@srcId", idsToLoad);

            var documentQuery = _container.GetItemQueryIterator<dynamic>(
                queryDefinition,
                requestOptions: new QueryRequestOptions()
                {
                    MaxItemCount = pageSize
                });

            while (documentQuery.HasMoreResults)
            {
                var response = await documentQuery.ReadNextAsync();
                yield return new DocumentPage(
                    response.ToList()
                        .Select(d => new DocumentWrapper(d)).ToImmutableArray(),
                    response.ContinuationToken);
            }
        }

        private static string BuildQuery(string? hashExpression)
        {
            var query = string.IsNullOrWhiteSpace(hashExpression)
                ? DefaultQuery
                : string.Format(
                    ExpressionTemplate,
                    string.Join(
                        ',',
                        hashExpression.Split(',')
                            .Select(s => BuildParameter(s))
                    )
                );
            return query;
        }

        private static string BuildParameter(string s)
        {
            var parameter = s.Trim();

            var builder = new StringBuilder("c");

            foreach (var paramPath in parameter.Split("."))
            {
                builder.Append($"[\"{paramPath}\"]");
            }

            builder.Append("as ").Append(s.Replace(".", string.Empty));

            return builder.ToString();
        }

        // private Uri CollectionUri()
        // {
        //     return UriFactory.CreateDocumentCollectionUri(_dbName, _collection);
        // }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}