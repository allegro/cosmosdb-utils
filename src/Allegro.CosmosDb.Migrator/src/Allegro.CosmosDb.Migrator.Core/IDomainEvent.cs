using System;

namespace Allegro.CosmosDb.Migrator.Core
{
    public interface IDomainEvent
    {
        DateTime TimeStamp { get; }
    }
}