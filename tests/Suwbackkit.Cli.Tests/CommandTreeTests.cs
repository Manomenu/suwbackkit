using System.CommandLine;

namespace Suwbackkit.Cli.Tests;

public sealed class CommandTreeTests
{
    private static RootCommand Tree() =>
        new("app")
        {
            new Command("status"),
            new Command("facts") { new Command("list"), new Command("show") },
            new Command("versions") { new Command("delete") { new Command("plan") } },
        };

    [Theory]
    [InlineData("status")]
    [InlineData("facts show")]
    [InlineData("versions delete plan")]
    public void A_command_is_found_by_its_words(string words)
    {
        ArgumentNullException.ThrowIfNull(words);

        Assert.Equal(words.Split(' ')[^1], CommandTree.Find(Tree(), words)?.Name);
    }

    [Theory]
    [InlineData("frobnicate")]
    [InlineData("facts add")]
    [InlineData("status list")]
    public void Words_that_name_no_command_find_nothing(string words) => Assert.Null(CommandTree.Find(Tree(), words));

    [Fact]
    public void All_walks_every_command_below_the_root_each_before_its_own()
    {
        var root = Tree();

        Assert.Equal(
            ["status", "facts", "facts list", "facts show", "versions", "versions delete", "versions delete plan"],
            CommandTree.All(root).Select(CommandTree.WordsOf)
        );
    }

    [Fact]
    public void The_root_has_no_words() => Assert.Equal("", CommandTree.WordsOf(Tree()));
}
