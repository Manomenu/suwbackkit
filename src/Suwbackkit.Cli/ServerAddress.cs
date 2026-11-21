namespace Suwbackkit.Cli;

public static class ServerAddress
{
    public const string DefaultSource = "The default address";

    public static AddressChoice Choose(IEnumerable<AddressSource> sources, string fallback)
    {
        ArgumentNullException.ThrowIfNull(sources);
        foreach (var source in sources)
        {
            var value = source.Value();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return Parse(value, source.Name, fallback);
            }
        }
        return Parse(fallback, DefaultSource, fallback);
    }

    public static AddressChoice Parse(string text, string source, string example)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Uri.TryCreate(text.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var address) && address.Scheme is "http" or "https"
            ? new AddressChoice(address, null)
            : new AddressChoice(null, $"{source} is '{text}', not an http or https address. Give one like {example}.");
    }

    public static string Display(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return address.ToString().TrimEnd('/');
    }

    public static Uri Api(Uri site) => new(site, "api/");
}
