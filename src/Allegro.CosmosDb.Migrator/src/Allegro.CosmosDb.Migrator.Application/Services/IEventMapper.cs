using System.Collections.Generic;
using System.Linq;
using Allegro.CosmosDb.Migrator.Core;
using Convey.CQRS.Events;

namespace Allegro.CosmosDb.Migrator.Application.Services
{
    public interface IEventMapper
    {
        IEvent? Map(IDomainEvent @event);
        IEnumerable<IEvent> MapAll(IEnumerable<IDomainEvent> events);
    }

    internal sealed class DomainToIntegrationEventMapper : IEventMapper
    {
        public IEnumerable<IEvent> MapAll(IEnumerable<IDomainEvent> events)
            => events.Select(@event => Map(@event)).Where(@event => @event is not null)!;

        public IEvent? Map(IDomainEvent @event)
            => @event switch
            {
                _ => null
            };
    }
}