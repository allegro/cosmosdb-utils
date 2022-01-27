using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities;
using AllegroPay.CosmosDb.ConsistencyLevelHelpers;
using AllegroPay.CosmosDb.Demo.Configuration;
using AllegroPay.CosmosDb.Demo.Entities;
using AllegroPay.CosmosDb.Demo.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AllegroPay.CosmosDb.Demo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<SampleController> _logger;
        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly ICosmosAutoScalerFactory _cosmosAutoScalerFactory;
        private readonly CosmosDbConfiguration _cosmosDbConfiguration;

        public SampleController(
            ILogger<SampleController> logger,
            ICosmosClientProvider cosmosClientProvider,
            ICosmosAutoScalerFactory cosmosAutoScalerFactory,
            CosmosDbConfiguration cosmosDbConfiguration)
        {
            _logger = logger;
            _cosmosClientProvider = cosmosClientProvider;
            _cosmosAutoScalerFactory = cosmosAutoScalerFactory;
            _cosmosDbConfiguration = cosmosDbConfiguration;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var cosmosClient = _cosmosClientProvider.GetRegularClient();
            var rng = new Random();
            var array = Enumerable.Range(1, 5).Select(
                    index => new WeatherForecast
                    {
                        Date = DateTime.Now.AddDays(index),
                        TemperatureC = rng.Next(-20, 55),
                        Summary = Summaries[rng.Next(Summaries.Length)]
                    })
                .ToArray();
            var document = new CosmosDocument { Array = array };
            var database = cosmosClient.GetDatabase(_cosmosDbConfiguration.DatabaseName);
            var container = database.GetContainer(_cosmosDbConfiguration.ContainerName);
            await container.CreateItemAsync(
                document,
                new PartitionKey(document.Id),
                cancellationToken: HttpContext.RequestAborted);

            return array;
        }

        [HttpGet("batch")]
        public async Task<IActionResult> SendBatch(
            [FromQuery] int documentsToGenerate = 5000)
        {
            var autoScaler = _cosmosAutoScalerFactory.ForContainer(
                _cosmosDbConfiguration.DatabaseName,
                _cosmosDbConfiguration.ContainerName);
            var container = _cosmosClientProvider.GetBatchClient().GetContainer(
                _cosmosDbConfiguration.DatabaseName,
                _cosmosDbConfiguration.ContainerName);

            autoScaler.ReportBatchStart();

            var rng = new Random();
            var array = Enumerable.Range(1, 5).Select(
                    index => new WeatherForecast
                    {
                        Date = DateTime.Now.AddDays(index),
                        TemperatureC = rng.Next(-20, 55),
                        Summary = Summaries[rng.Next(Summaries.Length)]
                    })
                .ToArray();

            var tasks = new List<Task<ItemResponse<CosmosDocument>>>();
            for (var i = 0; i < documentsToGenerate; i++)
            {
                var document = new CosmosDocument { Array = array };
                tasks.Add(container.CreateItemAsync(
                    document,
                    new PartitionKey(document.Id),
                    cancellationToken: HttpContext.RequestAborted));
            }

            var response = await Task.WhenAll(tasks);

            _logger.LogInformation(
                "{Limiter} avg RU: {AvgRate}",
                nameof(CosmosRuLimiters.CosmosDocumentRuLimiter),
                CosmosRuLimiters.CosmosDocumentRuLimiter.AvgRate);

            return Ok(response.Select(x => x.Resource));
        }

        [HttpGet("custom-consistency/{documentId}")]
        public async Task<IActionResult> GetWithCustomConsistency(string documentId)
        {
            var cosmosClient = _cosmosClientProvider.GetRegularClient();
            var database = cosmosClient.GetDatabase(_cosmosDbConfiguration.DatabaseName);
            var container = database.GetContainer(_cosmosDbConfiguration.ContainerName);

            var item = await container.ReadItemWithCustomConsistencyAsync<CosmosDocument>(
                documentId,
                new PartitionKey(documentId),
                cancellationToken: HttpContext.RequestAborted);

            return Ok(item);
        }
    }
}