using System.ComponentModel;
using System.Diagnostics;

namespace Suwbackkit.Cli;

public static class WebPage
{
    public const string NoBrowser = "No browser could be opened: open the address above.";

    public static async Task ShowAsync(Uri page, TextWriter output, TextWriter error, Action<Uri>? openBrowser = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        await output.WriteLineAsync(page.ToString());
        try
        {
            (openBrowser ?? OpenInDefaultBrowser)(page);
        }
        catch (Win32Exception)
        {
            await error.WriteLineAsync(NoBrowser);
        }
    }

    private static void OpenInDefaultBrowser(Uri page) =>
        Process.Start(new ProcessStartInfo(page.ToString()) { UseShellExecute = true })?.Dispose();
}
