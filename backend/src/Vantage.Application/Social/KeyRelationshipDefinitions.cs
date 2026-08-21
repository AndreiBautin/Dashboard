using Vantage.Application.Settings;
using Vantage.Domain.Social;

namespace Vantage.Application.Social;

/// <summary>
/// The single registry of the fixed key relationships and which configured
/// threshold setting governs each -- adding a new one means one entry here
/// (plus a KnownAppSettings threshold and a seeded row), not scattered
/// switch statements across SocialService/DashboardService.
/// </summary>
public static class KeyRelationshipDefinitions
{
    public sealed record Definition(KeyRelationshipKind Kind, string Label, AppSettingDefinition ThresholdSetting);

    public static readonly IReadOnlyList<Definition> All =
    [
        new(KeyRelationshipKind.DateWithWife, "Date with Wife", KnownAppSettings.DateWithWifeThresholdMonths),
        new(KeyRelationshipKind.VisitedMother, "Visited Mother", KnownAppSettings.VisitedMotherThresholdMonths),
    ];
}
