using Vantage.Application.Social;
using Vantage.Domain.Social;

namespace Vantage.Application.Tests.Social.Fakes;

public sealed class FakeKeyRelationshipRepository : IKeyRelationshipRepository
{
    private static readonly System.Reflection.PropertyInfo IdProperty =
        typeof(KeyRelationship).GetProperty(nameof(KeyRelationship.Id))!;

    private readonly Dictionary<int, KeyRelationship> _relationshipsById = new();

    /// <summary>Assigns the given id via reflection, same rationale as FakeFriendRepository.Seed.</summary>
    public void Seed(int id, KeyRelationship relationship)
    {
        IdProperty.SetValue(relationship, id);
        _relationshipsById[id] = relationship;
    }

    public Task<IReadOnlyList<KeyRelationship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KeyRelationship>>(_relationshipsById.Values.ToList());

    public Task<KeyRelationship?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_relationshipsById.GetValueOrDefault(id));
}
