using Dashboard.Application.Metrics;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DashboardDbContext _dbContext;

    public EfUnitOfWork(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
