using Vantage.Application.Metrics;
using Vantage.Domain.Metrics;

namespace Vantage.Application.Tests.Metrics.Fakes;

public sealed class FakeCategoryRepository : ICategoryRepository
{
    private static readonly System.Reflection.PropertyInfo IdProperty =
        typeof(Category).GetProperty(nameof(Category.Id))!;

    private readonly Dictionary<int, Category> _categoriesById = new();

    /// <summary>
    /// Unlike <see cref="FakeMetricDefinitionRepository"/>, this fake does have
    /// to assign the real entity Id: <see cref="Vantage.Application.Dashboard.DashboardService"/>
    /// matches metrics to their category via <c>metric.CategoryId == category.Id</c>,
    /// so the seeded category's own Id has to actually match, not just the
    /// dictionary key it's stored under. Category's Id setter is private
    /// (EF-materialization-only in production), so it's set here via
    /// reflection rather than fighting the constructor.
    /// </summary>
    public void Seed(int id, Category category)
    {
        IdProperty.SetValue(category, id);
        _categoriesById[id] = category;
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(_categoriesById.Values.ToList());

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_categoriesById.GetValueOrDefault(id));

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        var nextId = _categoriesById.Count == 0 ? 1 : _categoriesById.Keys.Max() + 1;
        _categoriesById[nextId] = category;
        return Task.CompletedTask;
    }
}
