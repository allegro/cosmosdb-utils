using System;
using System.Collections.Generic;
using System.Linq;
using Allegro.CosmosDb.BatchUtilities.Configuration;
using Allegro.CosmosDb.BatchUtilities.Events;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.BatchUtilities.Extensions
{
    public static class CosmosExtensions
    {
        public static IServiceCollection AddCosmosBatchUtilitiesFromConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string configurationSection,
            Action<CosmosClientBuilder>? additionalCosmosConfiguration = null)
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

            return services.AddCosmosBatchUtilities(
                additionalCosmosConfiguration,
                registrations.ToArray());
        }

        public static IServiceCollection AddCosmosBatchUtilities(
            this IServiceCollection services,
            params BatchUtilitiesRegistration[] registrations)
        {
            return services.AddCosmosBatchUtilities(null, registrations);
        }

        public static IServiceCollection AddCosmosBatchUtilities(
            this IServiceCollection services,
            Action<CosmosClientBuilder>? additionalCosmosConfiguration,
            params BatchUtilitiesRegistration[] registrations)
        {
            return services.AddCosmosBatchUtilities(
                (builder, _) => additionalCosmosConfiguration?.Invoke(builder),
                registrations
                    .Select(x => new Func<IServiceProvider, BatchUtilitiesRegistration>(_ => x))
                    .ToArray());
        }

        public static IServiceCollection AddCosmosBatchUtilities(
            this IServiceCollection services,
            Action<CosmosClientBuilder, IServiceProvider>? additionalCosmosConfiguration,
            params Func<IServiceProvider, BatchUtilitiesRegistration>[] registrationFactories)
        {
            services.AddSingleton(sp => registrationFactories.Select(f => f(sp)));
            services.AddSingleton<ICosmosClientProvider>(
                sp =>
                {
                    var registrations = sp.GetRequiredService<IEnumerable<BatchUtilitiesRegistration>>();
                    var cosmosClient = sp.GetRequiredService<CosmosClient>();
                    var builderFactory = sp.GetRequiredService<Func<CosmosClientBuilder>>();
                    var logger = sp.GetRequiredService<ILogger<BatchRequestHandler>>();

                    var batchClientBuilder = builderFactory()
                        .AddCustomHandlers(new BatchRequestHandler(logger, registrations.ToArray()));

                    additionalCosmosConfiguration?.Invoke(batchClientBuilder, sp);

                    return new CosmosClientProvider(
                        cosmosClient,
                        batchClientBuilder.Build());
                });

            services.AddHostedService(sp => new CosmosAutoScalerInitializationHostedService(sp));
            services.AddSingleton<ICosmosAutoScalerFactory>(
                sp =>
                {
                    var registrations = sp.GetRequiredService<IEnumerable<BatchUtilitiesRegistration>>()
                        .Where(x => x.CosmosAutoScalerConfiguration?.Enabled == true);
                    var cosmosClient = sp.GetRequiredService<CosmosClient>();
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var cosmosMetricsEventHandlers = sp.GetServices<CosmosAutoScalerMetricsCalculatedEventHandler>();
                    return new CosmosAutoScalerFactory(
                        cosmosClient,
                        loggerFactory,
                        cosmosMetricsEventHandlers,
                        registrations.ToArray());
                });

            return services;
        }
    }
}