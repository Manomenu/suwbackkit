using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Suwbackkit.Postgres;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, bool migrateOnStart)
    {
        var databaseOptions = services
            .AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Default") ?? "")
            .Validate(
                options => options.ConnectionString.Length > 0,
                "ConnectionStrings:Default is missing: set it in user secrets or as ConnectionStrings__Default."
            );
        services.AddSingleton(provider =>
            DataSource.Create(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString)
        );
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);
        if (migrateOnStart)
        {
            databaseOptions.ValidateOnStart();
            services.AddHostedService<DatabaseStartup>();
        }
        return services;
    }
}
