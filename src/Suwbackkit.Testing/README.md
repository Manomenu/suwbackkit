# Suwbackkit.Testing

Test helpers for the backends of Manomenu's apps, the ones every app's test project carried a copy of. Nothing here knows xUnit:
the app's fixture keeps its own skipping and lifetime.

- **`TestDatabases.RecreateAsync(connectionString)`** drops the database the connection string names (closing every connection to
  it) and creates it empty; **`TestDatabases.DropAsync(connectionString)`** drops it, and is no error when it is gone. Both connect
  to the server's `postgres` database with a 3-second timeout and no pooling.
- **They refuse any database whose name lacks the word `test`** between underscores or hyphens (`myapp_test`, `myapp_test_<checkout>`,
  `myapp_test_knowledge`, `myapp_seed_test` pass; `myapp`, `postgres`, `latest`, `myapp_tests` do not), before they connect, with an
  `InvalidOperationException` that names the database and the fix. A connection string pointed at an app's own database by mistake
  (a stray `TEST_CONNECTION_STRING`) can never drop it.
- **`LinkedWorktree.RefuseSharedDatabase()`**, called first in the fixture, throws when `TEST_CONNECTION_STRING` is not set and the
  tests run in a linked git worktree (its `.git` is a file pointing into the main checkout's worktrees folder): the fixture would otherwise drop and recreate
  the main checkout's test database under the tests running there. The message says to run them after
  `eval "$(<app>.Web/node_modules/.bin/suwgate dev checkout-env)"`, which names the checkout's own databases, or to run the gate.
  `RefuseSharedDatabase(testConnectionString, testDirectory)` takes both from the caller, for a test of the fixture.
- **`CapturedLogs`**, an `ILoggerProvider` that keeps every entry in memory as a `CapturedLog` (category, level, message, and the
  `TraceId` of the scope it was written in, the one ASP.NET Core opens per request), so a test can assert that a request's error
  was logged with the trace id the response carries.

## Usage

```csharp
using Suwbackkit.Postgres;
using Suwbackkit.Testing;

[assembly: AssemblyFixture(typeof(Myapp.Server.Tests.TestDatabase))]

public sealed class TestDatabase : IAsyncLifetime
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
        ?? "Host=127.0.0.1;Port=5533;Database=myapp_test;Username=myapp;Password=myapp";

    public async ValueTask InitializeAsync()
    {
        LinkedWorktree.RefuseSharedDatabase();
        await TestDatabases.RecreateAsync(ConnectionString);
        await Migrator.MigrateAsync(ConnectionString, Migrator.DefaultDirectory);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

```csharp
var logs = new CapturedLogs();
builder.Services.AddSingleton<ILoggerProvider>(logs);
// ...
Assert.Contains(logs.Entries, entry => entry.Level == LogLevel.Error && entry.TraceId == traceId);
```

Source, license and changes: https://github.com/Manomenu/suwbackkit
