﻿using System.Collections.Generic;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Convey.CQRS.Queries;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Queries
{
    // TODO: paging
    [Contract]
    public class GetMigrations : IQuery<IEnumerable<MigrationDto>>
    {
    }
}