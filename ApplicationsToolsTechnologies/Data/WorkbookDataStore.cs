using System.Text.Json;

namespace ApplicationsToolsTechnologies.Data;

/// <summary>
/// Loads the two Excel workbook snapshots into memory at application startup.
/// This is read-only seed data: changes are not persisted and reset on restart.
/// </summary>
public sealed class WorkbookDataStore : IDisposable
{
    private readonly Dictionary<string, JsonDocument> _workbooks =
        new(StringComparer.OrdinalIgnoreCase);

    public WorkbookDataStore(IWebHostEnvironment environment)
    {
        var seedDirectory = Path.Combine(environment.ContentRootPath, "Data", "SeedData");

        LoadWorkbook(seedDirectory, "applications-tools");
        LoadWorkbook(seedDirectory, "technologies");
    }

    private void LoadWorkbook(string seedDirectory, string workbookName)
    {
        var path = Path.Combine(seedDirectory, $"{workbookName}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Seed data file was not found: {path}. Ensure Data/SeedData is copied into the project.",
                path);
        }

        _workbooks[workbookName] = JsonDocument.Parse(File.ReadAllText(path));
    }

    public IEnumerable<object> GetSummary()
    {
        foreach (var (workbookName, document) in _workbooks)
        {
            var sheets = document.RootElement.GetProperty("sheets");
            var sheetSummary = new List<object>();

            foreach (var sheet in sheets.EnumerateObject())
            {
                var rowCount = sheet.Value.GetProperty("rowCount").GetInt32();
                var columnCount = sheet.Value.GetProperty("columnCount").GetInt32();

                sheetSummary.Add(new
                {
                    sheetName = sheet.Name,
                    rowCountIncludingHeader = rowCount,
                    dataRowCount = Math.Max(0, rowCount - 1),
                    columnCount
                });
            }

            yield return new
            {
                workbookName,
                sourceFile = document.RootElement.GetProperty("sourceFile").GetString(),
                sheets = sheetSummary
            };
        }
    }

    public bool TryGetSheet(string workbookName, string sheetName, out JsonElement sheet)
    {
        sheet = default;

        if (!_workbooks.TryGetValue(workbookName, out var document))
            return false;

        if (!document.RootElement.TryGetProperty("sheets", out var sheets))
            return false;

        foreach (var property in sheets.EnumerateObject())
        {
            if (string.Equals(property.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                sheet = property.Value.Clone();
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        foreach (var document in _workbooks.Values)
            document.Dispose();

        _workbooks.Clear();
    }
}
