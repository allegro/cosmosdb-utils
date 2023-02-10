using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Allegro.CosmosDb.Migrator.Infrastructure.BackgroundServices
{
    internal class MigrationProgressHost : BackgroundService
    {
        private const int SleepTime = 10000;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private static readonly TimeSpan DelayTime = TimeSpan.FromMilliseconds(SleepTime);

        private CancellationToken _cancellationToken;

        public MigrationProgressHost(IServiceScopeFactory serviceScopeFactory, ILogger<MigrationProgressHost> logger)
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
                    var migrationProgressManager =
                        scope.ServiceProvider.GetRequiredService<IMigrationProgressManager>();
                    await migrationProgressManager.ExecuteAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MigrationProgressHost error");
                }

                await Delay(cancellationToken);
            }
        }

        private static Task Delay(CancellationToken cancellationToken) => Task.Delay(DelayTime, cancellationToken);
    }
}