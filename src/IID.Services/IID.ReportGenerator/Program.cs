using IID.ReportGenerator.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.ConfigureSerilogWithOpenObserve();
    builder.Services.ConfigureReportGeneratorServices(builder.Configuration);

    var host = builder.Build();

    Log.Information("IID.ReportGenerator worker starting");
    await host.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "IID.ReportGenerator terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
