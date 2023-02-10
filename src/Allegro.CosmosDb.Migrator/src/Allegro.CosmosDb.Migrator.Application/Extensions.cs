using System.Runtime.CompilerServices;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Application.Services;
using Convey;
using Convey.CQRS.Commands;
using Convey.CQRS.Events;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Allegro.CosmosDb.Migrator.Tests.Unit")]
namespace Allegro.CosmosDb.Migrator.Application
{
    public static class Extensions
    {
        public static IConveyBuilder AddApplication(this IConveyBuilder builder)
        {
            builder.Services.AddSingleton<IEventMapper, DomainToIntegrationEventMapper>();
            builder.Services.AddTransient<IMigrationProcessorManager, MigrationProcessorManager>();
            builder.Services.AddTransient<IMigrationProgressManager, MigrationProgressManager>();

            return builder
                .AddCommandHandlers()
                .AddEventHandlers()
                .AddInMemoryCommandDispatcher()
                .AddInMemoryEventDispatcher();
        }

        public static IConveyBuilder AddApplicationInMemoryInfrastructure(this IConveyBuilder builder)
        {
            builder.Services.AddScoped<IMigrationProcessorFactory, InjectedMigrationProcessorFactory>();
            builder.Services.AddScoped<IEventProcessor, LogOnlyEventProcessor>();
            builder.Services.AddScoped<IMigrationRepository, InMemoryMigrationRepository>();
            builder.Services.AddScoped<IStatisticsRepository, InMemoryStatisticsRepository>();

            builder.Services.AddScoped<IDocumentCollectionClientFactory, InMemoryDocumentCollectionClientFactory>();
            return builder;
        }
    }
}