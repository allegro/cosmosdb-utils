using System.Collections.Generic;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core;

namespace Allegro.CosmosDb.Migrator.Application.Services
{
    public interface IEventProcessor
    {
        Task ProcessAsync(IEnumerable<IDomainEvent> events);
    }

    internal sealed class LogOnlyEventProcessor : IEventProcessor
    {
        private readonly List<IDomainEvent> _receivedEvents = new();

        public IReadOnlyCollection<IDomainEvent> ReceivedEvents => _receivedEvents;

        public Task ProcessAsync(IEnumerable<IDomainEvent> events)
        {
            _receivedEvents.AddRange(events);

            return Task.CompletedTask;
        }
    }
}