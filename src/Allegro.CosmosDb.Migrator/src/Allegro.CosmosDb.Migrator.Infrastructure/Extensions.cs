using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Allegro.CosmosDb.Migrator.Application;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Infrastructure.BackgroundServices;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.DocumentClient;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Allegro.CosmosDb.Migrator.Infrastructure.Exceptions;
using Allegro.CosmosDb.Migrator.Infrastructure.Services;
using Convey;
using Convey.CQRS.Queries;
using Convey.Docs.Swagger;
using Convey.WebApi;
using Convey.WebApi.CQRS;
using Convey.WebApi.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: InternalsVisibleTo("Allegro.CosmosDb.Migrator.Tests.Unit")]
[assembly: InternalsVisibleTo("Allegro.CosmosDb.Migrator.Tests.Integration")]
namespace Allegro.CosmosDb.Migrator.Infrastructure
{
    public static class Extensions
    {
        public static IConveyBuilder AddInfrastructure(this IConveyBuilder builder, IHostEnvironment hostingEnvironment)
        {
            builder.Services.AddTransient<IMigrationRepository, MigrationCosmosRepository>();
            builder.Services.AddTransient<IStatisticsRepository, StatisticsCosmosRepository>();
            builder.Services.AddTransient<IEventProcessor, EventProcessor>();
            builder.Services.AddTransient<IMessageBroker, NullMessageBroker>();

            builder.Services.AddSingleton<IDocumentCollectionClientFactory, CosmosDbDocumentCollectionClientFactory>();

            builder.Services.AddSingleton<IMigrationProcessorFactory, ChangeFeedProcessorFactory>();

            builder.Services.Scan(s => s.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            builder.Services.Scan(s => s.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                .AddClasses(c => c.AssignableTo(typeof(ICosmosRepository<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            builder.Services.Scan(s => s.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                .AddClasses(c => c.AssignableTo(typeof(ICosmosDbContainerInitializer)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            if (!hostingEnvironment.IsTestEnvironment())
            {
                builder.Services.AddHostedService<ChangeFeedProcessorHost>();
                builder.Services.AddHostedService<MigrationProgressHost>();
            }

            return builder
                .AddErrorHandler<ExceptionToResponseMapper>()
                .AddCosmos(hostingEnvironment)
                .AddQueryHandlers()
                .AddInMemoryQueryDispatcher()
                .AddWebApiSwaggerDocs();
        }

        private static IConveyBuilder AddCosmos(
            this IConveyBuilder builder,
            IHostEnvironment hostEnvironment,
            string sectionName = "cosmos")
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                sectionName = "cosmos";

            var options = builder.GetOptions<CosmosDbOptions>(sectionName);
            var cosmosClientBuilder = new CosmosClientBuilder(options.ConnectionString);

            builder.Services.AddSingleton<CosmosClient>(s =>
            {
                cosmosClientBuilder
                    .WithRequestTimeout(TimeSpan.FromSeconds(options.RequestTimeoutInSeconds == default ? 10 : options.RequestTimeoutInSeconds));

                if (hostEnvironment.IsDevelopment())
                {
                    cosmosClientBuilder.WithHttpClientFactory(() =>
                    {
                        HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                        {
#pragma warning disable MA0039
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#pragma warning restore MA0039
                        };
                        return new HttpClient(httpMessageHandler);
                    })
                        .WithConnectionModeGateway();
                }

                return cosmosClientBuilder.Build();
            });

            builder.Services.AddSingleton(options);

            builder.Services.AddSingleton<ICosmosStorage, CosmosStorage>();

            builder.Services.AddHostedService(sp => new CosmosInitializationHostedService(sp));

            return builder;
        }

        // private static IConveyBuilder AddCosmosRepository<T>(this IConveyBuilder builder) where T: ICosmosDocument
        // {
        //     builder.Services.AddScoped<ICosmosRepository<T>>(s =>
        //         new CosmosRepository<T>(s.GetRequiredService<ICosmosStorage>(),
        //             s.GetRequiredService<ITransactionScopeAssigner>()));
        //
        //     return builder;
        // }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            // PPTODO: add cosmos initialization here
            app.UseErrorHandler()
                .UseConvey()
                .UseSwaggerDocs()
                .UseErrorHandler()
                .UsePublicContracts<ContractAttribute>()
                // .UseMetrics()
                // .UseMiddleware<CustomMetricsMiddleware>()
                // .UseCertificateAuthentication()
                ;

            return app;
        }
    }
}