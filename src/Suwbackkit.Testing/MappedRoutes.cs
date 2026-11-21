using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Suwbackkit.Testing;

public static partial class MappedRoutes
{
    public static IReadOnlyList<string> Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return
        [
            .. services
                .GetRequiredService<EndpointDataSource>()
                .Endpoints.OfType<RouteEndpoint>()
                .SelectMany(endpoint =>
                    (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []).Select(method =>
                        $"{method} {WithoutConstraints(endpoint.RoutePattern.RawText ?? "")}"
                    )
                )
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }

    internal static string WithoutConstraints(string pattern) => Constraint().Replace(pattern, "{$1}");

    [GeneratedRegex(@"\{(\w+):[^}]+\}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Constraint();
}
