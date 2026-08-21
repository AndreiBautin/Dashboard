namespace Dashboard.Application.Metrics;

/// <summary>
/// The only place Application code touches "commit to the database" —
/// keeps Application decoupled from EF Core's DbContext.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
