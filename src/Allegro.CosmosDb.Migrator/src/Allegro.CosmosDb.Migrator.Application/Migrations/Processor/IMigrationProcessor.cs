using System;
using System.Threading;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core.Migrations;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Processor
{
    public interface IMigrationProcessor : IDisposable
    {
        Task<MigrationProcessorResult> StartAsync(MigrationSnapshot migration);
        Task CloseAsync();
        Task CompleteAsync();
    }

    public interface IMigrationProcessorFactory
    {
        IMigrationProcessor Create(CancellationToken cancellationToken);
    }

    public class MigrationProcessorResult
    {
        public static MigrationProcessorResult Failure(string status) => new MigrationProcessorResult(false, status);
        public static MigrationProcessorResult Success() => new MigrationProcessorResult(true, string.Empty);

        private MigrationProcessorResult(bool status, string details)
        {
            Status = status;
            Details = details;
        }

        public bool Status { get; }
        public string Details { get; }
    }

    internal sealed class InjectedMigrationProcessorFactory : IMigrationProcessorFactory
    {
        private readonly IMigrationProcessor _migrationProcessor;

        public InjectedMigrationProcessorFactory(IMigrationProcessor migrationProcessor)
        {
            _migrationProcessor = migrationProcessor;
        }

        public IMigrationProcessor Create(CancellationToken cancellationToken)
        {
            return _migrationProcessor;
        }
    }
}