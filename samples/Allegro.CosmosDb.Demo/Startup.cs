using System;
using Allegro.CosmosDb.BatchUtilities;
using Allegro.CosmosDb.BatchUtilities.Configuration;
using Allegro.CosmosDb.BatchUtilities.Extensions;
using Allegro.CosmosDb.ConsistencyLevelUtilities;
using Allegro.CosmosDb.Demo.Configuration;
using Allegro.CosmosDb.Demo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Demo
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
            services.AddSwaggerGen();
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
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}