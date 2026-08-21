namespace Dashboard.Api.Contracts;

/// <summary>
/// Request body for POST /api/categories/{id}/entries. Uses a concrete
/// Dictionary rather than IReadOnlyDictionary -- safer for System.Text.Json
/// model binding, which needs a constructible type to deserialize into.
/// </summary>
public sealed record RecordEntriesRequest(DateOnly Month, Dictionary<int, decimal> Values);
