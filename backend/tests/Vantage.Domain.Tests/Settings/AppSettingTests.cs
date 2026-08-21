using Vantage.Domain.Settings;

namespace Vantage.Domain.Tests.Settings;

public class AppSettingTests
{
    [Fact]
    public void Constructor_WithBlankKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AppSetting(" ", "12"));
    }

    [Fact]
    public void UpdateValue_ReplacesTheValue()
    {
        var setting = new AppSetting("ActiveCircleThresholdMonths", "12");

        setting.UpdateValue("18");

        Assert.Equal("18", setting.Value);
    }
}
