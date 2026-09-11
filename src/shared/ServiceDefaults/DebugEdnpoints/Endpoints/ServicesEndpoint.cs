using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class ServicesEndpoint
{
    public static async Task HandleAsync(HttpContext context, IServiceProvider services)
    {
        var descriptors = GetDescriptors(services);

        var htmlBuilder = new System.Text.StringBuilder(
            """
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset=""utf-8"">
            <style>
            body { font-family: system-ui, sans-serif; margin: 2rem; background: #f8f9fa; }
            h1 { color: #333; }
            .count { color: #6c757d; margin-bottom: 1rem; }
            table { width: 100%; border-collapse: collapse; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
            th { background: #0d6efd; color: #fff; padding: 10px 14px; text-align: left; }
            td { padding: 8px 14px; border-bottom: 1px solid #dee2e6; }
            tr:hover td { background: #f1f5f9; }
            .scope-singleton { color: #0d6efd; font-weight: 600; }
            .scope-scoped { color: #198754; font-weight: 600; }
            .scope-transient { color: #fd7e14; font-weight: 600; }
            .type-name { font-family: 'Consolas', monospace; font-size: 0.85rem; max-width: 500px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
            .type-name:hover { white-space: normal; overflow: visible; }
            </style>
            </head>
            <body>
            <h1>Registered Services</h1>
            """
        );

        if (descriptors != null)
        {
            var list = descriptors
                .OrderBy(d => d.ServiceType.FullName ?? d.ServiceType.Name)
                .ThenBy(d => d.ImplementationType?.FullName ?? string.Empty)
                .ToList();

            htmlBuilder.Append($"<p class=\"count\">{list.Count} service(s) registered</p>");
            htmlBuilder.Append(
                """
                <table>
                <tr><th>Scope</th><th>Service Type</th><th>Implementation</th></tr>
                """
            );

            foreach (var d in list)
            {
                var scopeClass = d.Lifetime switch
                {
                    ServiceLifetime.Singleton => "scope-singleton",
                    ServiceLifetime.Scoped => "scope-scoped",
                    ServiceLifetime.Transient => "scope-transient",
                    _ => "",
                };

                var serviceType = d.ServiceType.FullName ?? d.ServiceType.Name;
                var implType =
                    d.ImplementationType?.FullName
                    ?? (
                        d.ImplementationInstance != null
                            ? d.ImplementationInstance.GetType().FullName ?? "Instance"
                        : d.ImplementationFactory != null ? "Factory delegate"
                        : "Unknown"
                    );

                htmlBuilder.Append(
                    $"""
                    <tr>
                    <td><span class=""{scopeClass}"">{d.Lifetime}</span></td>
                    <td class=""type-name"" title=""{serviceType}"">{serviceType}</td>
                    <td class=""type-name"" title=""{implType}"">{implType}</td>
                    </tr>
                    """
                );
            }

            htmlBuilder.Append("</table>");
        }
        else
        {
            htmlBuilder.Append("<p>Unable to resolve service descriptors from the container.</p>");
        }

        htmlBuilder.Append("</body></html>");

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(htmlBuilder.ToString());
    }

    static IEnumerable<ServiceDescriptor>? GetDescriptors(IServiceProvider provider)
    {
        // Access descriptors via RootProvider.CallSiteFactory.Descriptors.
        var rootProvider = provider
            .GetType()
            .GetProperty(
                "RootProvider",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            )
            ?.GetValue(provider);

        if (rootProvider == null)
            return null;

        var callSiteFactory = rootProvider
            .GetType()
            .GetProperty(
                "CallSiteFactory",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            )
            ?.GetValue(rootProvider);

        if (callSiteFactory == null)
            return null;

        return callSiteFactory
                .GetType()
                .GetProperty(
                    "Descriptors",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                )
                ?.GetValue(callSiteFactory) as IEnumerable<ServiceDescriptor>;
    }
}
