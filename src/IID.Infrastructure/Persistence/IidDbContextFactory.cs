using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IID.Infrastructure.Persistence;

/// <summary>
/// Design-time factory that bypasses the IID.Api host entirely.
///
/// <para>
/// When the EF Core CLI runs tools (<c>migrations add</c>, <c>migrations list</c>,
/// <c>database update</c>…) it spins up <c>startup-project</c>'s host to discover
/// the DbContext. That host fails today because <c>IActivityRepository</c> (and
/// likely other Application-layer services) are not registered, even though they
/// have nothing to do with schema discovery.
/// </para>
///
/// <para>
/// By implementing <see cref="IDesignTimeDbContextFactory{TContext}"/> we tell EF:
/// "don't try to build the host, call me instead" — no DI graph, no mediator, no
/// SignalR. The factory loads the real connection string from the same source
/// the host would, so migration output matches what <see cref="IidDbInitializer"/>
/// will run at runtime.
/// </para>
/// </summary>
public sealed class IidDbContextFactory : IDesignTimeDbContextFactory<IidDbContext>
{
    public IidDbContext CreateDbContext(string[] args)
    {
        // Resolve API project regardless of where the CLI is invoked from.
        var apiProject = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "src", "IID.Api"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProject)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("IID")
            ?? throw new InvalidOperationException(
                "Connection string 'IID' is not configured. " +
                "Ensure src/IID.Api/appsettings.json (or environment variables) " +
                "contains ConnectionStrings:IID for SQL Server.");

        var optionsBuilder = new DbContextOptionsBuilder<IidDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(IidDbContext).Assembly.GetName().Name));

        // DomainEventDispatchInterceptor is registered via AddDbContext at runtime;
        // it is irrelevant for design-time tooling, so pass null.
        return new IidDbContext(optionsBuilder.Options, domainEventInterceptor: null);
    }
}
