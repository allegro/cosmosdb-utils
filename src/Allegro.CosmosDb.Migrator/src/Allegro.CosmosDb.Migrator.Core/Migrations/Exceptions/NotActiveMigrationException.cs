using Allegro.CosmosDb.Migrator.Core.Entities;
using Allegro.CosmosDb.Migrator.Core.Exceptions;

namespace Allegro.CosmosDb.Migrator.Core.Migrations.Exceptions
{
    public class NotActiveMigrationException : DomainException
    {
        public override string Code => "not_active_migration";

        public NotActiveMigrationException(AggregateId migrationId) : base($"Migration with id {migrationId} is not active")
        {
        }
    }
}