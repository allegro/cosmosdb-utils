using System;
using AllegroPay.CosmosDb.BatchUtilities;
using AllegroPay.CosmosDb.BatchUtilities.Configuration;
using AllegroPay.CosmosDb.BatchUtilities.Extensions;
using AllegroPay.CosmosDb.ConsistencyLevelHelpers;
using AllegroPay.CosmosDb.Demo.Configuration;
using AllegroPay.CosmosDb.Demo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllegroPay.CosmosDb.Demo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            var cosmosConfiguration = _configuration
                .GetSection("CosmosDb")
                .Get<CosmosDbConfiguration>(o => o.BindNonPublicProperties = true);

            services.AddControllers();
            services.AddLogging(logging => logging.AddConsole());
            services.AddSingleton(cosmosConfiguration);
            services.AddSingleton<Func<CosmosClientBuilder>>(_ =>
                () => new CosmosClientBuilder(cosmosConfiguration.EndpointUri, cosmosConfiguration.Key));
            services.AddSingleton(sp => sp.GetRequiredService<Func<CosmosClientBuilder>>()().Build());
            services.AddHostedService<InitializeCosmosHostedService>();
            services.AddCosmosBatchUtilities(
                BatchUtilitiesRegistration.ForContainer(
                    cosmosConfiguration.DatabaseName,
                    cosmosConfiguration.ContainerName,
                    CosmosRuLimiters.CosmosDocumentRuLimiter,
                    new CosmosAutoScalerConfiguration
                    {
                        Enabled = true,
                        IdleMaxThroughput = 400,
                        ProcessingMaxThroughput = 2000,
                        ProcessingMaxRu = 1000,
                        ProvisioningMode = CosmosProvisioningMode.Manual,
                        DownscaleGracePeriod = TimeSpan.FromMinutes(10)
                    }));

            ContainerExtensions.WarmUp();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}