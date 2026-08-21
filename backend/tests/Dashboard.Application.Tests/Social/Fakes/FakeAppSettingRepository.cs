using Dashboard.Application.Settings;
using Dashboard.Domain.Settings;

namespace Dashboard.Application.Tests.Social.Fakes;

public sealed class FakeAppSettingRepository : IAppSettingRepository
{
    private readonly Dictionary<string, AppSetting> _settingsByKey = new();

    public void Seed(AppSetting setting) => _settingsByKey[setting.Key] = setting;

    public Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settingsByKey.GetValueOrDefault(key));

    public Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppSetting>>(_settingsByKey.Values.ToList());

    public Task AddAsync(AppSetting setting, CancellationToken cancellationToken = default)
    {
        _settingsByKey[setting.Key] = setting;
        return Task.CompletedTask;
    }
}
