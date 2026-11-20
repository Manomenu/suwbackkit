# Suwbackkit.Server

Server defaults for the ASP.NET Core backends of Manomenu's apps, in two calls, the way .NET Aspire's ServiceDefaults does it:

- **Logging**: JSON on the console, with scopes and UTC timestamps; `Microsoft.AspNetCore` at `Warning`. These are the lowest-priority
  configuration values, so `appsettings.json` or environment variables override any of them (`Logging__Console__FormatterName=simple`).
- **ProblemDetails**: every error is `application/problem+json` with a `traceId` (the W3C trace id the logs carry in their scope),
  from unhandled exceptions (`UseExceptionHandler`) and from empty error responses such as 404 (`UseStatusCodePages`).
- **OpenAPI**: `AddOpenApi` with the document pinned to OpenAPI 3.1, so a newer .NET does not change the contract on its own. The pin
  holds for the document served at run time and for the one `Microsoft.Extensions.ApiDescription.Server` writes at build time: the build
  ignores `OpenApiOptions`, so the package also adds `--openapi-version OpenApi3_1` to `OpenApiGenerateDocumentsOptions`, unless the
  app's project already names a version there. An app adds its own options with another `AddOpenApi(...)` call.
- **Health checks**: `/health` runs every check (503 when one fails), `/alive` only the liveness ones (the process answers);
  both stay out of the OpenAPI document. Other packages add their checks with `AddHealthChecks()`, as `Suwbackkit.Postgres` does.
- **`/api/health` in the contract**: `MapHealthStatus()` on a route group maps `GET <group>/health`, answering `{"status":"ok"}`, or
  503 ProblemDetails naming the failed checks.

## Usage

```csharp
using Suwbackkit.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddServerDefaults();

var app = builder.Build();
app.MapServerDefaults();
app.MapGroup("/api").MapHealthStatus();
app.Run();
```

Call `MapServerDefaults()` right after `Build()`: it puts the exception handler first in the pipeline.

`ServerDefaults.WritingOpenApiDocument` is true while the build-time OpenAPI generator (`GetDocument.Insider`) runs the app only to read its
routes: skip anything that needs the outside world then, such as migrations or background workers.

```csharp
builder.Services.AddDatabase(builder.Configuration, migrateOnStart: !ServerDefaults.WritingOpenApiDocument);
```

`ServerKit.Version` is the version of this package, for an app that logs which kit it runs.

Source, license and changes: https://github.com/Manomenu/suwbackkit
