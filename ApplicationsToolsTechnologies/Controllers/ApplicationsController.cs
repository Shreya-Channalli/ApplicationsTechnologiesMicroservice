
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationsToolsTechnologies.Controllers;

[ApiController]
[Route("api/applications")]
[Produces("application/json")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationsService _service;

    public ApplicationsController(IApplicationsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets applications, tools, and solutions with filtering,
    /// sorting, searching, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<ApplicationDto>>> GetAll(
        [FromQuery] ApplicationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            parameters, cancellationToken);

        return Ok(result);
    }
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApplicationProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationProfileDto>> GetById(
    int id,
    CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("ID must be greater than zero.");

        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
    [HttpGet("insights")]
    [ProducesResponseType(typeof(ApplicationInsightsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationInsightsDto>> GetInsights(
    CancellationToken cancellationToken)
    {
        var insights = await _service.GetInsightsAsync(cancellationToken);
        return Ok(insights);
    }
}
