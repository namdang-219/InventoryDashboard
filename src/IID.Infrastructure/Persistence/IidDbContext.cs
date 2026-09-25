using IID.Domain.Dealerships;
using IID.Domain.Notifications;
using IID.Infrastructure.Identity;
using IID.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace IID.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the IID application. Inherits from
/// <c>IdentityDbContext&lt;ApplicationUser&gt;</c> so the Identity user store resolves
/// <see cref="ApplicationUser"/> directly (without this generic parameter, ASP.NET Identity
/// would try to materialize <c>IdentityUser</c> and the UserManager would fail at runtime).
/// </summary>
public sealed class IidDbContext(
    DbContextOptions<IidDbContext> options,
    DomainEventDispatchInterceptor? domainEventInterceptor = null) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Dealership> Dealerships => Set<Dealership>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAction> VehicleActions => Set<VehicleAction>();
    public DbSet<UserActivityReadStatus> UserActivityReadStatuses => Set<UserActivityReadStatus>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (domainEventInterceptor is not null)
            optionsBuilder.AddInterceptors(domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(typeof(IidDbContext).Assembly);
    }
}
