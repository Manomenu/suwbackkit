# Suwbackkit.Server

Server defaults for the ASP.NET Core backends of Manomenu's apps, in two calls, the way .NET Aspire's ServiceDefaults does it:

- **Logging**: JSON on the console, with scopes and UTC timestamps; `Microsoft.AspNetCore` at `Warning`. These are the lowest-priority
  configuration values, so `appsettings.json` or environment variables override any of them (`Logging__Console__FormatterName=simple`).
- **ProblemDetails**: every error is `application/problem+json` with a `traceId` (the W3C trace id the logs carry in their scope),
  from unhandled exceptions (`UseExceptionHandler`) and from empty error responses such as 404 (`UseStatusCodePages`). A request
  body the endpoint cannot read stays a 400, also in Development, where minimal APIs throw `BadHttpRequestException` instead of
  answering 400 themselves.
- **OpenAPI**: `AddOpenApi` with the document pinned to OpenAPI 3.1, so a newer .NET does not change the contract on its own. The pin
  holds for the document served at run time and for the one `Microsoft.Extensions.ApiDescription.Server` writes at build time: the build
  ignores `OpenApiOptions`, so the package also adds `--openapi-version OpenApi3_1` to `OpenApiGenerateDocumentsOptions`, unless the
  app's project already names a version there. An app adds its own options with another `AddOpenApi(...)` call.
- **Health checks**: `/health` runs every check (503 when one fails), `/alive` only the liveness ones (the process answers);
  both stay out of the OpenAPI document and answer without credentials (`AllowAnonymous`), also in an app whose other endpoints ask for them. Other packages add their checks with `AddHealthChecks()`, as `Suwbackkit.Postgres` does.
- **Contract schemas** (on by default; `AddServerDefaults(contractSchemas: false)` opts out): every schema says what the JSON always
  holds, so the types an app generates from the document (openapi-typescript) need no `?` or `| null` where the server never leaves
  a value out: a property the server always writes and never as `null` (a get-only property of a non-null reference type, which
  ASP.NET Core leaves optional and nullable) is required and not nullable; a union's discriminator (`[JsonPolymorphic]`) is required in
  each of its variants; and `null` is no value of an enum (ASP.NET Core adds it to an enum used only as a nullable property, whose
  property already says `null`).
- **`/api/health` in the contract**: `MapHealthStatus()` on a route group maps `GET <group>/health`, answering `{"status":"ok"}`, or
  503 ProblemDetails naming the failed checks; it answers without credentials too.

- **Refusals**: one shape for "the server refused, here is what is wrong and how to fix it" (below), from `Refusals.Refuse(...)`, with
  its schema in the contract from `.ProducesRefusal<TCode>(status)`.

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

The app's own OpenAPI options go in another call, beside the defaults:

```csharp
builder.Services.AddOpenApi(options => options.AddDocumentTransformer(...));
```

An app that is not ready for the stricter schemas yet keeps ASP.NET Core's with `builder.AddServerDefaults(contractSchemas: false)`;
`options.AddContractSchemas()` stays public for an `OpenApiOptions` of a second document.

Call `MapServerDefaults()` right after `Build()`: it puts the exception handler first in the pipeline.

`ServerDefaults.WritingOpenApiDocument` is true while the build-time OpenAPI generator (`GetDocument.Insider`) runs the app only to read its
routes: skip anything that needs the outside world then, such as migrations or background workers.

```csharp
builder.Services.AddDatabase(builder.Configuration, migrateOnStart: !ServerDefaults.WritingOpenApiDocument);
```

## Refusals

A refusal is the server saying no because of what the caller sent (422, 409, 404, 403). Every server on this kit sends it in one
shape, so a web client (suwfrontkit's `ApiError`), a CLI and an agent read refusals of any of our apps the same way. It is an
RFC 9457 ProblemDetails (`type`, `title`, `status`, `detail`, `traceId`) with two extension members:

| Member | Type | Meaning |
| :- | :- | :- |
| `code` | string, always on a refusal | why the request was refused, a stable dotted word (`resource.stale`); callers branch on it, never on the text. In the app's contract it is the app's enum of codes |
| `mistakes` | array, optional | one item per thing that is wrong |
| `mistakes[].code` | string | the kind of this mistake, an app enum like `code` |
| `mistakes[].message` | string | what is wrong, in a sentence a person or an agent reads |
| `mistakes[].fix` | string | what to do about it, in a sentence, never empty |
| `mistakes[].at` | string, optional | where it applies, in the app's own terms: a field path, a step id, a resource id |
| other members of a mistake | any | the app's own detail (a stage, a resource, related items), kept as sent |

With mistakes, `detail` says how many there are and to fix each as its `fix` says; without them, `detail` itself says what to do. A
failure that is not a refusal (an unhandled exception, a 503, a 404 of an unknown route) keeps the plain ProblemDetails, with no `code`.
ASP.NET Core's `errors` of a model-binding failure stays as the framework sends it.

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<NoteCode>))]
internal enum NoteCode
{
    [JsonStringEnumMemberName("note.invalid")] Invalid,
    [JsonStringEnumMemberName("note.stale")] Stale,
    [JsonStringEnumMemberName("note.titleMissing")] TitleMissing,
}

routes.MapPut("/notes/{id}", Save)
    .ProducesRefusal<NoteCode>(StatusCodes.Status409Conflict)
    .ProducesRefusal<NoteCode>(StatusCodes.Status422UnprocessableEntity);

static Results<Ok<Note>, ProblemHttpResult> Save(...) =>
    stale
        ? Refusals.Refuse(StatusCodes.Status409Conflict, "The note changed", NoteCode.Stale, "Read it again and save once more.")
        : Refusals.Refuse(
            StatusCodes.Status422UnprocessableEntity,
            "The note cannot be saved",
            NoteCode.Invalid,
            [new Mistake<NoteCode>(NoteCode.TitleMissing, "The note has no title.", "Give it a title.") { At = "title" }]
        );
```

- `Refuse` refuses to build a refusal without a title, a detail or a code, or a mistake without a code, a message or a fix
  (`ArgumentException`): a refusal that cannot say what to fix is a bug of the server.
- The codes are the app's own enum (with `JsonStringEnumMemberName` for dotted names), so `.ProducesRefusal<TCode>()` lists them as
  the enum of `code` and of `mistakes[].code`, and a new code is a new enum value in the contract. A `string` works too, without the enum.
- A mistake with more to say derives from `Mistake<TCode>` (`record NoteMistake(NoteCode Code, string Message, string Fix, Stage Stage)
  : Mistake<NoteCode>(Code, Message, Fix)`); pass a list of it to `Refuse` and declare `.ProducesRefusal<NoteCode, NoteMistake>(status)`,
  and its members travel and are documented next to the shared ones.
- In the contract the shape is `Refusal<TCode, TMistake>`: `code`, `title`, `detail`, `status`, `type` and `traceId` required,
  `mistakes` optional, and each mistake's `code`, `message` and `fix` required.

`ServerKit.Version` is the version of this package, for an app that logs which kit it runs.

Source, license and changes: https://github.com/Manomenu/suwbackkit
