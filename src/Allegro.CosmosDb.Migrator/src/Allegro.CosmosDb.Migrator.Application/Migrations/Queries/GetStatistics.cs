using System;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Convey.CQRS.Queries;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Queries
{
    [Contract]
    public class GetStatistics : IQuery<StatisticsDto>
    {
        public Guid MigrationId { get; }

        public GetStatistics(Guid migrationId)
        {
            MigrationId = migrationId;
        }
    }
}