
namespace ApplicationsToolsTechnologies.Models.DTOs;

public sealed class ApplicationInsightsDto
{
    public int TotalAssets { get; init; }

    public int Products { get; init; }
    public int Tools { get; init; }
    public int Solutions { get; init; }

    public int MissingStatusCount { get; init; }
    public int MissingSourcingCount { get; init; }
    public int MissingAudienceCount { get; init; }
    public int AssetsWithoutOwnerCount { get; init; }
    public int AssetsWithoutVendorCount { get; init; }

    public List<InsightCountDto> StatusDistribution { get; init; } = [];
    public List<InsightCountDto> SourcingDistribution { get; init; } = [];
    public List<InsightCountDto> AssetTypeDistribution { get; init; } = [];
}

public sealed class InsightCountDto
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}
