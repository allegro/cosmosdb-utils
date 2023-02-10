using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convey.CQRS.Events;

namespace Allegro.CosmosDb.Migrator.Infrastructure.Services
{
    internal interface IMessageBroker
    {
        Task PublishAsync(params IEvent[] events);
        Task PublishAsync(IEnumerable<IEvent> events);
    }

    internal class NullMessageBroker : IMessageBroker// TODO: support with events if needed
    {
        private readonly List<IEvent> _events = new();

        public Task PublishAsync(params IEvent[] events) => PublishAsync(events?.AsEnumerable());

        public Task PublishAsync(IEnumerable<IEvent>? events)
        {
            if (events is not null)
            {
                _events.AddRange(events);
            }

            return Task.CompletedTask;
        }
    }
}