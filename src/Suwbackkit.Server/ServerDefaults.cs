using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace Suwbackkit.Server;

public static class ServerDefaults
{
    internal const string LiveTag = "live";

    internal static readonly Dictionary<string, string?> LoggingDefaults = new()
    {
        ["Logging:LogLevel:Default"] = "Information",
        ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
        ["Logging:Console:FormatterName"] = "json",
        ["Logging:Console:FormatterOptions:IncludeScopes"] = "true",
        ["Logging:Console:FormatterOptions:UseUtcTimestamp"] = "true",
        ["Logging:Console:FormatterOptions:TimestampFormat"] = "yyyy-MM-ddTHH:mm:ss.fffZ",
    };

    public static bool WritingOpenApiDocument => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static TBuilder AddServerDefaults<TBuilder>(this TBuilder builder, bool contractSchemas = true)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource { InitialData = LoggingDefaults });
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier
        );
        builder.Services.Configure<ExceptionHandlerOptions>(options => options.StatusCodeSelector = StatusCodeOf);
        builder.Services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            if (contractSchemas)
            {
                options.AddContractSchemas();
            }
        });
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);
        return builder;
    }

    private static int StatusCodeOf(Exception error) =>
        error is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;

    public static WebApplication MapServerDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = check => check.Tags.Contains(LiveTag) }).AllowAnonymous();
        return app;
    }
}
