namespace Suwbackkit.Testing;

public static class LinkedWorktree
{
    public const string Variable = "TEST_CONNECTION_STRING";

    public static void RefuseSharedDatabase() =>
        RefuseSharedDatabase(Environment.GetEnvironmentVariable(Variable), AppContext.BaseDirectory);

    public static void RefuseSharedDatabase(string? testConnectionString, string testDirectory)
    {
        if (testConnectionString is not null || !Contains(testDirectory))
        {
            return;
        }
        throw new InvalidOperationException(
            $"{Variable} is not set, and this is a linked git worktree, so the tests would use the main checkout's test databases, "
                + "which the fixture drops and recreates under the tests running there: run them after "
                + "eval \"$(<app>.Web/node_modules/.bin/suwgate dev checkout-env)\", which names this checkout's own databases, "
                + "or run the gate (just check)."
        );
    }

    private static bool Contains(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var git = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(git))
            {
                return false;
            }
            if (File.Exists(git))
            {
                return File.ReadAllText(git).Replace('\\', '/').Contains("/worktrees/", StringComparison.Ordinal);
            }
        }
        return false;
    }
}
