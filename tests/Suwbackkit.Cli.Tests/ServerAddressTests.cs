namespace Suwbackkit.Cli.Tests;

public sealed class ServerAddressTests
{
    private const string Fallback = "http://localhost:3000";

    private static AddressSource Given(string name, string? value) => new(name, () => value);

    private static AddressSource Unread(string name) => new(name, () => throw new InvalidOperationException($"{name} was read"));

    [Fact]
    public void The_first_source_with_a_value_wins_and_the_later_ones_are_not_read()
    {
        var choice = ServerAddress.Choose([Given("--server", "http://option"), Unread("the file")], Fallback);

        Assert.Equal(new Uri("http://option/"), choice.Address);
        Assert.Null(choice.Problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_source_with_no_value_passes_to_the_next(string? empty)
    {
        var choice = ServerAddress.Choose([Given("--server", empty), Given("SERVER_URL", "https://variable/")], Fallback);

        Assert.Equal(new Uri("https://variable/"), choice.Address);
    }

    [Fact]
    public void With_no_source_answering_it_is_the_fallback() =>
        Assert.Equal(new Uri(Fallback + "/"), ServerAddress.Choose([Given("--server", null)], Fallback).Address);

    [Theory]
    [InlineData("http://cms.internal:3330", "http://cms.internal:3330/")]
    [InlineData(" http://cms.internal/base// ", "http://cms.internal/base/")]
    public void An_address_ends_in_one_slash_so_paths_resolve_under_it(string text, string expected) =>
        Assert.Equal(new Uri(expected), ServerAddress.Parse(text, "--server", Fallback).Address);

    [Theory]
    [InlineData("localhost:3330")]
    [InlineData("ftp://files")]
    [InlineData("not an address")]
    public void An_address_that_is_not_http_or_https_names_its_source_and_an_example(string text)
    {
        var choice = ServerAddress.Choose([Given("SERVER_URL", text)], Fallback);

        Assert.Null(choice.Address);
        Assert.Equal($"SERVER_URL is '{text}', not an http or https address. Give one like {Fallback}.", choice.Problem);
    }

    [Fact]
    public void A_bad_fallback_is_named_as_the_default() =>
        Assert.StartsWith(ServerAddress.DefaultSource, ServerAddress.Choose([], "nowhere").Problem, StringComparison.Ordinal);

    [Fact]
    public void The_api_lives_under_api_and_the_display_drops_the_last_slash()
    {
        var site = new Uri("http://cms.internal/base/");

        Assert.Equal(new Uri("http://cms.internal/base/api/"), ServerAddress.Api(site));
        Assert.Equal("http://cms.internal/base", ServerAddress.Display(site));
    }
}
