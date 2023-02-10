using System;
using Convey.CQRS.Commands;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Commands
{
    [Contract]
    public class CompleteMigration : ICommand
    {
        public CompleteMigration(Guid migrationId)
        {
            MigrationId = migrationId;
        }

        public Guid MigrationId { get; }
    }
}