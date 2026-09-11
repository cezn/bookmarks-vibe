using System.Text.Json.Nodes;
using BookmarksApi.Shared;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public class CaseInsensitiveSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var type = context.ParameterDescription?.Type ?? context.JsonTypeInfo.Type;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CaseInsensitive<>))
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = Enum.GetNames(type.GetGenericArguments().Single())
                .Select(name => JsonValue.Create(name) as JsonNode)
                .ToList();
        }

        return Task.CompletedTask;
    }
}
