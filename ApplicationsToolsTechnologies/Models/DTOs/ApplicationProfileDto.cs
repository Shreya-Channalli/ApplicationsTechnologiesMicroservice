
namespace ApplicationsToolsTechnologies.Models.DTOs;

public sealed class ApplicationProfileDto
{
    public int AssetId { get; init; }
    public string? AssetCode { get; init; }
    public string? AssetName { get; init; }
    public string? AssetType { get; init; }
    public string? Description { get; init; }
    public string? ArchitectureSummary { get; init; }
    public string? UserAudience { get; init; }
    public string? Sourcing { get; init; }
    public string? StatusName { get; init; }
    public string? StatusNote { get; init; }

    public List<ApplicationStageDto> Stages { get; init; } = [];
    public List<ApplicationOwnershipDto> Ownerships { get; init; } = [];
    public List<ApplicationVendorDto> Vendors { get; init; } = [];
    public List<ApplicationTagDto> Tags { get; init; } = [];
}

public sealed class ApplicationStageDto
{
    public int StageId { get; init; }
    public string? StageCode { get; init; }
    public string? StageName { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class ApplicationOwnershipDto
{
    public int OrganizationalUnitId { get; init; }
    public string? UnitName { get; init; }
    public string? UnitType { get; init; }
    public string? OwnershipRole { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class ApplicationVendorDto
{
    public int VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? VendorType { get; init; }
    public string? RelationshipRole { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class ApplicationTagDto
{
    public int TagId { get; init; }
    public string? TagName { get; init; }
    public string? TagCategory { get; init; }
}
