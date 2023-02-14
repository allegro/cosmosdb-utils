using Allegro.CosmosDb.Migrator.Core.Migrations;
using FizzWare.NBuilder;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Core
{
    public class CoreFixture
    {
        private readonly RandomGenerator _randomGenerator;
        public CoreFixture()
        {
            _randomGenerator = new RandomGenerator();

        }

        public Migration CreateMigration()
        {

            var migration = Migration.Create(sourceConfig: CreateCollectionConfig(),
                destinationConfig: CreateCollectionConfig());
            return migration;
        }

        public CollectionConfig CreateCollectionConfig()
        {
            return new(_randomGenerator.NextString(40, 50), _randomGenerator.NextString(10, 20), _randomGenerator.NextString(10, 20));
        }
    }
}