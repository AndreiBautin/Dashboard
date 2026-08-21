using Dashboard.Application.Settings;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Application.Tests.Social.Fakes;
using Dashboard.Domain.Settings;

namespace Dashboard.Application.Tests.Settings;

public class SettingsServiceTests
{
    private readonly FakeAppSettingRepository _appSettings = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private SettingsService CreateService() => new(_appSettings, _unitOfWork);

    [Fact]
    public async Task GetAllAsync_WithNothingStored_ReturnsEveryKnownSettingAtItsDefault()
    {
        var settings = await CreateService().GetAllAsync();

        Assert.Equal(KnownAppSettings.All.Count, settings.Count);
        Assert.All(settings, s => Assert.Equal(s.DefaultValue, s.Value));
    }

    [Fact]
    public async Task GetAllAsync_WithAStoredOverride_ReflectsItRatherThanTheDefault()
    {
        _appSettings.Seed(new AppSetting(KnownAppSettings.ActiveCircleThresholdMonths.Key, "6"));

        var settings = await CreateService().GetAllAsync();

        var setting = settings.Single(s => s.Key == KnownAppSettings.ActiveCircleThresholdMonths.Key);
        Assert.Equal("6", setting.Value);
        Assert.Equal("12", setting.DefaultValue);
    }

    [Fact]
    public async Task SetAsync_WithAnUnknownKey_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().SetAsync("NotARealSetting", "1"));
    }

    [Fact]
    public async Task SetAsync_WithANonIntegerValue_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().SetAsync(KnownAppSettings.ActiveCircleThresholdMonths.Key, "not-a-number"));
    }

    [Fact]
    public async Task SetAsync_WithADecimalValueKindSetting_AllowsAFractionalValue()
    {
        await CreateService().SetAsync(KnownAppSettings.ArmMeasurementTier1Max.Key, "13.5");

        var stored = await _appSettings.GetAsync(KnownAppSettings.ArmMeasurementTier1Max.Key);
        Assert.Equal("13.5", stored!.Value);
    }

    [Fact]
    public async Task SetAsync_WithADecimalValueKindSetting_RejectsANonNumericValue()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().SetAsync(KnownAppSettings.ArmMeasurementTier1Max.Key, "not-a-number"));
    }

    [Fact]
    public async Task SetAsync_WithNoExistingRow_CreatesOne()
    {
        await CreateService().SetAsync(KnownAppSettings.SocialCircleThinMax.Key, "2");

        var stored = await _appSettings.GetAsync(KnownAppSettings.SocialCircleThinMax.Key);
        Assert.Equal("2", stored!.Value);
        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task SetAsync_WithAnExistingRow_UpdatesInPlace()
    {
        _appSettings.Seed(new AppSetting(KnownAppSettings.SocialCircleThinMax.Key, "4"));

        await CreateService().SetAsync(KnownAppSettings.SocialCircleThinMax.Key, "3");

        var stored = await _appSettings.GetAsync(KnownAppSettings.SocialCircleThinMax.Key);
        Assert.Equal("3", stored!.Value);
    }
}
