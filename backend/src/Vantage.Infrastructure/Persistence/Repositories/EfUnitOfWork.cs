using Vantage.Application.Metrics;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly VantageDbContext _dbContext;

    public EfUnitOfWork(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
