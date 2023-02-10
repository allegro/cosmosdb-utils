using System;
using Convey.CQRS.Commands;

namespace Allegro.CosmosDb.Migrator.Application.Migrations.Commands
{
    [Contract]
    public class StartMigration : ICommand
    {
        public Guid? MigrationId { get; set; }

        public string SourceConnectionString { get; }
        public string SourceDbName { get; }
        public string SourceCollectionName { get; }
        public int SourceMaxRuConsumption { get; }

        public DateTime? StartFromUtc { get; }

        public string DestinationConnectionString { get; }
        public string DestinationDbName { get; }
        public string DestinationCollectionName { get; }
        public int DestinationMaxRuConsumption { get; }

        public StartMigration(string sourceConnectionString, string sourceDbName, string sourceCollectionName, int sourceMaxRuConsumption, DateTime? startFromUtc, string destinationConnectionString, string destinationDbName, string destinationCollectionName, int destinationMaxRuConsumption)
        {
            SourceConnectionString = sourceConnectionString;
            SourceDbName = sourceDbName;
            SourceCollectionName = sourceCollectionName;
            SourceMaxRuConsumption = sourceMaxRuConsumption;
            StartFromUtc = startFromUtc;
            DestinationConnectionString = destinationConnectionString;
            DestinationDbName = destinationDbName;
            DestinationCollectionName = destinationCollectionName;
            DestinationMaxRuConsumption = destinationMaxRuConsumption;
        }
    }
}