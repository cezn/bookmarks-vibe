using Auth;
using Auth.Components;
using Auth.Components.Account;
using Auth.NpgsqlStores;
using Auth.Services;
using Auth.Setup;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddCustomOpenApi(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.AddRedisClient("redis");
builder.AddNpgsql();
builder.Services.AddAndConfigureDataProtection(builder.Configuration.GetConnectionString("redis"));
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(o =>
    {
        o.ApplicationCookie!.Configure(cookieOptions =>
        {
            cookieOptions.Cookie.Path = "/";
            cookieOptions.Cookie.SameSite = SameSiteMode.Lax;
            cookieOptions.Cookie.Name = "bookmarks-auth";
            cookieOptions.Cookie.HttpOnly = true;
            cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
    });

// Get connection string for custom stores
var authConnectionString =
    builder.Configuration.GetConnectionString("authdb")
    ?? throw new InvalidOperationException("Connection string 'Auth' not found.");

builder
    .Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<ApplicationRole>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Register custom stores
builder.Services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("AuthMessageSenderOptions"));
builder.Services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>();

// builder.Services.AddTransient<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Register custom services for role management
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddCustomAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCustomForwardedHeaders(builder.Configuration.GetSection("ForwardedHeaders"));
app.UseStatusCodePagesWithReExecute("/status-{0}", createScopeForStatusCodePages: true);
app.UsePathBase("/auth"); // can be done with X-Forwarded-Prefix from YARP too
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapUsersEndpoints();
app.MapOpenApi().AllowAnonymous().RequireCors("AllowSwagger");
app.MapDefaultEndpoints();
app.Run();
