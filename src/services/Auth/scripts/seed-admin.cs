#!/usr/local/dotnet/dotnet

#:package ConsoleAppFramework
#:project ../Auth.csproj

using System.Diagnostics;
using Auth;
using Auth.NpgsqlStores;
using ConsoleAppFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceDefaults;

await ConsoleApp.RunAsync(args, SeedAdminAsync);

/// <summary>
/// Seeds the core admin user and assigns the admin role.
/// </summary>
/// <param name="connectionString">-c, Optional database connection string.</param>
static async Task SeedAdminAsync(
    string connectionString = "host=localhost;database=authdb;user id=postgres;password=secret;"
)
{
    const string scriptName = "SeedAdmin";
    const string adminRoleName = "admin";
    const string adminRoleId = "33862e21-fcec-5399-843f-c9d66cdf10c3";
    const string adminUserId = "8dfb53aa-dba8-5bb3-9a77-68994c4daab2";
    const string adminUserName = "admin@cezn.tech";

    var adminUserPassword =
        Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        ?? throw new InvalidOperationException("ADMIN_PASSWORD not set");

    Monitoring.ActivitySource = new ActivitySource(scriptName);

    var services = new ServiceCollection();
    services.AddLogging(x =>
    {
        x.AddSimpleConsole();
        x.AddFilter((category, level) => category == scriptName && level >= LogLevel.Information);
    });
    services.AddNpgsqlDataSource(connectionString);
    services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        .AddRoles<ApplicationRole>()
        .AddDefaultTokenProviders();
    services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
    services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
    services.AddDataProtection();

    using var sp = services.BuildServiceProvider();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(scriptName);
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

    logger.LogInformation("Checking for existing admin user...");
    var adminUser = await userManager.FindByNameAsync(adminUserName);
    if (adminUser is null)
    {
        logger.LogInformation("Creating admin user...");
        adminUser = new()
        {
            Id = adminUserId,
            UserName = adminUserName,
            Email = adminUserName,
            EmailConfirmed = true,
        };
        var createUserResult = await userManager.CreateAsync(adminUser, adminUserPassword);
        if (createUserResult.Succeeded is false)
        {
            var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }
    }

    logger.LogInformation("Checking if admin user is in admin role...");
    if (await userManager.IsInRoleAsync(adminUser, adminRoleName))
    {
        logger.LogInformation("Admin user already in admin role, skipping seeding.");
        return;
    }

    logger.LogInformation("Checking for existing admin role...");
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        logger.LogInformation("Creating admin role...");
        await roleManager.CreateAsync(new() { Id = adminRoleId, Name = adminRoleName });
    }

    logger.LogInformation("Adding admin role to user...");
    var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
    if (addToRoleResult.Succeeded)
        logger.LogInformation(
            "Admin user seeded successfully. {username}/{password}",
            adminUserName,
            adminUserPassword
        );
    else
    {
        var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to add admin user to admin role: {errors}");
    }
}
