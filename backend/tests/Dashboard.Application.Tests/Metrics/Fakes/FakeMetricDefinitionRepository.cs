using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Tests.Metrics.Fakes;

/// <summary>
/// An in-memory stand-in for the real EF Core repository. Hand-rolled rather
/// than mocked — the interface is small enough that a fake is simpler to
/// read than a mocking framework's setup ceremony.
///
/// <see cref="MetricDefinition.Id"/> is only ever assigned by EF Core in real
/// usage, but callers like <c>DashboardService</c> and
/// <c>MetricEvaluationService</c> look metrics up (and re-evaluate them) by
/// that same Id, so the seeded entity's Id has to actually match the key
/// it's stored under here -- set via reflection since the setter is private.
/// </summary>
public sealed class FakeMetricDefinitionRepository : IMetricDefinitionRepository
{
    private static readonly System.Reflection.PropertyInfo IdProperty =
        typeof(MetricDefinition).GetProperty(nameof(MetricDefinition.Id))!;

    private readonly Dictionary<int, MetricDefinition> _metricDefinitionsById = new();

    public void Seed(int id, MetricDefinition metricDefinition)
    {
        IdProperty.SetValue(metricDefinition, id);
        _metricDefinitionsById[id] = metricDefinition;
    }

    public Task<IReadOnlyList<MetricDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricDefinition>>(_metricDefinitionsById.Values.ToList());

    public Task<MetricDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_metricDefinitionsById.GetValueOrDefault(id));

    public Task AddAsync(MetricDefinition metricDefinition, CancellationToken cancellationToken = default)
    {
        var nextId = _metricDefinitionsById.Count == 0 ? 1 : _metricDefinitionsById.Keys.Max() + 1;
        _metricDefinitionsById[nextId] = metricDefinition;
        return Task.CompletedTask;
    }
}
