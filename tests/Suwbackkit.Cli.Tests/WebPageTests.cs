using System.ComponentModel;

namespace Suwbackkit.Cli.Tests;

public sealed class WebPageTests
{
    private static readonly Uri Page = new("http://cms.internal/bots/one");

    [Fact]
    public async Task The_page_is_printed_and_opened()
    {
        var opened = new List<Uri>();
        using var output = new StringWriter();
        using var error = new StringWriter();

        await WebPage.ShowAsync(Page, output, error, opened.Add);

        Assert.Equal(Page, Assert.Single(opened));
        Assert.Equal(Page + Environment.NewLine, output.ToString());
        Assert.Equal("", error.ToString());
    }

    [Fact]
    public async Task With_no_browser_the_printed_address_stays_and_the_error_says_to_open_it()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        await WebPage.ShowAsync(Page, output, error, _ => throw new Win32Exception("xdg-open not found"));

        Assert.Contains(Page.ToString(), output.ToString(), StringComparison.Ordinal);
        Assert.Equal(WebPage.NoBrowser + Environment.NewLine, error.ToString());
    }
}
