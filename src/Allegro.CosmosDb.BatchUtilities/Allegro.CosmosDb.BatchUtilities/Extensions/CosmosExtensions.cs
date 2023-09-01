using System;
using System.Collections.Generic;
using System.Linq;
using Allegro.CosmosDb.BatchUtilities.Configuration;
using Allegro.CosmosDb.BatchUtilities.Events;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.BatchUtilities.Extensions
{
    public record BatchClientServiceProviderMarker(string ClientName);

    public static class CosmosExtensions
    {
        public static IServiceCollection AddCosmosBatchClientFromConfiguration(
            this IServiceCollection services,
            Func<IServiceProvider, CosmosClientBuilder> cosmosClientBuilderFunc,
            IConfiguration configuration,
            string configurationSection,
            string clientName)
        {
            var configurations = configuration
                .GetSection(configurationSection)
                .Get<CosmosBatchUtilitiesConfigurations>();
            var registrations = new List<BatchUtilitiesRegistration>();

            foreach (var (databaseName, databaseConfiguration) in configurations.Databases)
            {
                if (!databaseConfiguration.Containers.Any())
                {
                    registrations.Add(
                        BatchUtilitiesRegistration.ForDatabase(
                            databaseName,
                            RateLimiter.WithMaxRps(databaseConfiguration.MaxRu),
                            databaseConfiguration.AutoScaler));
                    continue;
                }

                foreach (var (containerName, containerConfiguration) in databaseConfiguration.Containers)
                {
                    registrations.Add(
                        BatchUtilitiesRegistration.ForContainer(
                            databaseName,
                            containerName,
                            RateLimiter.WithMaxRps(containerConfiguration.MaxRu),
                            containerConfiguration.AutoScaler));
                }
            }

            return services.AddCosmosBatchClient(
                cosmosClientBuilderFunc,
                clientName,
                registrations.Select(p => new Func<IServiceProvider, BatchUtilitiesRegistration>(_ => p)).ToArray());
        }

        public static IServiceCollection AddCosmosBatchClient(
            this IServiceCollection services,
            Func<IServiceProvider, CosmosClientBuilder> cosmosClientBuilderFunc,
            string clientName,
            params BatchUtilitiesRegistration[] registrationFactories)
        {
            services.AddCosmosBatchClient(
                cosmosClientBuilderFunc,
                clientName,
                registrationFactories.Select(p => new Func<IServiceProvider, BatchUtilitiesRegistration>(_ => p))
                    .ToArray());

            return services;
        }

        public static IServiceCollection AddCosmosBatchClient(
            this IServiceCollection services,
            Func<IServiceProvider, CosmosClientBuilder> cosmosClientBuilderFunc,
            string clientName,
            params Func<IServiceProvider, BatchUtilitiesRegistration>[] registrationFactories)
        {
            var name = clientName.ToLowerInvariant();

            services.TryAddSingleton<ICosmosBatchClientProvider, CosmosBatchClientProvider>();
            services.TryAddSingleton<ICosmosAutoScalerFactoryProvider, CosmosAutoScalerFactoryProvider>();

            if (!services.Any(
                    p => p.ServiceType == typeof(BatchClientServiceProviderMarker) &&
                         ((BatchClientServiceProviderMarker)p.ImplementationInstance!).ClientName == name))
            {
                services.AddSingleton(new BatchClientServiceProviderMarker(name));
                services.AddSingleton<CosmosBatchClientBuilderWrapper>(
                    sp =>
                    {
                        var registrations = registrationFactories.Select(f => f(sp));
                        var logger = sp.GetRequiredService<ILogger<BatchRequestHandler>>();
                        var batchClientBuilder = cosmosClientBuilderFunc(sp)
                            .AddCustomHandlers(new BatchRequestHandler(logger, registrations.ToArray()));

                        return new CosmosBatchClientBuilderWrapper(name, batchClientBuilder);
                    });

                services.AddSingleton<CosmosAutoScalerFactoryWrapper>(
                    sp =>
                    {
                        var registrations = registrationFactories.Select(f => f(sp))
                            .Where(x => x.CosmosAutoScalerConfiguration?.Enabled == true);
                        var cosmosClientBatchProvider = sp.GetRequiredService<ICosmosBatchClientProvider>();
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        var cosmosMetricsEventHandlers =
                            sp.GetServices<CosmosAutoScalerMetricsCalculatedEventHandler>();
                        var factory = new CosmosAutoScalerFactory(
                            cosmosClientBatchProvider.GetBatchClient(name),
                            loggerFactory,
                            cosmosMetricsEventHandlers,
                            registrations.ToArray());
                        var factoryWrapper = new CosmosAutoScalerFactoryWrapper(name, factory);
                        return factoryWrapper;
                    });
            }
            else
            {
                throw new InvalidOperationException($"Cosmos batch client with name = '{name}' is already registered!");
            }

            return services;
        }
    }
}