using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Vantage.Demo;

namespace Vantage.Wasm;

/// <summary>
/// The browser-side equivalent of <c>Vantage.Api</c>'s controllers.
///
/// Every method here mirrors one HTTP endpoint: same operation, same
/// arguments, same JSON payload. A controller turns an HTTP request into a
/// service call and serializes the result; these turn a JavaScript call into
/// the *same* service call and serialize the result the same way, with the
/// same naming policy and the same enum handling. That symmetry is the whole
/// point — the React app's two data adapters return identical payloads, so
/// nothing above the data layer can tell which one is in use.
///
/// Failures are returned, not thrown. A managed exception crossing the
/// interop boundary surfaces in JavaScript as an opaque runtime error with no
/// usable message, which would lose the validation messages the application
/// deliberately writes for users. Returning an envelope lets the adapter
/// rebuild a normal <c>Error</c> with the real text, matching what the
/// fetch-based adapter does with a non-OK response body.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class DemoApi
{
    private static DemoWorkspace? _workspace;

    /// <remarks>
    /// One workspace for the page's lifetime. It holds the in-memory store,
    /// so recreating it would silently discard whatever the visitor had
    /// entered.
    /// </remarks>
    private static DemoWorkspace Workspace => _workspace ??= DemoWorkspace.Create(Today);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Builds the in-memory application and reports what the fixture loaded,
    /// giving the page something to log and the deployment smoke test
    /// something unambiguous to assert against.
    /// </summary>
    [JSExport]
    internal static string Initialize() => Guard(() =>
    {
        var store = Workspace.Store;
        return Ok(JsonSerializer.Serialize(
            new DemoFixtureInfo(store.CategoryCount, store.MetricDefinitionCount, store.MonthCount, store.FriendCount),
            DemoJsonContext.Default.DemoFixtureInfo));
    });

    /// <summary>Discards anything the visitor changed and reseeds the fixture.</summary>
    [JSExport]
    internal static string Reset() => Guard(() =>
    {
        Workspace.Reset(Today);
        return OkEmpty();
    });

    // -- Reads ------------------------------------------------------------

    [JSExport]
    internal static string GetDashboard() => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.Dashboard.GetSummaryAsync()), DemoJsonContext.Default.DashboardSummary)));

    [JSExport]
    internal static string GetCategories() => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.CategoryRepository.GetAllAsync()), DemoJsonContext.Default.IReadOnlyListCategory)));

    [JSExport]
    internal static string GetCategoryDetail(int categoryId) => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.CategoryDetail.GetDetailAsync(categoryId)), DemoJsonContext.Default.CategoryDetail)));

    [JSExport]
    internal static string GetMetricTrend(int metricDefinitionId) => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.MetricTrend.GetTrendAsync(metricDefinitionId)), DemoJsonContext.Default.IReadOnlyListMetricTrendPoint)));

    [JSExport]
    internal static string GetSocial() => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.Social.GetSummaryAsync()), DemoJsonContext.Default.SocialSummary)));

    [JSExport]
    internal static string GetSocialTrend() => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.Social.GetTrendAsync()), DemoJsonContext.Default.IReadOnlyListMetricTrendPoint)));

    [JSExport]
    internal static string GetSettings() => Guard(() => Ok(JsonSerializer.Serialize(
        Await(Workspace.Settings.GetAllAsync()), DemoJsonContext.Default.IReadOnlyListAppSettingSummary)));

    // -- Writes -----------------------------------------------------------

    /// <param name="valuesJson">
    /// A JSON object of metric id to value, matching the wire shape of
    /// <c>RecordEntriesRequest.Values</c>. Passed as a string rather than a
    /// marshalled dictionary because JS interop marshals only a small set of
    /// primitives and arrays — sending JSON keeps this boundary byte-identical
    /// to the HTTP one.
    /// </param>
    [JSExport]
    internal static string RecordEntries(int categoryId, string month, string valuesJson) => Guard(() =>
    {
        var raw = JsonSerializer.Deserialize(valuesJson, DemoJsonContext.Default.DictionaryStringDecimal)
            ?? throw new ArgumentException("Entry values were not valid JSON.");

        var values = new Dictionary<int, decimal>();
        foreach (var (key, value) in raw)
        {
            if (!int.TryParse(key, out var metricDefinitionId))
            {
                throw new ArgumentException($"\"{key}\" is not a valid metric id.");
            }

            values[metricDefinitionId] = value;
        }

        Await(Workspace.MetricEntry.RecordEntriesAsync(categoryId, ParseMonth(month), values));
        return OkEmpty();
    });

    [JSExport]
    internal static string AddFriend(string name, string lastHangoutDate, string? notes) => Guard(() =>
    {
        Await(Workspace.Friends.AddFriendAsync(name, ParseDate(lastHangoutDate), notes));
        return OkEmpty();
    });

    [JSExport]
    internal static string LogHangout(int friendId, string date) => Guard(() =>
    {
        Await(Workspace.Friends.LogHangoutAsync(friendId, ParseDate(date)));
        return OkEmpty();
    });

    [JSExport]
    internal static string LogKeyRelationshipContact(int keyRelationshipId, string date) => Guard(() =>
    {
        Await(Workspace.KeyRelationships.LogContactAsync(keyRelationshipId, ParseDate(date)));
        return OkEmpty();
    });

    [JSExport]
    internal static string UpdateSetting(string key, string value) => Guard(() =>
    {
        Await(Workspace.Settings.SetAsync(key, value));
        return OkEmpty();
    });

    // -- Plumbing ---------------------------------------------------------

    /// <remarks>
    /// Wrapping an already-serialized payload by hand rather than serializing
    /// a generic envelope type: the source generator needs closed types, and
    /// there is one closed envelope per payload otherwise. The interpolation
    /// is safe because <paramref name="payloadJson"/> is always serializer
    /// output, never user text.
    /// </remarks>
    private static string Ok(string payloadJson) => $"{{\"ok\":true,\"data\":{payloadJson}}}";

    /// <summary>For the writes, which have no payload beyond "it worked".</summary>
    private static string OkEmpty() => "{\"ok\":true,\"data\":null}";

    /// <remarks>
    /// Returns the exception's message and nothing else — no stack trace, no
    /// type name. The application's own messages are already written for a
    /// user ("\"abc\" is not a valid whole number for ..."); anything beyond
    /// that would leak internals into a public page for no benefit.
    /// </remarks>
    private static string Guard(Func<string> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception exception)
        {
            return JsonSerializer.Serialize(new DemoError(false, exception.Message), DemoJsonContext.Default.DemoError);
        }
    }

    /// <remarks>
    /// Every call here completes synchronously: the in-memory repositories
    /// return already-completed tasks, so this never actually blocks. It
    /// would be unsafe against a real I/O-bound repository, which is why this
    /// façade exists only in the demo host and never in the API.
    /// </remarks>
    private static T Await<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private static void Await(Task task) => task.GetAwaiter().GetResult();

    /// <summary>
    /// The entry form sends a review month as "YYYY-MM"; other callers send a
    /// full date. Accept both rather than making the caller pad it.
    /// </summary>
    private static DateOnly ParseMonth(string month) =>
        DateOnly.TryParse(month, out var parsed) ? parsed
        : DateOnly.TryParse($"{month}-01", out var padded) ? padded
        : throw new ArgumentException($"\"{month}\" is not a valid month.");

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, out var parsed)
            ? parsed
            : throw new ArgumentException($"\"{value}\" is not a valid date.");
}
