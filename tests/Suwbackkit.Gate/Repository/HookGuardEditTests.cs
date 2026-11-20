using System.Diagnostics;
using System.Text.Json;

namespace Suwbackkit.Gate.Repository;

public sealed class HookGuardEditTests
{
    private const string Script = "tests/Suwbackkit.Gate/scripts/hook-guard-edit.sh";

    private static (int ExitCode, string Errors) Run(string path, bool allowGateEdit = false)
    {
        var gitBash = @"C:\Program Files\Git\bin\bash.exe";
        var start = new ProcessStartInfo(File.Exists(gitBash) ? gitBash : "bash")
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            WorkingDirectory = RepositoryFiles.Root,
        };
        start.ArgumentList.Add(Script);
        start.Environment.Remove("ALLOW_GATE_EDIT");
        start.Environment.Remove("CLAUDE_PROJECT_DIR");
        if (allowGateEdit)
        {
            start.Environment["ALLOW_GATE_EDIT"] = "1";
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("bash did not start");
        process.StandardInput.Write(
            JsonSerializer.Serialize(
                new
                {
                    tool_name = "Edit",
                    tool_input = new { file_path = Path.IsPathRooted(path) ? path : $"{RepositoryFiles.Root}/{path}" },
                }
            )
        );
        process.StandardInput.Close();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, errors);
    }

    [Theory]
    [InlineData("tests/Suwbackkit.Gate/scripts/check.sh")]
    [InlineData(Script)]
    [InlineData("tests/Suwbackkit.Gate/Code/CommentsTests.cs")]
    [InlineData("tests/Suwbackkit.Gate/Suwbackkit.Gate.csproj")]
    [InlineData(".claude/settings.json")]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".editorconfig")]
    [InlineData("BannedSymbols.txt")]
    [InlineData("Directory.Build.props")]
    [InlineData("src/Directory.Build.props")]
    public void An_agent_cannot_edit_the_gate_and_is_told_to_ask_the_owner(string path)
    {
        var (exitCode, errors) = Run(path);

        Assert.Equal(2, exitCode);
        Assert.Contains("ALLOW_GATE_EDIT=1 claude", errors, StringComparison.Ordinal);
        Assert.Contains("ask the user", errors, StringComparison.Ordinal);
    }

    [Fact]
    public void The_owner_lets_a_session_edit_the_gate() =>
        Assert.Equal(0, Run("tests/Suwbackkit.Gate/scripts/check.sh", allowGateEdit: true).ExitCode);

    [Fact]
    public void A_lockfile_is_refused_with_the_command_that_regenerates_it()
    {
        var (exitCode, errors) = Run("src/Suwbackkit.Server/packages.lock.json");

        Assert.Equal(2, exitCode);
        Assert.Contains("dotnet restore", errors, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("src/Suwbackkit.Server/ServerKit.cs")]
    [InlineData("tests/Suwbackkit.Server.Tests/ServerKitTests.cs")]
    [InlineData("Directory.Packages.props")]
    [InlineData("AGENTS.md")]
    public void Everything_else_stays_editable(string path) => Assert.Equal(0, Run(path).ExitCode);

    [Theory]
    [InlineData("/elsewhere/other-repo/.editorconfig")]
    [InlineData("/elsewhere/other-repo/.claude/settings.json")]
    [InlineData("/elsewhere/other-repo/Directory.Build.props")]
    [InlineData("/elsewhere/other-repo/src/App/packages.lock.json")]
    public void Files_of_another_project_are_not_this_projects_gate(string path) => Assert.Equal(0, Run(path).ExitCode);
}
