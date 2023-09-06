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

//Connection strings in these tests are fake. They do not point to existing resources, and they do not include real key
public class StartupExtensionsTests
{
    private const string ClientNameA = "ClientNameA";
    private const string ClientNameB = "ClinetNameB";

    private const string FakeClientConnectionStringA =
        "AccountEndpoint=https://clienta.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699A==;";
    private const string FakeClientConnectionStringB =
        "AccountEndpoint=https://clientb.documents.azure.com:443/;AccountKey=craKHH9vLiQtacNuQ97cZyqchs1NBL3XHw7RkkNDsyca8ZAmc7MPP4xnBO9zRBdNNeFvK4cC3EbZVrzeDf699B==;";
    private static IServiceCollection Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<BatchRequestHandler>>(NullLogger<BatchRequestHandler>.Instance);
        return services;
    }

    [Fact]
    public void WhenAddingTwoDifferentClients_ShouldRegisterProperCountOfServicesWithoutDuplications()
    {
        var services = Init();
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringA), ClientNameA);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringB), ClientNameB);

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
        var services = Init();
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringA), ClientNameA);

        var act = () => services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringA), ClientNameA);

        act.Should()
            .ThrowExactly<InvalidOperationException>("Cosmos batch client with name = 'cosmosclienta' is already registered!");
    }

    [Fact]
    public void WhenAddingClients_ShouldBeAbleToGetThemFromProviders()
    {
        var services = Init();
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringA), ClientNameA);
        services.AddCosmosBatchClient(sp => new CosmosClientBuilder(FakeClientConnectionStringB), ClientNameB);

        var serviceProvider = services.BuildServiceProvider();

        var cosmosBatchClientProvider = serviceProvider.GetRequiredService<ICosmosBatchClientProvider>();
        var factoryProvider = serviceProvider.GetRequiredService<ICosmosAutoScalerFactoryProvider>();
        var clientA = cosmosBatchClientProvider.GetBatchClient(ClientNameA);
        var clientB = cosmosBatchClientProvider.GetBatchClient(ClientNameB);
        var factoryA = factoryProvider.GetFactory(ClientNameA);
        var factoryB = factoryProvider.GetFactory(ClientNameB);
        clientA.Should().NotBe(clientB);
        factoryA.Should().NotBe(factoryB);
        clientA.Endpoint.Should().Be("https://clienta.documents.azure.com/");
        clientB.Endpoint.Should().Be("https://clientb.documents.azure.com/");
    }
}