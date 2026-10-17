
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Repositories.Interfaces;
using ApplicationsToolsTechnologies.Services.Interfaces;

namespace ApplicationsToolsTechnologies.Services;

public sealed class TechnologiesService : ITechnologiesService
{
    private readonly ITechnologiesRepository _repository;

    public TechnologiesService(ITechnologiesRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResponse<TechnologyDto>> GetAllAsync(
        TechnologyQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return _repository.GetAllAsync(parameters, cancellationToken);
    }
    public Task<TechnologyDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(id), "Technology ID must be greater than zero.");

        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<TechnologyInsightsDto> GetInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetInsightsAsync(cancellationToken);
    }

}
