using System.CommandLine;

namespace Suwbackkit.Cli;

public static class CommandTree
{
    public static Command? Find(Command root, string words)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(words);
        var found = root;
        foreach (var word in words.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            found = found?.Subcommands.SingleOrDefault(command => command.Name == word);
        }
        return found;
    }

    public static IEnumerable<Command> All(Command root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.Subcommands.SelectMany(child => All(child).Prepend(child));
    }

    public static string WordsOf(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var words = new List<string>();
        for (var current = command; current.Parents.OfType<Command>().FirstOrDefault() is { } parent; current = parent)
        {
            words.Insert(0, current.Name);
        }
        return string.Join(" ", words);
    }
}
