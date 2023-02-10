using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core;

namespace Allegro.CosmosDb.Migrator.Application.Services
{
    public interface IDomainEventHandler<in T> where T : class, IDomainEvent
    {
        Task HandleAsync(T @event);
    }
}