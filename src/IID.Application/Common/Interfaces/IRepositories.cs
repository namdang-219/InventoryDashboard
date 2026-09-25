using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using IID.Domain.Dealerships;

namespace IID.Application.Common.Interfaces;

public interface IDealershipRepository
{
    Task<Dealership?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Dealership>> ListAsync(CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct);
    Task AddAsync(Dealership dealership, CancellationToken ct);
    void Update(Dealership dealership);
}

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> VinExistsAsync(string vin, CancellationToken ct);
    Task<bool> StockNumberExistsAsync(string stockNumber, CancellationToken ct);
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    void Update(Vehicle vehicle);
    void Remove(Vehicle vehicle);
    Task<(IReadOnlyList<Vehicle> Items, int Total)> ListAsync(VehicleListFilter filter, CancellationToken ct = default);
}

public interface IVehicleActionRepository
{
    Task AddAsync(Domain.VehicleActions.VehicleAction action, CancellationToken ct);
    void Remove(Domain.VehicleActions.VehicleAction action);
    Task<(IReadOnlyList<Domain.VehicleActions.VehicleAction> Items, int Total)> ListAsync(
        VehicleActionListFilter filter, CancellationToken ct = default);
    Task<(IReadOnlyList<Domain.VehicleActions.VehicleAction> Items, string? NextCursor, bool HasMore, int Total)> ListByCursorAsync(
        string? cursor, int limit, CancellationToken ct);
}

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all tracked changes to the database.
    /// </summary>
    Task<Result<int>> SaveChangesAsync(CancellationToken ct);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUser
{
    string? Id { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsInRole(string role);
}

public interface IUserDisplayNameProvider
{
    Task<string> GetDisplayNameAsync(string userId, CancellationToken ct = default);
}

public interface IVehicleHubNotifier
{
    Task VehicleAddedAsync(Vehicle v, CancellationToken ct);
    Task VehicleUpdatedAsync(Vehicle v, CancellationToken ct);
    Task VehicleRemovedAsync(Guid vehicleId, CancellationToken ct);
    Task VehicleAgingAsync(Vehicle v, CancellationToken ct);
    Task VehicleActionLoggedAsync(Domain.VehicleActions.VehicleAction a, Vehicle? v, CancellationToken ct);
    Task VehicleActionLoggedAsync(Domain.VehicleActions.VehicleAction a, CancellationToken ct);
    Task DashboardSummaryUpdatedAsync(DashboardSummaryDto summary, CancellationToken ct);
    Task DashboardAlertsUpdatedAsync(IReadOnlyList<DashboardAlertDto> alerts, CancellationToken ct);
    Task InventoryChangedAsync(CancellationToken ct);
}

public interface IUserActivityReadRepository
{
    Task<IReadOnlySet<string>> GetReadActivityIdsAsync(string userId, CancellationToken ct);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct);
    Task MarkAsReadAsync(string userId, IEnumerable<string> activityIds, CancellationToken ct);
    Task MarkAllAsReadAsync(string userId, CancellationToken ct);
}
