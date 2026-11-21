namespace Suwbackkit.Testing.Tests;

public sealed class LinkedWorktreeTests : IDisposable
{
    private readonly DirectoryInfo checkout = Directory.CreateTempSubdirectory("suwbackkit-linked-worktree-");

    private string TestDirectory => Directory.CreateDirectory(Path.Combine(checkout.FullName, "bin", "Debug")).FullName;

    private void LinkedTo(string gitDirectory) => File.WriteAllText(Path.Combine(checkout.FullName, ".git"), $"gitdir: {gitDirectory}\n");

    [Fact]
    public void A_linked_worktree_without_its_own_connection_string_is_refused_with_the_way_to_get_one()
    {
        LinkedTo("/repos/main/.git/worktrees/feature");

        var refusal = Assert.Throws<InvalidOperationException>(() => LinkedWorktree.RefuseSharedDatabase(null, TestDirectory));

        Assert.Contains("TEST_CONNECTION_STRING is not set", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("suwgate dev checkout-env", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_linked_worktree_on_windows_is_recognised_too()
    {
        LinkedTo(@"C:\repos\main\.git\worktrees\feature");

        Assert.Throws<InvalidOperationException>(() => LinkedWorktree.RefuseSharedDatabase(null, TestDirectory));
    }

    [Fact]
    public void A_linked_worktree_with_its_own_connection_string_is_accepted()
    {
        LinkedTo("/repos/main/.git/worktrees/feature");

        LinkedWorktree.RefuseSharedDatabase("Host=127.0.0.1;Database=myapp_test_feature", TestDirectory);
    }

    [Fact]
    public void The_main_checkout_keeps_the_plain_database()
    {
        Directory.CreateDirectory(Path.Combine(checkout.FullName, ".git"));

        LinkedWorktree.RefuseSharedDatabase(null, TestDirectory);
    }

    [Fact]
    public void A_submodule_is_not_a_linked_worktree()
    {
        LinkedTo("../.git/modules/library");

        LinkedWorktree.RefuseSharedDatabase(null, TestDirectory);
    }

    public void Dispose() => checkout.Delete(recursive: true);
}
