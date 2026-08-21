using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard.Application.Dashboard;
using Dashboard.Application.Metrics;
using Dashboard.Application.Settings;
using Dashboard.Application.Social;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Social;

namespace Dashboard.Wasm;

/// <summary>
/// Source-generated serialization for every type that crosses into
/// JavaScript.
///
/// This is generated rather than reflection-based for one concrete reason:
/// the published WebAssembly bundle is trimmed, and reflection-based
/// serialization is exactly the pattern the trimmer cannot see. Properties
/// would survive locally and silently vanish from the deployed build — the
/// worst possible failure shape, because it only appears in production.
/// Generating the serializers makes every type an explicit, statically
/// visible reference.
///
/// The options deliberately mirror <c>Program.cs</c>'s
/// <c>AddJsonOptions</c> on the API — camelCase names and enums as strings.
/// If these two ever disagree, the demo and the real API stop returning the
/// same payloads and the frontend's shared types become a lie.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(JsonStringEnumConverter<CategoryStatus>),
                  typeof(JsonStringEnumConverter<MetricStatus>),
                  typeof(JsonStringEnumConverter<MetricRatingTier>),
                  typeof(JsonStringEnumConverter<SocialCircleRating>),
                  typeof(JsonStringEnumConverter<KeyRelationshipKind>)])]
[JsonSerializable(typeof(DashboardSummary))]
[JsonSerializable(typeof(IReadOnlyList<Category>))]
[JsonSerializable(typeof(CategoryDetail))]
[JsonSerializable(typeof(IReadOnlyList<MetricTrendPoint>))]
[JsonSerializable(typeof(SocialSummary))]
[JsonSerializable(typeof(IReadOnlyList<AppSettingSummary>))]
[JsonSerializable(typeof(DemoError))]
[JsonSerializable(typeof(DemoFixtureInfo))]
[JsonSerializable(typeof(Dictionary<string, decimal>))]
internal sealed partial class DemoJsonContext : JsonSerializerContext;

/// <summary>The failure envelope. Carries a message and nothing else.</summary>
internal sealed record DemoError(bool Ok, string Error);

/// <summary>What the fixture loaded, for the console line and the smoke test.</summary>
internal sealed record DemoFixtureInfo(int Categories, int Metrics, int Months, int Friends);
