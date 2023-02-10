using System;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Convey.CQRS.Queries;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Queries
{
    [Contract]
    public class GetMigration : IQuery<MigrationDto>
    {
        public Guid MigrationId { get; }

        public GetMigration(Guid migrationId)
        {
            MigrationId = migrationId;
        }
    }
}