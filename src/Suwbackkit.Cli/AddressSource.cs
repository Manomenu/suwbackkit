namespace Suwbackkit.Cli;

public sealed record AddressSource(string Name, Func<string?> Value);
