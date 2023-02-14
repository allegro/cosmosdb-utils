using System;
using Convey.CQRS.Commands;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Commands
{
    [Contract]
    public class PauseMigration : ICommand
    {
        public PauseMigration(Guid migrationId)
        {
            MigrationId = migrationId;
        }

        public Guid MigrationId { get; }
    }
}