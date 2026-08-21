using Dashboard.Application.Metrics;

namespace Dashboard.Application.Social;

/// <summary>
/// The write side for key relationships: logging a check-in against an
/// existing row. Unlike <see cref="FriendService"/>, there's no "add" here --
/// the fixed set of key relationships is seeded once and never grows -- and
/// no monthly snapshot to refresh, since key relationships don't factor into
/// active-circle size.
/// </summary>
public sealed class KeyRelationshipService
{
    private readonly IKeyRelationshipRepository _keyRelationshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public KeyRelationshipService(IKeyRelationshipRepository keyRelationshipRepository, IUnitOfWork unitOfWork)
    {
        _keyRelationshipRepository = keyRelationshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogContactAsync(int id, DateOnly date, CancellationToken cancellationToken = default)
    {
        var relationship = await _keyRelationshipRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Key relationship {id} was not found.");

        relationship.LogContact(date);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
