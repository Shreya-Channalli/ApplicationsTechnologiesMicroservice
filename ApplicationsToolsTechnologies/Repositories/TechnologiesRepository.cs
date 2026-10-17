
using System.Data;
using ApplicationsToolsTechnologies.Data;
using ApplicationsToolsTechnologies.Models.Common;
using ApplicationsToolsTechnologies.Models.DTOs;
using ApplicationsToolsTechnologies.Models.Requests;
using ApplicationsToolsTechnologies.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace ApplicationsToolsTechnologies.Repositories;

public sealed class TechnologiesRepository : ITechnologiesRepository
{
    private readonly SqlDataAccess _dataAccess;

    public TechnologiesRepository(SqlDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public async Task<PagedResponse<TechnologyDto>> GetAllAsync(
        TechnologyQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<string>();

        var sortColumns = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["TechnologyName"] = "t.TechnologyName",
            ["ConsilioUsageStatus"] = "t.ConsilioUsageStatus",
            ["MappedAssetCount"] = "Usage.MappedAssetCount",
            ["TechnologyId"] = "t.TechnologyId"
        };

        var sortColumn = sortColumns.TryGetValue(
            parameters.SortBy ?? "TechnologyName", out var column)
            ? column
            : "t.TechnologyName";

        var sortDirection = string.Equals(
            parameters.SortDirection, "desc",
            StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            filters.Add("t.TechnologyName LIKE @Search");
        }

        if (!string.IsNullOrWhiteSpace(parameters.UsageStatus))
        {
            filters.Add("t.ConsilioUsageStatus = @UsageStatus");
        }

        if (parameters.CategoryId.HasValue)
        {
            filters.Add("""
                EXISTS (
                    SELECT 1
                    FROM dbo.TechnologyCategoryLinks AS filterLink
                    WHERE filterLink.TechnologyId = t.TechnologyId
                      AND filterLink.TechnologyCategoryId = @CategoryId
                )
                """);
        }

        var whereClause = filters.Count > 0
            ? "WHERE " + string.Join(" AND ", filters)
            : string.Empty;

        var countSql = $"""
            SELECT COUNT(1) AS TotalCount
            FROM dbo.Technologies AS t
            {whereClause};
            """;

        var filterParameters = CreateFilterParameters(parameters);

        var countRows = await _dataAccess.QueryAsync(
            countSql, filterParameters, cancellationToken);

        var totalCount = Convert.ToInt32(countRows[0]["TotalCount"]);

        var page = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);
        var offset = (long)(page - 1) * pageSize;

        var dataSql = $"""
            WITH Usage AS
            (
                SELECT
                    t.TechnologyId,
                    COUNT(DISTINCT at.AssetId) AS MappedAssetCount
                FROM dbo.Technologies AS t
                LEFT JOIN dbo.AssetTechnologies AS at
                    ON at.TechnologyId = t.TechnologyId
                GROUP BY t.TechnologyId
            ),
            Ranked AS
            (
                SELECT
                    TechnologyId,
                    MappedAssetCount,
                    ROW_NUMBER() OVER (
                        ORDER BY MappedAssetCount DESC, TechnologyId ASC
                    ) AS UsageRank
                FROM Usage
            )
            SELECT
                t.TechnologyId,
                t.TechnologyName,
                t.ConsilioUsageStatus,
                STRING_AGG(CONVERT(nvarchar(max), cat.CategoryName), N'|')
                    AS Categories,
                Usage.MappedAssetCount,
                Ranked.UsageRank
            FROM dbo.Technologies AS t
            INNER JOIN Usage
                ON Usage.TechnologyId = t.TechnologyId
            INNER JOIN Ranked
                ON Ranked.TechnologyId = t.TechnologyId
            LEFT JOIN dbo.TechnologyCategoryLinks AS link
                ON link.TechnologyId = t.TechnologyId
            LEFT JOIN dbo.TechnologyCategories AS cat
                ON cat.TechnologyCategoryId = link.TechnologyCategoryId
            {whereClause}
            GROUP BY
                t.TechnologyId,
                t.TechnologyName,
                t.ConsilioUsageStatus,
                Usage.MappedAssetCount,
                Ranked.UsageRank
            ORDER BY {sortColumn} {sortDirection}, t.TechnologyId ASC
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

        var items = rows.Select(row => new TechnologyDto
        {
            TechnologyId = Convert.ToInt32(row["TechnologyId"]),
            TechnologyName = GetString(row, "TechnologyName"),
            ConsilioUsageStatus = GetString(row, "ConsilioUsageStatus"),
            Categories = (GetString(row, "Categories") ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            MappedAssetCount = Convert.ToInt32(row["MappedAssetCount"]),
            UsageRank = Convert.ToInt32(row["UsageRank"])
        }).ToList();

        return new PagedResponse<TechnologyDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static List<SqlParameter> CreateFilterParameters(
        TechnologyQueryParameters parameters)
    {
        var result = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            result.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 200)
            {
                Value = $"%{parameters.Search.Trim()}%"
            });
        }

        if (!string.IsNullOrWhiteSpace(parameters.UsageStatus))
        {
            result.Add(new SqlParameter(
                "@UsageStatus", SqlDbType.NVarChar, 100)
            {
                Value = parameters.UsageStatus.Trim()
            });
        }

        if (parameters.CategoryId.HasValue)
        {
            result.Add(new SqlParameter("@CategoryId", SqlDbType.Int)
            {
                Value = parameters.CategoryId.Value
            });
        }

        return result;
    }

    public async Task<TechnologyDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(id), "Technology ID must be greater than zero.");

        const string sql = """
        WITH Usage AS
        (
            SELECT
                t.TechnologyId,
                COUNT(DISTINCT at.AssetId) AS MappedAssetCount
            FROM dbo.Technologies AS t
            LEFT JOIN dbo.AssetTechnologies AS at
                ON at.TechnologyId = t.TechnologyId
            GROUP BY t.TechnologyId
        ),
        Ranked AS
        (
            SELECT
                TechnologyId,
                MappedAssetCount,
                ROW_NUMBER() OVER (
                    ORDER BY MappedAssetCount DESC, TechnologyId ASC
                ) AS UsageRank
            FROM Usage
        )
        SELECT
            t.TechnologyId,
            t.TechnologyName,
            t.ConsilioUsageStatus,
            STRING_AGG(CONVERT(nvarchar(max), cat.CategoryName), N'|')
                AS Categories,
            Usage.MappedAssetCount,
            Ranked.UsageRank
        FROM dbo.Technologies AS t
        INNER JOIN Usage
            ON Usage.TechnologyId = t.TechnologyId
        INNER JOIN Ranked
            ON Ranked.TechnologyId = t.TechnologyId
        LEFT JOIN dbo.TechnologyCategoryLinks AS link
            ON link.TechnologyId = t.TechnologyId
        LEFT JOIN dbo.TechnologyCategories AS cat
            ON cat.TechnologyCategoryId = link.TechnologyCategoryId
        WHERE t.TechnologyId = @TechnologyId
        GROUP BY
            t.TechnologyId,
            t.TechnologyName,
            t.ConsilioUsageStatus,
            Usage.MappedAssetCount,
            Ranked.UsageRank;
        """;

        var rows = await _dataAccess.QueryAsync(
            sql,
            [
                new SqlParameter("@TechnologyId", SqlDbType.Int)
            {
                Value = id
            }
            ],
            cancellationToken);

        if (rows.Count == 0)
            return null;

        var row = rows[0];

        var categories = (GetString(row, "Categories") ?? string.Empty)
            .Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TechnologyDto
        {
            TechnologyId = Convert.ToInt32(row["TechnologyId"]),
            TechnologyName = GetString(row, "TechnologyName"),
            ConsilioUsageStatus = GetString(row, "ConsilioUsageStatus"),
            Categories = categories,
            MappedAssetCount = Convert.ToInt32(row["MappedAssetCount"]),
            UsageRank = Convert.ToInt32(row["UsageRank"])
        };
    }

    public async Task<TechnologyInsightsDto> GetInsightsAsync(
        CancellationToken cancellationToken = default)
    {
        var summaryRows = await _dataAccess.QueryAsync("""
        SELECT
            (SELECT COUNT(*) FROM dbo.Technologies) AS TotalTechnologies,
            (SELECT COUNT(*) FROM dbo.TechnologyCategories) AS TotalCategories,
            (SELECT COUNT(DISTINCT AssetId)
             FROM dbo.AssetTechnologies) AS DistinctMappedAssets,
            (SELECT COUNT(*) FROM dbo.AssetTechnologies) AS TotalMappings,
            CAST(
                100.0 * (
                    SELECT COUNT(DISTINCT TechnologyId)
                    FROM dbo.AssetTechnologies
                ) / NULLIF((SELECT COUNT(*) FROM dbo.Technologies), 0)
                AS decimal(6,2)
            ) AS MappedTechnologyPercentage,
            CAST(
                1.0 * (SELECT COUNT(*) FROM dbo.AssetTechnologies) /
                NULLIF((SELECT COUNT(*) FROM dbo.Technologies), 0)
                AS decimal(10,2)
            ) AS AverageAssetsPerTechnology;
        """, cancellationToken);

        var summary = summaryRows.Single();

        var categoryRows = await _dataAccess.QueryAsync("""
        SELECT TOP (1)
            c.CategoryName AS Name,
            COUNT(DISTINCT l.TechnologyId) AS TechnologyCount
        FROM dbo.TechnologyCategories c
        LEFT JOIN dbo.TechnologyCategoryLinks l
            ON l.TechnologyCategoryId = c.TechnologyCategoryId
        GROUP BY c.CategoryName
        ORDER BY TechnologyCount DESC, c.CategoryName;
        """, cancellationToken);

        var statusRows = await _dataAccess.QueryAsync("""
        SELECT
            COALESCE(
                NULLIF(LTRIM(RTRIM(ConsilioUsageStatus)), ''),
                'Not recorded'
            ) AS Name,
            COUNT(*) AS Count
        FROM dbo.Technologies
        GROUP BY COALESCE(
            NULLIF(LTRIM(RTRIM(ConsilioUsageStatus)), ''),
            'Not recorded'
        )
        ORDER BY Count DESC, Name;
        """, cancellationToken);

        var topTechnologyRows = await _dataAccess.QueryAsync("""
        SELECT TOP (5)
            t.TechnologyId,
            t.TechnologyName,
            COUNT(DISTINCT at.AssetId) AS MappedAssetCount
        FROM dbo.Technologies t
        LEFT JOIN dbo.AssetTechnologies at
            ON at.TechnologyId = t.TechnologyId
        GROUP BY t.TechnologyId, t.TechnologyName
        ORDER BY MappedAssetCount DESC, t.TechnologyName;
        """, cancellationToken);

        static int GetInt(
            Dictionary<string, object?> row,
            string column)
        {
            return Convert.ToInt32(row[column] ?? 0);
        }

        static decimal GetDecimal(
            Dictionary<string, object?> row,
            string column)
        {
            return Convert.ToDecimal(row[column] ?? 0);
        }

        return new TechnologyInsightsDto
        {
            TotalTechnologies = GetInt(summary, "TotalTechnologies"),
            TotalCategories = GetInt(summary, "TotalCategories"),
            DistinctMappedAssets = GetInt(summary, "DistinctMappedAssets"),
            TotalMappings = GetInt(summary, "TotalMappings"),
            MappedTechnologyPercentage =
                GetDecimal(summary, "MappedTechnologyPercentage"),
            AverageAssetsPerTechnology =
                GetDecimal(summary, "AverageAssetsPerTechnology"),
            TopCategory = categoryRows.Count > 0
                ? Convert.ToString(categoryRows[0]["Name"])
                : null,
            UsageStatusDistribution = statusRows.Select(row =>
                new InsightCountDto
                {
                    Name = Convert.ToString(row["Name"]) ?? "Not recorded",
                    Count = Convert.ToInt32(row["Count"] ?? 0)
                }).ToList(),
            TopTechnologies = topTechnologyRows.Select(row =>
                new TopTechnologyDto
                {
                    TechnologyId = Convert.ToInt32(row["TechnologyId"]),
                    TechnologyName =
                        Convert.ToString(row["TechnologyName"]),
                    MappedAssetCount =
                        Convert.ToInt32(row["MappedAssetCount"] ?? 0)
                }).ToList()
        };
    }



    private static string? GetString(
        Dictionary<string, object?> row, string column)
    {
        return row[column] is null
            ? null
            : Convert.ToString(row[column]);
    }
}
