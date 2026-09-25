using IID.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap logging before host is built so we see early binding errors.
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Single composition-root call — mirrors Sportcast's reference pattern.
builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

// Run migrations + seeders before the pipeline starts handling traffic.
await app.InitializeDatabaseAsync();

// Single pipeline-mapping call.
app.MapApi();

var url = builder.Configuration["Urls"] ?? "http://localhost:8080";
Log.Information("🚀 IID Backend API is ready and listening on {Url}", url);

app.Run(url);

// Visible to WebApplicationFactory in integration tests.
public partial class Program { }
