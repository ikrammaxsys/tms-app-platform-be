namespace tms_template_net8.Models.ViewModels;

/// <summary>
/// Column definition for the custom (non UI-Foundation) data table.
/// </summary>
public sealed class DataTableColumn
{
    /// <summary>Property key used to look up the value in each row dictionary (camelCase).</summary>
    public string Data { get; set; } = string.Empty;
    /// <summary>Column header text.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Optional CSS width, e.g. "10%" or "120px".</summary>
    public string? Width { get; set; }
    /// <summary>Optional renderer name. Supported: "__status".</summary>
    public string? RenderName { get; set; }
    /// <summary>Whether the column can be sorted by clicking the header.</summary>
    public bool Orderable { get; set; } = true;
}

/// <summary>
/// View model for the server-rendered custom data table. Data is passed in directly
/// (no remote <c>ui-source</c> fetch); client-side JS only handles search / sort / paging.
/// </summary>
public sealed class DataTableViewModel
{
    public string Id { get; set; } = "dataTable";
    public List<DataTableColumn> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>Row-click navigation template, e.g. "~/ApplicationManagement/Detail/{id}".</summary>
    public string? RouteTemplate { get; set; }
    /// <summary>Row field used to substitute the <c>{id}</c> placeholder in <see cref="RouteTemplate"/>.</summary>
    public string IdField { get; set; } = "id";
    public bool IncludeCheckbox { get; set; }
    public bool RowClickRedirect { get; set; }
    public int PageLength { get; set; } = 10;
    public bool Searching { get; set; } = true;
    public string EmptyMessage { get; set; } = "No records found.";
}
