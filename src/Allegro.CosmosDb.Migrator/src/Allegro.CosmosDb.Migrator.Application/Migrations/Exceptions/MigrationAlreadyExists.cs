using System;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Exceptions
{
    public class MigrationAlreadyExists : AppException
    {
        public override string Code { get; } = "migration_already_exists";
        public Guid Id { get; }

        public MigrationAlreadyExists(Guid id) : base($"Migration with id: {id} already exists.")
            => Id = id;
    }
}