
using System.Data;
using ApplicationsToolsTechnologies.Data;
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace ApplicationsToolsTechnologies.Repositories;

public sealed class ApplicationsRepository : IApplicationsRepository
{
    private readonly SqlDataAccess _dataAccess;

    public ApplicationsRepository(SqlDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<PagedResponse<ApplicationDto>> GetAllAsync(
        ApplicationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<string>();
        var sortColumns = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["AssetId"] = "a.AssetId",
            ["AssetCode"] = "a.AssetCode",
            ["AssetName"] = "a.AssetName",
            ["AssetType"] = "a.AssetType",
            ["StatusName"] = "a.StatusName",
            ["Sourcing"] = "a.Sourcing"
        };

        var sortColumn = sortColumns.TryGetValue(
            parameters.SortBy ?? "AssetName", out var column)
            ? column
            : "a.AssetName";

        var sortDirection = string.Equals(
            parameters.SortDirection, "desc",
            StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        if (!string.IsNullOrWhiteSpace(parameters.Search))
            filters.Add("""
                (
                    a.AssetName LIKE @Search
                    OR a.AssetCode LIKE @Search
                    OR a.Description LIKE @Search
                )
                """);

        if (!string.IsNullOrWhiteSpace(parameters.AssetType))
            filters.Add("a.AssetType = @AssetType");

        if (!string.IsNullOrWhiteSpace(parameters.Status))
            filters.Add("a.StatusName = @Status");

        if (parameters.StageId.HasValue)
            filters.Add("a.PrimaryStageId = @StageId");

        if (!string.IsNullOrWhiteSpace(parameters.Sourcing))
            filters.Add("a.Sourcing = @Sourcing");

        if (!string.IsNullOrWhiteSpace(parameters.UserAudience))
            filters.Add("a.UserAudience = @UserAudience");

        var whereClause = filters.Count > 0
            ? "WHERE " + string.Join(" AND ", filters)
            : string.Empty;

        var countSql = $"""
            SELECT COUNT(1) AS TotalCount
            FROM dbo.Assets AS a
            {whereClause};
            """;

        var countRows = await _dataAccess.QueryAsync(
            countSql,
            CreateFilterParameters(parameters),
            cancellationToken);

        var totalCount = Convert.ToInt32(countRows[0]["TotalCount"]);

        var page = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);
        var offset = (long)(page - 1) * pageSize;

        var dataSql = $"""
            SELECT
                a.AssetId,
                a.AssetCode,
                a.AssetName,
                a.AssetType,
                a.Description,
                a.ArchitectureSummary,
                a.PrimaryStageId,
                a.UserAudience,
                a.Sourcing,
                a.StatusName
            FROM dbo.Assets AS a
            {whereClause}
            ORDER BY {sortColumn} {sortDirection}, a.AssetId ASC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var queryParameters = CreateFilterParameters(parameters);
        queryParameters.Add(new SqlParameter("@Offset", SqlDbType.BigInt)
        {
            Value = offset
        });
        queryParameters.Add(new SqlParameter("@PageSize", SqlDbType.Int)
        {
            Value = pageSize
        });

        var rows = await _dataAccess.QueryAsync(
            dataSql, queryParameters, cancellationToken);

        var items = rows.Select(row => new ApplicationDto
        {
            AssetId = Convert.ToInt32(row["AssetId"]),
            AssetCode = GetString(row, "AssetCode"),
            AssetName = GetString(row, "AssetName"),
            AssetType = GetString(row, "AssetType"),
            Description = GetString(row, "Description"),
            ArchitectureSummary = GetString(row, "ArchitectureSummary"),
            PrimaryStageId = row["PrimaryStageId"] is null
                ? null
                : Convert.ToInt32(row["PrimaryStageId"]),
            UserAudience = GetString(row, "UserAudience"),
            Sourcing = GetString(row, "Sourcing"),
            StatusName = GetString(row, "StatusName")
        }).ToList();

        return new PagedResponse<ApplicationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static List<SqlParameter> CreateFilterParameters(
        ApplicationQueryParameters parameters)
    {
        var result = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            result.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 200)
            {
                Value = $"%{parameters.Search.Trim()}%"
            });
        }

        if (!string.IsNullOrWhiteSpace(parameters.AssetType))
        {
            result.Add(new SqlParameter("@AssetType", SqlDbType.NVarChar, 50)
            {
                Value = parameters.AssetType.Trim()
            });
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            result.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 100)
            {
                Value = parameters.Status.Trim()
            });
        }

        if (parameters.StageId.HasValue)
        {
            result.Add(new SqlParameter("@StageId", SqlDbType.Int)
            {
                Value = parameters.StageId.Value
            });
        }

        if (!string.IsNullOrWhiteSpace(parameters.Sourcing))
        {
            result.Add(new SqlParameter("@Sourcing", SqlDbType.NVarChar, 100)
            {
                Value = parameters.Sourcing.Trim()
            });
        }

        if (!string.IsNullOrWhiteSpace(parameters.UserAudience))
        {
            result.Add(new SqlParameter("@UserAudience", SqlDbType.NVarChar, 100)
            {
                Value = parameters.UserAudience.Trim()
            });
        }

        return result;
    }


    public async Task<ApplicationProfileDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var assetRows = await _dataAccess.QueryAsync(
            """
        SELECT AssetId, AssetCode, AssetName, AssetType, Description,
               ArchitectureSummary, UserAudience, Sourcing,
               StatusName, StatusNote
        FROM dbo.Assets
        WHERE AssetId = @AssetId;
        """,
            [new SqlParameter("@AssetId", SqlDbType.Int) { Value = id }],
            cancellationToken);

        if (assetRows.Count == 0)
            return null;

        var asset = assetRows[0];

        var stageRows = await _dataAccess.QueryAsync(
            """
        SELECT s.StageId, s.StageCode, s.StageName, ast.IsPrimary
        FROM dbo.AssetStages ast
        INNER JOIN dbo.Stages s ON s.StageId = ast.StageId
        WHERE ast.AssetId = @AssetId
        ORDER BY ast.IsPrimary DESC, s.SequenceNumber, s.StageName;
        """,
            [new SqlParameter("@AssetId", SqlDbType.Int) { Value = id }],
            cancellationToken);

        var ownershipRows = await _dataAccess.QueryAsync(
            """
        SELECT o.OrganizationalUnitId, ou.UnitName, ou.UnitType,
               o.OwnershipRole, o.IsPrimary
        FROM dbo.AssetOwnerships o
        INNER JOIN dbo.OrganizationalUnits ou
            ON ou.OrganizationalUnitId = o.OrganizationalUnitId
        WHERE o.AssetId = @AssetId
        ORDER BY o.IsPrimary DESC, ou.UnitName;
        """,
            [new SqlParameter("@AssetId", SqlDbType.Int) { Value = id }],
            cancellationToken);

        var vendorRows = await _dataAccess.QueryAsync(
            """
        SELECT v.VendorId, v.VendorName, v.VendorType,
               av.RelationshipRole, av.IsPrimary
        FROM dbo.AssetVendors av
        INNER JOIN dbo.Vendors v ON v.VendorId = av.VendorId
        WHERE av.AssetId = @AssetId
        ORDER BY av.IsPrimary DESC, v.VendorName;
        """,
            [new SqlParameter("@AssetId", SqlDbType.Int) { Value = id }],
            cancellationToken);

        var tagRows = await _dataAccess.QueryAsync(
            """
        SELECT t.TagId, t.TagName, t.TagCategory
        FROM dbo.AssetTags at
        INNER JOIN dbo.Tags t ON t.TagId = at.TagId
        WHERE at.AssetId = @AssetId
        ORDER BY t.TagName;
        """,
            [new SqlParameter("@AssetId", SqlDbType.Int) { Value = id }],
            cancellationToken);

        return new ApplicationProfileDto
        {
            AssetId = Convert.ToInt32(asset["AssetId"]),
            AssetCode = GetString(asset, "AssetCode"),
            AssetName = GetString(asset, "AssetName"),
            AssetType = GetString(asset, "AssetType"),
            Description = GetString(asset, "Description"),
            ArchitectureSummary = GetString(asset, "ArchitectureSummary"),
            UserAudience = GetString(asset, "UserAudience"),
            Sourcing = GetString(asset, "Sourcing"),
            StatusName = GetString(asset, "StatusName"),
            StatusNote = GetString(asset, "StatusNote"),

            Stages = stageRows.Select(r => new ApplicationStageDto
            {
                StageId = Convert.ToInt32(r["StageId"]),
                StageCode = GetString(r, "StageCode"),
                StageName = GetString(r, "StageName"),
                IsPrimary = Convert.ToBoolean(r["IsPrimary"])
            }).ToList(),

            Ownerships = ownershipRows.Select(r => new ApplicationOwnershipDto
            {
                OrganizationalUnitId = Convert.ToInt32(r["OrganizationalUnitId"]),
                UnitName = GetString(r, "UnitName"),
                UnitType = GetString(r, "UnitType"),
                OwnershipRole = GetString(r, "OwnershipRole"),
                IsPrimary = Convert.ToBoolean(r["IsPrimary"])
            }).ToList(),

            Vendors = vendorRows.Select(r => new ApplicationVendorDto
            {
                VendorId = Convert.ToInt32(r["VendorId"]),
                VendorName = GetString(r, "VendorName"),
                VendorType = GetString(r, "VendorType"),
                RelationshipRole = GetString(r, "RelationshipRole"),
                IsPrimary = Convert.ToBoolean(r["IsPrimary"])
            }).ToList(),

            Tags = tagRows.Select(r => new ApplicationTagDto
            {
                TagId = Convert.ToInt32(r["TagId"]),
                TagName = GetString(r, "TagName"),
                TagCategory = GetString(r, "TagCategory")
            }).ToList()
        };
    }



    public async Task<ApplicationInsightsDto> GetInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        var summaryRows = await _dataAccess.QueryAsync("""
        SELECT
            COUNT(*) AS TotalAssets,
            SUM(CASE
                WHEN LOWER(AssetType) = 'product' THEN 1
                ELSE 0
            END) AS Products,
            SUM(CASE
                WHEN LOWER(AssetType) = 'tool' THEN 1
                ELSE 0
            END) AS Tools,
            SUM(CASE
                WHEN LOWER(AssetType) = 'solution' THEN 1
                ELSE 0
            END) AS Solutions,
            SUM(CASE
                WHEN NULLIF(LTRIM(RTRIM(StatusName)), '') IS NULL
                THEN 1 ELSE 0
            END) AS MissingStatusCount,
            SUM(CASE
                WHEN NULLIF(LTRIM(RTRIM(Sourcing)), '') IS NULL
                THEN 1 ELSE 0
            END) AS MissingSourcingCount,
            SUM(CASE
                WHEN NULLIF(LTRIM(RTRIM(UserAudience)), '') IS NULL
                THEN 1 ELSE 0
            END) AS MissingAudienceCount,
            (
                SELECT COUNT(*)
                FROM dbo.Assets a2
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.AssetOwnerships o
                    WHERE o.AssetId = a2.AssetId
                )
            ) AS AssetsWithoutOwnerCount,
            (
                SELECT COUNT(*)
                FROM dbo.Assets a2
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.AssetVendors v
                    WHERE v.AssetId = a2.AssetId
                )
            ) AS AssetsWithoutVendorCount
        FROM dbo.Assets a;
        """, cancellationToken);

        var summary = summaryRows.Single();

        var statusRows = await _dataAccess.QueryAsync("""
        SELECT
            COALESCE(
                NULLIF(LTRIM(RTRIM(StatusName)), ''),
                'Not recorded'
            ) AS Name,
            COUNT(*) AS Count
        FROM dbo.Assets
        GROUP BY COALESCE(
            NULLIF(LTRIM(RTRIM(StatusName)), ''),
            'Not recorded'
        )
        ORDER BY Count DESC, Name;
        """, cancellationToken);

        var sourcingRows = await _dataAccess.QueryAsync("""
        SELECT
            COALESCE(
                NULLIF(LTRIM(RTRIM(Sourcing)), ''),
                'Not recorded'
            ) AS Name,
            COUNT(*) AS Count
        FROM dbo.Assets
        GROUP BY COALESCE(
            NULLIF(LTRIM(RTRIM(Sourcing)), ''),
            'Not recorded'
        )
        ORDER BY Count DESC, Name;
        """, cancellationToken);

        var assetTypeRows = await _dataAccess.QueryAsync("""
        SELECT
            COALESCE(
                NULLIF(LTRIM(RTRIM(AssetType)), ''),
                'Not recorded'
            ) AS Name,
            COUNT(*) AS Count
        FROM dbo.Assets
        GROUP BY COALESCE(
            NULLIF(LTRIM(RTRIM(AssetType)), ''),
            'Not recorded'
        )
        ORDER BY Count DESC, Name;
        """, cancellationToken);

        static int GetInt(
            Dictionary<string, object?> row,
            string column)
        {
            return Convert.ToInt32(row[column] ?? 0);
        }

        static List<InsightCountDto> MapDistribution(
            List<Dictionary<string, object?>> rows)
        {
            return rows.Select(row => new InsightCountDto
            {
                Name = Convert.ToString(row["Name"]) ?? "Not recorded",
                Count = Convert.ToInt32(row["Count"] ?? 0)
            }).ToList();
        }

        return new ApplicationInsightsDto
        {
            TotalAssets = GetInt(summary, "TotalAssets"),
            Products = GetInt(summary, "Products"),
            Tools = GetInt(summary, "Tools"),
            Solutions = GetInt(summary, "Solutions"),
            MissingStatusCount = GetInt(summary, "MissingStatusCount"),
            MissingSourcingCount = GetInt(summary, "MissingSourcingCount"),
            MissingAudienceCount = GetInt(summary, "MissingAudienceCount"),
            AssetsWithoutOwnerCount =
                GetInt(summary, "AssetsWithoutOwnerCount"),
            AssetsWithoutVendorCount =
                GetInt(summary, "AssetsWithoutVendorCount"),
            StatusDistribution = MapDistribution(statusRows),
            SourcingDistribution = MapDistribution(sourcingRows),
            AssetTypeDistribution = MapDistribution(assetTypeRows)
        };
    }

    private static string? GetString(
        Dictionary<string, object?> row,
        string column)
    {
        return row.TryGetValue(column, out var value) && value != null
            && value != DBNull.Value
                ? Convert.ToString(value)
                : null;
    }

}
