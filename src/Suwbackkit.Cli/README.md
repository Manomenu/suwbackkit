# Suwbackkit.Cli

The skeleton the .NET command lines of Manomenu's apps share: a CLI that talks to a server built on `Suwbackkit.Server`. It holds
only what every such CLI needs and knows nothing of any app; the commands, their output, exit codes and configuration files stay
in the app. It depends on System.CommandLine and not on ASP.NET Core, so a CLI packed as a .NET tool needs no web framework.

- **`ServerAddress.Choose(sources, fallback)`** picks the server's address from named sources, in order: the first
  `AddressSource(name, value)` whose value is not blank wins, and the later ones are not read (a value is a function, so a file is
  read only when it is needed); with none it is the fallback. The address must be `http` or `https`, else the `AddressChoice` holds no
  address and a `Problem` that names the source and gives the fallback as an example. An address ends in one `/`, so paths resolve
  under it; `ServerAddress.Api(site)` is the address of the API one folder `api` below the site, `ServerAddress.Display(address)` the address without its last `/`.
- **`ServerRefusal.Read(body)`** reads the server's refusal, the format `Suwbackkit.Server`'s `Refusals.Refuse(...)` writes: its `Code`,
  `Title`, `Detail` and every `ServerMistake` with its `Code`, `Message`, `Fix`, `At`, and its whole `Json` for what an app adds to a
  mistake. A field that is missing or not text is `null`; an answer that is no JSON object is no refusal (`null`). How it is printed
  is the app's.
- **`WebPage.ShowAsync(page, output, error, openBrowser)`** prints the page's address and opens it in the default browser; with no
  browser the address stays printed and the error says to open it (`WebPage.NoBrowser`). Tests pass `openBrowser`.
- **`CommandTree`** walks a System.CommandLine tree: `Find(root, "facts show")` finds a command by its words, `All(root)` gives every
  command below the root (each before its own subcommands), `WordsOf(command)` the words that run it. A test that checks every
  command (its help, its plugin docs, its route) walks the tree instead of copying a list.

## Usage

```csharp
using Suwbackkit.Cli;

var choice = ServerAddress.Choose(
    [
        new AddressSource("--server", () => option),
        new AddressSource("MYAPP_URL", () => Environment.GetEnvironmentVariable("MYAPP_URL")),
        new AddressSource("myappUrl in myapp/config.yaml", () => config?.MyappUrl),
    ],
    "http://localhost:3000"
);
if (choice.Address is not { } site)
{
    await error.WriteLineAsync(choice.Problem);
    return 2;
}
using var http = new HttpClient { BaseAddress = ServerAddress.Api(site) };
using var response = await http.GetAsync(new Uri("things", UriKind.Relative));
if (!response.IsSuccessStatusCode && ServerRefusal.Read(await response.Content.ReadAsStringAsync()) is { } refusal)
{
    foreach (var mistake in refusal.Mistakes)
    {
        await error.WriteLineAsync($"{mistake.At}: {mistake.Message} {mistake.Fix}");
    }
}
```

A test that every route the server maps has a command, with `MappedRoutes` from `Suwbackkit.Testing`:

```csharp
var missing = MappedRoutes.Of(app.Services).Where(route => !Routes.ContainsKey(route));
var unknownCommands = Routes.Values.Where(words => CommandTree.Find(CliApp.Create(), words) is null);
```

Source, license and changes: https://github.com/Manomenu/suwbackkit
