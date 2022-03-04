using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allegro.CosmosDb.BatchUtilities
{
    public class CosmosAutoScalerInitializationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public CosmosAutoScalerInitializationHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // warms up auto scaler
            _serviceProvider.GetRequiredService<ICosmosAutoScalerFactory>();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}