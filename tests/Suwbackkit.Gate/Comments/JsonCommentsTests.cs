namespace Suwbackkit.Gate.Comments;

public sealed class JsonCommentsTests
{
    private static bool IsJson(string path) =>
        path.EndsWith(".json", StringComparison.Ordinal) || path.EndsWith(".jsonc", StringComparison.Ordinal);

    private static IEnumerable<FoundComment> CommentsIn(string path, string text) => CommentLexer.Scan(text, "//", "\"");

    [Fact]
    public void No_JSON_file_holds_a_comment() => CommentFiles.AssertNone(CommentFiles.Matching(IsJson), CommentsIn);

    [Theory]
    [InlineData(".vscode/settings.json", "{ \"a\": 1 }", 0)]
    [InlineData(".vscode/settings.json", "{\n  // only points at the projects\n  \"a\": 1\n}", 2)]
    [InlineData(".vscode/settings.json", "{ \"a\": 1 /* a block */ }", 1)]
    [InlineData(".vscode/settings.json", "{ \"$schema\": \"https://example.com/x.json\", \"b\": \"/* no */\" }", 0)]
    [InlineData(".vscode/settings.json", "{ \"a\": \"say \\\"// no\\\"\" }", 0)]
    [InlineData(".vscode/settings.json", "{\n  // AC-" + "notes-1\n  \"a\": 1\n}", 2)]
    public void The_rules_find_a_comment_in_JSON(string path, string text, int line) =>
        Assert.Equal(line, CommentsIn(path, text).Select(comment => comment.Line).FirstOrDefault());
}
