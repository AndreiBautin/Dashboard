using Microsoft.AspNetCore.Mvc;
using Vantage.Api.Contracts;
using Vantage.Application.Metrics;
using Vantage.Application.Social;

namespace Vantage.Api.Controllers;

[ApiController]
[Route("api/social")]
public sealed class SocialController : ControllerBase
{
    private readonly SocialService _socialService;
    private readonly FriendService _friendService;
    private readonly KeyRelationshipService _keyRelationshipService;

    public SocialController(SocialService socialService, FriendService friendService, KeyRelationshipService keyRelationshipService)
    {
        _socialService = socialService;
        _friendService = friendService;
        _keyRelationshipService = keyRelationshipService;
    }

    [HttpGet]
    public async Task<ActionResult<SocialSummary>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _socialService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("trend")]
    public async Task<ActionResult<IReadOnlyList<MetricTrendPoint>>> GetTrend(CancellationToken cancellationToken)
    {
        var trend = await _socialService.GetTrendAsync(cancellationToken);
        return Ok(trend);
    }

    [HttpPost("friends")]
    public async Task<ActionResult> AddFriend(AddFriendRequest request, CancellationToken cancellationToken)
    {
        var id = await _friendService.AddFriendAsync(request.Name, request.LastHangoutDate, request.Notes, cancellationToken);
        return CreatedAtAction(nameof(GetSummary), new { }, new { id });
    }

    [HttpPost("friends/{id:int}/hangouts")]
    public async Task<IActionResult> LogHangout(int id, LogHangoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _friendService.LogHangoutAsync(id, request.Date, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("key-relationships/{id:int}/log")]
    public async Task<IActionResult> LogKeyRelationshipContact(int id, LogKeyRelationshipContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _keyRelationshipService.LogContactAsync(id, request.Date, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
