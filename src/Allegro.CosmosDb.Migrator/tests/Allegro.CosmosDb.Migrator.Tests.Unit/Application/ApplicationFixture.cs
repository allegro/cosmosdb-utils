using System;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application;
using Allegro.CosmosDb.Migrator.Application.Migrations;
using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Allegro.CosmosDb.Migrator.Application.Services;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Allegro.CosmosDb.Migrator.Tests.Unit.Core;
using Convey;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Application
{
    public class ApplicationFixture
    {
        private IServiceProvider? _serviceProvider;
        private readonly CoreFixture _coreFixture;
        private readonly IConveyBuilder _conveyBuilder;

        public ApplicationFixture()
        {
            var services = new ServiceCollection();

            _conveyBuilder = ConveyBuilder.Create(services);

            _conveyBuilder.AddApplication();
            _conveyBuilder.AddApplicationInMemoryInfrastructure();
            _conveyBuilder.Services.AddSingleton(Substitute.For<ILogger<IMigrationProcessorManager>>());
            _conveyBuilder.Services.AddSingleton(Substitute.For<ILogger<IMigrationProgressManager>>());

            _coreFixture = new CoreFixture();
        }

        public ApplicationFixture WithMigrationProcessor(IMigrationProcessor processor)
        {
            _conveyBuilder.Services.AddTransient<IMigrationProcessor>(p => processor);

            return this;
        }

        public ApplicationFixture Build()
        {
            _serviceProvider = _conveyBuilder.Build();

            return this;
        }

        public async Task<Migrator.Core.Migrations.Migration> WithMigration(string name)
        {
            var migrationRepository = GetService<IMigrationRepository>();

            var migration = _coreFixture.CreateMigration();
            await migrationRepository.Update(migration);

            return migration;
        }

        public async Task<Migrator.Core.Migrations.Migration> WithActiveMigration(string name)
        {
            var migrationRepository = GetService<IMigrationRepository>();

            var migration = _coreFixture.CreateMigration();
            migration.Init();
            await migrationRepository.Update(migration);

            var statisticsRepository = GetService<IStatisticsRepository>();
            await statisticsRepository.Add(Statistics.Create(migration.Id));

            return migration;
        }

        public async Task<Migrator.Core.Migrations.Migration> WithCompletedMigration(string name)
        {
            var migration = await WithMigration(name);
            migration.Complete();
            return migration;
        }

        public T GetService<T>()
        {
            if (_serviceProvider is null)
            {
                throw new NotSupportedException("Service provider should be initialized");
            }

            return _serviceProvider.GetService<T>() ?? throw new NotSupportedException($"Service was not registered {typeof(T).Name}");
        }

        internal IDocumentCollectionCrudClient GetCrudDocumentClientFor(CollectionConfig config)
        {
            return (IDocumentCollectionCrudClient)GetService<IDocumentCollectionClientFactory>().Create(config);
        }
    }
}