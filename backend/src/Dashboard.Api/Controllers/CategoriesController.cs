using Microsoft.AspNetCore.Mvc;
using Dashboard.Api.Contracts;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDetailService _categoryDetailService;
    private readonly MetricEntryService _metricEntryService;

    public CategoriesController(
        ICategoryRepository categoryRepository,
        CategoryDetailService categoryDetailService,
        MetricEntryService metricEntryService)
    {
        _categoryRepository = categoryRepository;
        _categoryDetailService = categoryDetailService;
        _metricEntryService = metricEntryService;
    }

    /// <summary>
    /// Lightweight lookup list (id/name/sortOrder) -- e.g. so the Fitness
    /// page can resolve "Fitness" to its category id without the frontend
    /// hardcoding one.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDetail>> GetDetail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _categoryDetailService.GetDetailAsync(id, cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/entries")]
    public async Task<IActionResult> RecordEntries(int id, RecordEntriesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _metricEntryService.RecordEntriesAsync(id, request.Month, request.Values, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
