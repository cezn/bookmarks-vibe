using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BookmarksApi.Setup;

static class ProblemDetailsExtensions
{
    public static IServiceCollection AddAndConfigureProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = (context) =>
            {
                if (context.ProblemDetails is HttpValidationProblemDetails validationProblem)
                {
                    context.ProblemDetails.Detail =
                        $"Error(s) occurred: {validationProblem.Errors.Values.Sum(x => x.Length)}";

                    var namingPolicy = context
                        .HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>()
                        .Value.SerializerOptions.PropertyNamingPolicy;

                    if (namingPolicy is not null)
                    {
                        validationProblem.Errors = validationProblem.Errors.ToDictionary(
                            kvp => namingPolicy.ConvertName(kvp.Key),
                            kvp => kvp.Value
                        );
                    }
                }
            };
        });

        return services;
    }
}
