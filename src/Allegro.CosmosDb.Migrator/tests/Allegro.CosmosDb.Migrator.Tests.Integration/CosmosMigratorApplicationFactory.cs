using System;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals;
using Allegro.CosmosDb.Migrator.Tests.Integration.Infrastructure.CosmosDb;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allegro.CosmosDb.Migrator.Tests.Integration
{
    public class CosmosMigratorApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        internal CosmosDbFixture? CosmosDbFixture { get; private set; }

        protected override IHostBuilder CreateHostBuilder()
            => base.CreateHostBuilder().UseEnvironment("tests");

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            var cosmosDbOptions = host.Services.GetRequiredService<CosmosDbOptions>();

            var testCollectionConfig = new CollectionConfig(
                cosmosDbOptions.ConnectionString,
                $"{cosmosDbOptions.Database}",
                $"TestCollection{DateTime.UtcNow:yyyyMMddhhmmss}");
            CosmosDbFixture = new CosmosDbFixture(testCollectionConfig);
            CosmosDbFixture.Initialize().Wait();

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CosmosDbFixture?.DisposeAsync().AsTask().Wait();
        }
    }
}