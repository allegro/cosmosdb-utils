using System.Collections.Generic;

namespace Allegro.CosmosDb.Migrator.Core.Entities
{
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        protected AggregateRoot(AggregateId id, string version)
        {
            Id = id;
            Version = version;
        }

        protected IEnumerable<IDomainEvent> Events => _events;

        public AggregateId Id { get; protected set; }
        public string Version { get; protected set; }

        protected void AddEvent(IDomainEvent @event)
        {
            _events.Add(@event);
        }

        protected void ClearEvents() => _events.Clear();
    }
}