using ServiceDefaults;
using SummarizeApi;
using SummarizeApi.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddCustomOpenApi(builder.Configuration);
builder.AddRedisDistributedCache("Redis");
builder.Services.AddHybridCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomAuth(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<Summarizer>();
builder.Services.AddSingleton<PageInfoLoader>();
builder.Services.AddCustomHttpClients(builder.Configuration);
builder.AddCustomOllama();

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapSummarizerEndpoints();
app.MapOpenApi().AllowAnonymous();
app.MapDefaultEndpoints();
app.Run();
