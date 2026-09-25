using IID.Application.Common.Interfaces;
using IID.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace IID.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIidInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IClock, System.SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, Identity.HttpContextCurrentUser>();
        services.AddScoped<ITokenService, Services.TokenService>();
        services.AddScoped<IAuthService, Services.AuthService>();
        services.AddScoped<IUserDisplayNameProvider, Identity.UserDisplayNameProvider>();
        // IHubContext<InventoryHub, IInventoryClient> is registered automatically by AddSignalR() in the API layer.
        services.AddScoped<IVehicleHubNotifier, Realtime.SignalRVehicleHubNotifier>();

        // Domain events: scoped interceptor so SaveChanges can publish via MediatR.
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(IID.Application.Vehicles.Commands.CreateVehicle.CreateVehicleCommand).Assembly,
            typeof(IID.Domain.Vehicles.Vehicle).Assembly));

        return services;
    }
}
