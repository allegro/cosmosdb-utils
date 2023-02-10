using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Exceptions;

namespace Allegro.CosmosDb.Migrator.Core.Migrations.Exceptions
{
    public class InvalidMigrationStateException : DomainException
    {
        public override string Code => "invalid_migration_state";

        public InvalidMigrationStateException(AggregateId migrationId, string operationName) : base(
            $"Migration with id {migrationId} is not in proper state for operation {operationName}")
        {
        }
    }
}