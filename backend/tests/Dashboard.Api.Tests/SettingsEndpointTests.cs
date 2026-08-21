using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard.Application.Settings;

namespace Dashboard.Api.Tests;

public class SettingsEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetAll_WithNothingStored_ReturnsEveryKnownSettingAtItsDefault()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var settings = await factory.CreateClient().GetFromJsonAsync<AppSettingSummary[]>("/api/settings", JsonOptions);

        Assert.NotNull(settings);
        Assert.Equal(KnownAppSettings.All.Count, settings!.Length);
        Assert.Contains(settings, s => s.Key == KnownAppSettings.ActiveCircleThresholdMonths.Key && s.Value == "12");
    }

    [Fact]
    public async Task Update_PersistsAndIsReflectedInAFollowUpGet()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/settings/{KnownAppSettings.ActiveCircleThresholdMonths.Key}", new { value = "6" }, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var settings = await client.GetFromJsonAsync<AppSettingSummary[]>("/api/settings", JsonOptions);
        var setting = settings!.Single(s => s.Key == KnownAppSettings.ActiveCircleThresholdMonths.Key);
        Assert.Equal("6", setting.Value);
    }

    [Fact]
    public async Task Update_WithAnUnknownKey_ReturnsNotFound()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var response = await factory.CreateClient().PutAsJsonAsync(
            "/api/settings/NotARealSetting", new { value = "1" }, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithANonIntegerValue_ReturnsBadRequest()
    {
        await using var factory = new SqliteWebApplicationFactory();
        await factory.InitializeDatabaseAsync(_ => { });

        var response = await factory.CreateClient().PutAsJsonAsync(
            $"/api/settings/{KnownAppSettings.ActiveCircleThresholdMonths.Key}", new { value = "not-a-number" }, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
