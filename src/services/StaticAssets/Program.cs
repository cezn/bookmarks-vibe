var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapStaticAssets().ShortCircuit();
app.MapFallbackToFile("index.html").ShortCircuit();
app.MapDefaultEndpoints();
app.Run();
