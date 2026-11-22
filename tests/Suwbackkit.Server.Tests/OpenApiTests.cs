using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Suwbackkit.Server.Tests;

public sealed class OpenApiTests
{
    private const string HealthPath = """
        {
          "get": {
            "tags": ["HealthEndpoints"],
            "responses": {
              "200": {
                "description": "OK",
                "content": { "application/json": { "schema": { "$ref": "#/components/schemas/HealthStatus" } } }
              }
            }
          }
        }
        """;

    private const string HealthStatusSchema = """
        { "required": ["status"], "type": "object", "properties": { "status": { "type": "string" } } }
        """;

    [Fact]
    public async Task The_document_is_openapi_3_1()
    {
        var document = await DocumentAsync();

        Assert.StartsWith("3.1.", document["openapi"]?.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_document_written_at_build_time_is_openapi_3_1_too()
    {
        var targets = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Suwbackkit.Server.targets"));

        Assert.Contains(targets.Descendants("OpenApiGenerateDocumentsOptions"), options =>
            options.Value.EndsWith("--openapi-version OpenApi3_1", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task The_health_status_keeps_the_contract_the_apps_already_publish()
    {
        var document = await DocumentAsync();

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(HealthPath), document["paths"]?["/api/health"]), document.ToJsonString());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(HealthStatusSchema), document["components"]?["schemas"]?["HealthStatus"]));
    }

    [Fact]
    public async Task The_probes_stay_out_of_the_contract()
    {
        var paths = (await DocumentAsync())["paths"]?.AsObject().Select(path => path.Key);

        Assert.Equal(["/api/fail", "/api/health"], paths?.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_app_adds_its_own_openapi_options_on_top_of_the_defaults()
    {
        var document = await DocumentAsync(builder =>
            builder.Services.AddOpenApi(options =>
                options.AddDocumentTransformer(
                    (openApi, _, _) =>
                    {
                        openApi.Info.Title = "Custom";
                        return Task.CompletedTask;
                    }
                )
            )
        );

        Assert.Equal("Custom", document["info"]?["title"]?.GetValue<string>());
        Assert.StartsWith("3.1.", document["openapi"]?.GetValue<string>(), StringComparison.Ordinal);
        Assert.Equal(2, document["paths"]?.AsObject().Count);
    }

    [Fact]
    public void A_test_run_is_not_writing_the_document() => Assert.False(ServerDefaults.WritingOpenApiDocument);

    private static async Task<JsonNode> DocumentAsync(Action<WebApplicationBuilder>? configure = null)
    {
        await using var server = await TestApp.StartAsync(configure);
        var json = await server.Client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);
        return JsonNode.Parse(json, documentOptions: new JsonDocumentOptions()) ?? throw new InvalidOperationException(json);
    }
}
