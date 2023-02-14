using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals
{
    internal interface ICosmosDbContainerInitializer
    {
        Task Initialize(ICosmosStorage cosmosStorage, CancellationToken cancellationToken);
    }

    internal sealed class CosmosInitializationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public CosmosInitializationHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var cosmosInitializers = _serviceProvider.GetServices<ICosmosDbContainerInitializer>().ToList();
            var cosmosStorage = _serviceProvider.GetRequiredService<ICosmosStorage>();

            await cosmosStorage.InitializeStorage();

            if (!cosmosInitializers.Any())
                return;

            var tasks = cosmosInitializers.Select(c => c.Initialize(cosmosStorage, cancellationToken));
            await Task.WhenAll(tasks);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}