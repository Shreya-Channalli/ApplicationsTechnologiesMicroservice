
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Repositories.Interfaces;
using ApplicationsToolsTechnologies.Services.Interfaces;

namespace ApplicationsToolsTechnologies.Services;

public sealed class ApplicationsService : IApplicationsService
{
    private readonly IApplicationsRepository _repository;

    public ApplicationsService(IApplicationsRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResponse<ApplicationDto>> GetAllAsync(
        ApplicationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return _repository.GetAllAsync(parameters, cancellationToken);
    }
    public Task<ApplicationProfileDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(id), "Asset ID must be greater than zero.");

        return _repository.GetByIdAsync(id, cancellationToken);
    }
    public async Task<ApplicationInsightsDto> GetInsightsAsync(
    CancellationToken cancellationToken = default)
    {
        return await _repository.GetInsightsAsync(cancellationToken);
    }
}
