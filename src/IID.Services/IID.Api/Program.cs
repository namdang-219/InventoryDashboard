using IID.Api.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSerilogWithOpenObserve();
    builder.Services.ConfigureServices(builder.Configuration);

    var app = builder.Build();

    await app.InitializeDatabaseAsync();
    app.MapApi();

    Log.Information("IID Backend API is ready and listening");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

