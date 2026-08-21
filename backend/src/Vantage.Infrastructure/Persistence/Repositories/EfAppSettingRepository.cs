using Microsoft.EntityFrameworkCore;
using Vantage.Application.Settings;
using Vantage.Domain.Settings;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfAppSettingRepository : IAppSettingRepository
{
    private readonly VantageDbContext _dbContext;

    public EfAppSettingRepository(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        _dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.AppSettings.ToListAsync(cancellationToken);

    public async Task AddAsync(AppSetting setting, CancellationToken cancellationToken = default) =>
        await _dbContext.AppSettings.AddAsync(setting, cancellationToken);
}
