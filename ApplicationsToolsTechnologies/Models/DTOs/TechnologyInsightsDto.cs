
namespace ApplicationsToolsTechnologies.Models.DTOs;

public sealed class TechnologyInsightsDto
{
    public int TotalTechnologies { get; init; }
    public int TotalCategories { get; init; }
    public int DistinctMappedAssets { get; init; }
    public int TotalMappings { get; init; }
    public decimal MappedTechnologyPercentage { get; init; }
    public decimal AverageAssetsPerTechnology { get; init; }

    public string? TopCategory { get; init; }

    public List<InsightCountDto> UsageStatusDistribution { get; init; } = [];
    public List<TopTechnologyDto> TopTechnologies { get; init; } = [];
}

public sealed class TopTechnologyDto
{
    public int TechnologyId { get; init; }
    public string? TechnologyName { get; init; }
    public int MappedAssetCount { get; init; }
}
