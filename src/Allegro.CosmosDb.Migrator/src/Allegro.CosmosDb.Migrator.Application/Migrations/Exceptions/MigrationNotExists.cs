using System;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Exceptions
{
    public class MigrationNotExists : AppException
    {
        public override string Code { get; } = "migration_not_exists";
        public Guid Id { get; }

        public MigrationNotExists(Guid id) : base($"Migration with id: {id} not exists.")
            => Id = id;
    }
}