namespace Dashboard.Domain.Metrics;

/// <summary>
/// A life area (Fitness, Finance, Social, ...). Categories are data, not an
/// enum — adding a future life area is a matter of inserting a row, not
/// shipping code.
/// </summary>
public sealed class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }

    // For EF Core materialization only.
    private Category()
    {
    }

    public Category(string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        Name = name;
        SortOrder = sortOrder;
    }
}
