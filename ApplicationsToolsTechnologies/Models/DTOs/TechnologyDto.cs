
namespace ApplicationsToolsTechnologies.Models.DTOs;

public sealed class TechnologyDto
{
    public int TechnologyId { get; init; }
    public string? TechnologyName { get; init; }
    public string? ConsilioUsageStatus { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public int MappedAssetCount { get; init; }
    public int? UsageRank { get; init; }
}
