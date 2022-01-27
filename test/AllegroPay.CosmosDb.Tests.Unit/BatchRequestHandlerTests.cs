using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AllegroPay.CosmosDb.Tests.Unit
{
    public class BatchRequestHandlerTests
    {
        [Fact]
        public async Task ShouldChooseCorrectContainerRuLimiter()
        {
            // arrange
            var rateLimiter = new Mock<IRateLimiterWithVariableRate>();
            var handler = CreateBatchRequestHandler(
                BatchUtilitiesRegistration.ForContainer(
                    "DbName", "OtherContainer", Mock.Of<IRateLimiterWithVariableRate>()),
                BatchUtilitiesRegistration.ForContainer(
                    "DbName", "ContainerName", rateLimiter.Object),
                BatchUtilitiesRegistration.ForContainer(
                    "OtherDb", "ContainerName", Mock.Of<IRateLimiterWithVariableRate>()));
            var message = new RequestMessage(HttpMethod.Get, new Uri("dbs/DbName/colls/ContainerName", UriKind.Relative));

            // act
            await handler.SendAsync(message, CancellationToken.None);

            // assert
            rateLimiter.Verify(x => x.ExecuteWithEstimatedWeight(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<ResponseMessage>>>(),
                    It.IsAny<Func<ResponseMessage, double?>>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldChooseCorrectDatabaseRuLimiter()
        {
            // arrange
            var rateLimiter = new Mock<IRateLimiterWithVariableRate>();
            var handler = CreateBatchRequestHandler(
                BatchUtilitiesRegistration.ForDatabase("OtherDb",  Mock.Of<IRateLimiterWithVariableRate>()),
                BatchUtilitiesRegistration.ForDatabase("DbName", rateLimiter.Object),
                BatchUtilitiesRegistration.ForContainer(
                    "DbName", "ContainerName", Mock.Of<IRateLimiterWithVariableRate>()));
            var message = new RequestMessage(HttpMethod.Get, new Uri("dbs/DbName/colls/ContainerName", UriKind.Relative));

            // act
            await handler.SendAsync(message, CancellationToken.None);

            // assert
            rateLimiter.Verify(x => x.ExecuteWithEstimatedWeight(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<ResponseMessage>>>(),
                    It.IsAny<Func<ResponseMessage, double?>>()),
                Times.Once);
        }

        private static BatchRequestHandler CreateBatchRequestHandler(params BatchUtilitiesRegistration[] batchUtilitiesRegistrations)
        {
            return new BatchRequestHandler(
                Mock.Of<ILogger<BatchRequestHandler>>(),
                batchUtilitiesRegistrations) { InnerHandler = new FakeCosmosHandler() };
        }

        private class FakeCosmosHandler : RequestHandler
        {
            public override Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ResponseMessage());
            }
        }
    }
}