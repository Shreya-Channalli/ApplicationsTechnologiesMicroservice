
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationsToolsTechnologies.Controllers;

[ApiController]
[Route("api/technologies")]
[Produces("application/json")]
public sealed class TechnologiesController : ControllerBase
{
    private readonly ITechnologiesService _service;

    public TechnologiesController(ITechnologiesService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets technologies with search, category and usage-status
    /// filters, sorting, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<TechnologyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<TechnologyDto>>> GetAll(
        [FromQuery] TechnologyQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            parameters, cancellationToken);

        return Ok(result);
    }
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TechnologyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TechnologyDto>> GetById(
    int id,
    CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("Technology ID must be greater than zero.");

        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets summary statistics and analytics for technologies.
    /// </summary>
    [HttpGet("insights")]
    [ProducesResponseType(
        typeof(TechnologyInsightsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TechnologyInsightsDto>> GetInsights(
        CancellationToken cancellationToken)
    {
        var result = await _service.GetInsightsAsync(cancellationToken);

        return Ok(result);
    }

}
