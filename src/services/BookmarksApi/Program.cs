using BookmarksApi.Bookmarks;
using BookmarksApi.FeatureFlags;
using BookmarksApi.Idempotency;
using BookmarksApi.Setup;
using BookmarksApi.Tags;
using OutboxsApi.Outbox;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.Configure<HostOptions>(opt =>
{
    opt.ServicesStartConcurrently = true;
    opt.ServicesStopConcurrently = true;
    opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});
builder.Services.Configure<RouteHandlerOptions>(x => x.ThrowOnBadRequest = true);
builder.Services.AddAndConfigureProblemDetails();
builder.Services.AddSingleton<UserIdProvider>();
builder.Services.AddOpenApi(); // for some reason it's needed here. Otherwise, xml docs are'nt picked up
builder.Services.AddCustomOpenApi(
    builder.Configuration,
    opt => opt.AddSchemaTransformer<CaseInsensitiveSchemaTransformer>()
);
builder.Services.AddValidation();
builder.Services.AddExceptionHandler<JsonExceptionHandler>();
builder.Services.AddCustomAuth(builder.Configuration.GetSection("Jwt"));
builder.Services.AddNpgsql(
    builder.Configuration.GetConnectionString("BookmarksRw"),
    builder.Configuration.GetConnectionString("BookmarksRo"),
    builder.Environment
);
builder.Services.AddFeatureFlags();
builder.Services.AddOutbox(builder.Configuration);
builder.Services.AddBookmarks(builder.Configuration);
builder.Services.AddTags(builder.Configuration);

var app = builder.Build();
app.UseRouting();
app.UseStatusCodePages();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();
app.MapBookmarkEndpoints();
app.MapTagEndpoints();
app.MapFeatureFlagsEndpoints();
app.MapOpenApi().AllowAnonymous().RequireCors("AllowSwagger");
app.MapDefaultEndpoints();
app.RunWithTrace();
