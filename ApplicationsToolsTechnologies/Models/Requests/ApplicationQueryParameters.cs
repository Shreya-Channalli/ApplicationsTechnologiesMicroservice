
using System.ComponentModel.DataAnnotations;

namespace ApplicationsToolsTechnologies.Models.Requests;

public sealed class ApplicationQueryParameters
{
    [StringLength(200)]
    public string? Search { get; init; }

    [StringLength(50)]
    public string? AssetType { get; init; }

    [StringLength(100)]
    public string? Status { get; init; }

    public int? StageId { get; init; }

    [StringLength(100)]
    public string? Sourcing { get; init; }

    [StringLength(100)]
    public string? UserAudience { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [StringLength(50)]
    public string SortBy { get; init; } = "AssetName";

    [RegularExpression("^(asc|desc)$",
        ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; init; } = "asc";
}
