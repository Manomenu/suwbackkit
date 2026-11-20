using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Suwbackkit.Server;

public static class ContractSchemas
{
    public static OpenApiOptions AddContractSchemas(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer(TellUnionsAndEnumsAsync);
        options.AddSchemaTransformer(RequireWrittenPropertiesAsync);
        return options;
    }

    private static Task TellUnionsAndEnumsAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var schemas = document.Components?.Schemas?.Values.OfType<OpenApiSchema>().ToList() ?? [];
        foreach (var listed in schemas.Where(schema => schema.Enum?.Any(value => value is null) == true))
        {
            listed.Enum = [.. listed.Enum?.Where(value => value is not null) ?? []];
        }
        foreach (var union in schemas.Where(schema => schema.Discriminator is not null))
        {
            RequireDiscriminator(document, union.Discriminator?.PropertyName ?? "", union.Discriminator?.Mapping?.Values ?? []);
        }
        return Task.CompletedTask;
    }

    private static void RequireDiscriminator(OpenApiDocument document, string told, IEnumerable<OpenApiSchemaReference> variants)
    {
        foreach (var variant in variants)
        {
            if (
                document.Components?.Schemas?.TryGetValue(variant.Reference.Id ?? "", out var target) == true
                && target is OpenApiSchema shape
            )
            {
                shape.Required ??= new HashSet<string>(StringComparer.Ordinal);
                shape.Required.Add(told);
            }
        }
    }

    private static Task RequireWrittenPropertiesAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.JsonPropertyInfo is not null || context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object || schema.Properties is null)
        {
            return Task.CompletedTask;
        }
        foreach (var property in context.JsonTypeInfo.Properties.Where(AlwaysWritten))
        {
            if (schema.Properties.TryGetValue(property.Name, out var shape) && shape is OpenApiSchema written && written.Type is { } type)
            {
                written.Type = type & ~JsonSchemaType.Null;
                schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
                schema.Required.Add(property.Name);
            }
        }
        return Task.CompletedTask;
    }

    private static bool AlwaysWritten(JsonPropertyInfo property) =>
        property is { Set: null, AssociatedParameter: null, ShouldSerialize: null, Get: not null }
        && property.AttributeProvider is PropertyInfo info
        && !info.PropertyType.IsValueType
        && new NullabilityInfoContext().Create(info).ReadState == NullabilityState.NotNull;
}
