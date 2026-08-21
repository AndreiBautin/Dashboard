using Microsoft.AspNetCore.Mvc;
using Dashboard.Application.Metrics;

namespace Dashboard.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private readonly MetricTrendService _metricTrendService;

    public MetricsController(MetricTrendService metricTrendService)
    {
        _metricTrendService = metricTrendService;
    }

    [HttpGet("{id:int}/trend")]
    public async Task<ActionResult<IReadOnlyList<MetricTrendPoint>>> GetTrend(int id, CancellationToken cancellationToken)
    {
        try
        {
            var trend = await _metricTrendService.GetTrendAsync(id, cancellationToken);
            return Ok(trend);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
