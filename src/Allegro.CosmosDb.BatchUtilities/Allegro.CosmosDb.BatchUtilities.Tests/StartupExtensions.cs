using System;
using Allegro.CosmosDb.BatchUtilities;
using Allegro.CosmosDb.BatchUtilities.Extensions;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Allegro.CosmosDb.Tests.Unit;

public class StartupExtensions
{
    private const string ClientNameA = "ClientNameA";
    private const string ClientNameB = "ClinetNameB";

    [Fact]
    public void WhenAddingTwoDifferentClients_ShouldRegisterProperCountOfServicesWithoutDuplications()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<BatchRequestHandler>>(NullLogger<BatchRequestHandler>.Instance);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clienta.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699A==;"), ClientNameA);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clientb.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699B==;"), ClientNameB);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetServices<ICosmosBatchClientProvider>().Should().HaveCount(1);
        serviceProvider.GetServices<ICosmosAutoScalerFactoryProvider>().Should().HaveCount(1);
        serviceProvider.GetServices<BatchClientServiceProviderMarker>().Should()
            .ContainSingle(p => p.ClientName == ClientNameA.ToLowerInvariant());
        serviceProvider.GetServices<BatchClientServiceProviderMarker>().Should()
            .ContainSingle(p => p.ClientName == ClientNameB.ToLowerInvariant());
        serviceProvider.GetServices<CosmosBatchClientBuilderWrapper>().Should().HaveCount(2);
        serviceProvider.GetServices<CosmosAutoScalerFactoryWrapper>().Should().HaveCount(2);
    }

    [Fact]
    public void WhenAddingTwoSameClients_ShouldThrowException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<BatchRequestHandler>>(NullLogger<BatchRequestHandler>.Instance);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clienta.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699A==;"), ClientNameA);

        var act = () => services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clienta.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699A==;"), ClientNameA);

        act.Should()
            .ThrowExactly<InvalidOperationException>("Cosmos batch client with name = 'cosmosclienta' is already registered!");
    }

    [Fact]
    public void WhenAddingClients_ShouldBeAbleToGetThemFromProviders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<BatchRequestHandler>>(NullLogger<BatchRequestHandler>.Instance);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clienta.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699A==;"), ClientNameA);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder("AccountEndpoint=https://clientb.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699B==;"), ClientNameB);

        var serviceProvider = services.BuildServiceProvider();

        var cosmosBatchClientProvider = serviceProvider.GetRequiredService<ICosmosBatchClientProvider>();
        var factoryProvider = serviceProvider.GetRequiredService<ICosmosAutoScalerFactoryProvider>();
        var clientA = cosmosBatchClientProvider.GetBatchClient(ClientNameA);
        var clientB = cosmosBatchClientProvider.GetBatchClient(ClientNameB);
        var factoryA = factoryProvider.GetFactory(ClientNameA);
        var factoryB = factoryProvider.GetFactory(ClientNameB);
        clientA.Should().NotBe(clientB);
        factoryA.Should().NotBe(factoryB);
    }
}