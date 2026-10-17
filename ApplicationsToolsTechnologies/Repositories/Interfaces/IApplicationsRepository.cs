
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;

namespace ApplicationsToolsTechnologies.Repositories.Interfaces;

public interface IApplicationsRepository
{
    Task<PagedResponse<ApplicationDto>> GetAllAsync(
        ApplicationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ApplicationProfileDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default);

    Task<ApplicationInsightsDto> GetInsightsAsync(
    CancellationToken cancellationToken = default);
}
