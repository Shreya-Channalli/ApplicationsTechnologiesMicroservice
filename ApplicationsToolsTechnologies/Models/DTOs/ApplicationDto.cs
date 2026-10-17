
namespace ApplicationsToolsTechnologies.Models.DTOs;

public sealed class ApplicationDto
{
    public int AssetId { get; init; }
    public string? AssetCode { get; init; }
    public string? AssetName { get; init; }
    public string? AssetType { get; init; }
    public string? Description { get; init; }
    public string? ArchitectureSummary { get; init; }
    public int? PrimaryStageId { get; init; }
    public string? UserAudience { get; init; }
    public string? Sourcing { get; init; }
    public string? StatusName { get; init; }
}
