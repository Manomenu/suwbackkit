using System.Reflection;

namespace Suwbackkit.Server;

public static class ServerKit
{
    public static string Version { get; } =
        typeof(ServerKit).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
}
