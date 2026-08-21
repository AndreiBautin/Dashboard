using Dashboard.Domain.Settings;

namespace Dashboard.Application.Settings;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AppSetting setting, CancellationToken cancellationToken = default);
}
