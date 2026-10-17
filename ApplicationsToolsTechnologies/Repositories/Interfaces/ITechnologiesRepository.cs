
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;

namespace ApplicationsToolsTechnologies.Repositories.Interfaces;

public interface ITechnologiesRepository
{
    Task<PagedResponse<TechnologyDto>> GetAllAsync(
        TechnologyQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<TechnologyDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default);

    Task<TechnologyInsightsDto> GetInsightsAsync(
    CancellationToken cancellationToken = default);
}
