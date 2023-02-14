using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Migrator.Infrastructure.BackgroundServices
{
    internal class ChangeFeedProcessorHost : BackgroundService
    {
        private static readonly TimeSpan DelayTime = TimeSpan.FromSeconds(10);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;

        private CancellationToken _cancellationToken;

        public ChangeFeedProcessorHost(IServiceScopeFactory serviceScopeFactory, ILogger<ChangeFeedProcessorHost> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            await Delay(cancellationToken);

            while (_cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    var migrationProcessorManager =
                        scope.ServiceProvider.GetRequiredService<IMigrationProcessorManager>();

                    await migrationProcessorManager.ExecuteAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChangeFeedProcessorHost error");
                }

                await Delay(cancellationToken);
            }
        }

        private static Task Delay(CancellationToken cancellationToken) => Task.Delay(DelayTime, cancellationToken);
    }
}