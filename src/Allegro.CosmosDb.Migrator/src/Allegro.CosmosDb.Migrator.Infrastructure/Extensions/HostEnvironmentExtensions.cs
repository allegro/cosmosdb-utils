using Microsoft.Extensions.Hosting;

namespace Allegro.CosmosDb.Migrator.Infrastructure
{
    public static class HostEnvironmentExtensions
    {
        public static bool IsTestEnvironment(this IHostEnvironment hostEnvironment) =>
            hostEnvironment.IsEnvironment("tests");
    }
}