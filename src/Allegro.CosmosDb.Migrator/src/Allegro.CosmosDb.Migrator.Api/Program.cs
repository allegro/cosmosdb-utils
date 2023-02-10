using System.Collections.Generic;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Application;
using Allegro.CosmosDb.Migrator.Application.Migrations.Commands;
using Allegro.CosmosDb.Migrator.Application.Migrations.DTO;
using Allegro.CosmosDb.Migrator.Application.Migrations.Queries;
using Allegro.CosmosDb.Migrator.Infrastructure;
using Convey;
using Convey.Docs.Swagger;
using Convey.Logging;
using Convey.Types;
using Convey.WebApi;
using Convey.WebApi.CQRS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Allegro.CosmosDb.Migrator.Api;

public class Program
{
    private static IHostEnvironment? HostingEnvironment { get; set; }
    public static Task Main(string[] args)
        => CreateHostBuilder(args).Build().RunAsync();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                HostingEnvironment = hostingContext.HostingEnvironment;
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices(services => services
                        .AddConvey()
                        .AddWebApi()
                        .AddApplication()
                        .AddInfrastructure(HostingEnvironment!)
                        .Build())
                    .Configure(app => app
                        .UseInfrastructure()
                        .UseDispatcherEndpoints(endpoints => endpoints
                            .Get(string.Empty, ctx => ctx.Response.WriteAsync(ctx.RequestServices.GetService<AppOptions>()!.Name))
                            .Post<StartMigration>(
                                "migrations",
                                afterDispatch: (cmd, ctx) => ctx.Response.Created($"migrations/{cmd.MigrationId}"))
                            .Get<GetMigration, MigrationDto>("migrations/{migrationId}")
                            .Get<GetStatistics, StatisticsDto>("migrations/{migrationId}/statistics")
                            .Put<PauseMigration>("migrations/{migrationId}/pause")
                            .Put<ResumeMigration>("migrations/{migrationId}/resume")
                            .Get<GetMigrations, IEnumerable<MigrationDto>>("migrations")
                            .Delete<CompleteMigration>("migrations/{migrationId}")
                        )
                        .UseSwaggerDocs())
                    .UseLogging();
            });
}