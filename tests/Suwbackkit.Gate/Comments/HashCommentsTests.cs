using System.Text.RegularExpressions;

namespace Suwbackkit.Gate.Comments;

public sealed partial class HashCommentsTests
{
    private static readonly string[] WholeLineOnly = [".gitignore", ".gitattributes", ".editorconfig"];

    private static readonly string[] AlsoAtTheEndOfALine = [".sh", ".yaml", ".yml", ".rsp"];

    [GeneratedRegex(@"^# shellcheck (disable|enable|source|shell|external-sources)=\S+$")]
    private static partial Regex ShellcheckDirective();

    [GeneratedRegex($"^# {CommentFiles.CriterionId}$")]
    private static partial Regex CriterionMarker();

    [GeneratedRegex(@"^(@?[A-Za-z_][\w-]*(\s+[^:=]*)?:(?!=)|mod\s+[\w-]+\s*$)")]
    private static partial Regex RecipeOrModule();

    private static string Name(string path) => Path.GetFileName(path);

    private static bool IsJust(string path) => Name(path) == "justfile" || path.EndsWith(".just", StringComparison.Ordinal);

    private static bool IsShell(string path) =>
        path.EndsWith(".sh", StringComparison.Ordinal) || path.StartsWith(".githooks/", StringComparison.Ordinal);

    private static bool UsesHashComments(string path) =>
        IsJust(path)
        || IsShell(path)
        || WholeLineOnly.Contains(Name(path))
        || AlsoAtTheEndOfALine.Any(extension => path.EndsWith(extension, StringComparison.Ordinal));

    private static bool AlsoAtTheEnd(string path) =>
        IsShell(path) || AlsoAtTheEndOfALine.Any(extension => path.EndsWith(extension, StringComparison.Ordinal));

    private static List<FoundComment> CommentsIn(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var found = new List<FoundComment>();
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var comment = CommentOn(path, lines[index]);
            if (comment is not null && !IsAllowed(path, lines, index, comment))
            {
                found.Add(new FoundComment(index + 1, comment));
            }
        }
        return found;
    }

    private static string? CommentOn(string path, string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith('#') || (Name(path) == ".editorconfig" && trimmed.StartsWith(';')))
        {
            return trimmed.TrimEnd();
        }
        return AlsoAtTheEnd(path) ? CommentAtTheEnd(line) : null;
    }

    private static string? CommentAtTheEnd(string line)
    {
        var quote = '\0';
        for (var at = 0; at < line.Length; at++)
        {
            var sign = line[at];
            if (quote != '\0')
            {
                if (sign == '\\' && quote == '"')
                {
                    at++;
                }
                else if (sign == quote)
                {
                    quote = '\0';
                }
            }
            else if (sign is '"' or '\'')
            {
                quote = sign;
            }
            else if (sign == '#' && at > 0 && char.IsWhiteSpace(line[at - 1]))
            {
                return line[at..].TrimEnd();
            }
        }
        return null;
    }

    private static bool IsAllowed(string path, string[] lines, int index, string comment) =>
        (index == 0 && comment.StartsWith("#!", StringComparison.Ordinal))
        || (IsShell(path) && ShellcheckDirective().IsMatch(comment))
        || (Name(path) == ".editorconfig" && CriterionMarker().IsMatch(comment))
        || (IsJust(path) && DescribesTheRecipeBelow(lines, index));

    private static bool DescribesTheRecipeBelow(string[] lines, int index)
    {
        var next = index + 1;
        while (next < lines.Length && lines[next].StartsWith('[') && !lines[next].StartsWith("[private]", StringComparison.Ordinal))
        {
            next++;
        }
        return next < lines.Length && RecipeOrModule().IsMatch(lines[next]);
    }

    [Fact]
    public void No_shell_yaml_ignore_or_just_file_holds_a_comment_but_a_tool_directive() =>
        CommentFiles.AssertNone(CommentFiles.Matching(UsesHashComments), CommentsIn);

    [Theory]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "#!/usr/bin/env bash\nset -euo pipefail", 0)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "#!/usr/bin/env bash\n# The local PostgreSQL\nset -euo pipefail", 2)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "set -euo pipefail\n# shellcheck disable=SC2086\necho $a", 0)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "# shellcheck source=scripts/.internal/checkout-env.sh\nsource x", 0)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "# shellcheck disable=SC2086 because\necho $a", 1)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "echo up # start it", 1)]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh", "echo \"# not a comment\" '# nor this' ${#list[@]} $#", 0)]
    [InlineData(".githooks/pre-commit", "#!/usr/bin/env bash\n# Before a commit\nset -e", 2)]
    [InlineData(".github/workflows/ci.yml", "jobs:\n  check:\n    runs-on: ubuntu-24.04 # pinned", 3)]
    [InlineData(".github/workflows/ci.yml", "name: ci\n# the gate\non: push", 2)]
    [InlineData(".github/workflows/ci.yml", "run: echo \"a # b\"\nname: 'c # d'", 0)]
    [InlineData(".gitignore", "**/bin/\n# generated\n**/obj/", 2)]
    [InlineData(".gitattributes", "*.png binary\n# Line endings\n* text=auto", 2)]
    [InlineData(".editorconfig", "[*.cs]\n# AC-" + "notes-1\ndotnet_diagnostic.CA1000.severity = error", 0)]
    [InlineData(".editorconfig", "[*.cs]\n# Reason: formatting is CSharpier's job\ndotnet_diagnostic.IDE0055.severity = none", 2)]
    [InlineData(".editorconfig", "[*.cs]\n; old style\nroot = true", 2)]
    [InlineData("Directory.Build.rsp", "# no idle workers\n-nodeReuse:false", 1)]
    [InlineData("justfile", "# Format the code\n[group('maintenance')]\nfmt:\n    dotnet csharpier format .", 0)]
    [InlineData("justfile", "# The whole gate\ncheck *args:\n    @./check.sh", 0)]
    [InlineData("justfile", "# The local stack\nmod tmux-dev", 0)]
    [InlineData("justfile", "# Entry point for humans\n\nset shell := [\"bash\"]", 1)]
    [InlineData("justfile", "# Entry point for humans\n# Format the code\nfmt:\n    x", 1)]
    [InlineData("justfile", "# hidden\n[private]\ndefault:\n    @just --list", 1)]
    [InlineData("justfile", "fmt:\n    # format it\n    x", 2)]
    public void The_rules_find_a_comment_in_a_hash_comment_file(string path, string text, int line) =>
        Assert.Equal(line, CommentsIn(path, text).Select(comment => comment.Line).FirstOrDefault());
}
