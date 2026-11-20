# Suwbackkit.Postgres

PostgreSQL for the backends of Manomenu's apps: one Npgsql data source, grate migrations when the server starts, and a health check.

- **Connection**: `ConnectionStrings:Default` (user secrets, or `ConnectionStrings__Default`). With migrations on, a missing one stops
  the server at start with a message that says what to set.
- **`NpgsqlDataSource`**: registered once, as Npgsql recommends; inject it and open connections from it. The pool defaults to
  `Maximum Pool Size=5` and `Keepalive=10`; any parameter in the connection string overrides them.
- **Migrations**: grate runs `Database/Migrations/up/*.sql` from the app's output folder in one transaction when the server starts.
  A script runs once; editing a script that already ran fails the migration; a failing script leaves nothing behind.
- **Health check** `database`: `SELECT 1`, so `/health` and `/api/health` from `Suwbackkit.Server` answer 503 with
  "The database does not answer" when it is down. It does not run for `/alive`.

## Usage

```csharp
using Suwbackkit.Postgres;
using Suwbackkit.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddServerDefaults();
builder.Services.AddDatabase(builder.Configuration, migrateOnStart: !ServerDefaults.WritingOpenApiDocument);

var app = builder.Build();
app.MapServerDefaults();
app.Run();
```

Copy the migrations to the output folder in the app's `.csproj`:

```xml
<None Include="Database/Migrations/**" CopyToOutputDirectory="PreserveNewest" />
```

Query with the data source, for example with Dapper (the app's choice, not a dependency of this package):

```csharp
internal sealed class NoteStore(NpgsqlDataSource dataSource)
{
    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return [.. await connection.QueryAsync<Note>(new CommandDefinition("SELECT id, text FROM notes", cancellationToken: cancellationToken))];
    }
}
```

Tests migrate their own database with `Migrator.MigrateAsync(connectionString, Migrator.DefaultDirectory)`.

## Broken connections

A connection the database closed while it sat idle in the pool (a database restart, `pg_terminate_backend`) is not tested before it is
handed out: Npgsql decided against a check on every open, because it costs a round trip each time and still races with the server
([Npgsql FAQ](https://www.npgsql.org/doc/faq.html#broken_connection_from_pool)). Instead `Keepalive=10` pings idle connections every
10 seconds and drops the dead ones, so after a restart only work in that window can meet a closed connection, which then fails once
and leaves the pool. Code that must survive that retries on `NpgsqlException.IsTransient`.

Source, license and changes: https://github.com/Manomenu/suwbackkit
