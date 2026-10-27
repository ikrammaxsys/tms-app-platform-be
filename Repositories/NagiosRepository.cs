using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Common.Nagios;
using tms_template_net8.Common.Time;
using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Data.Repositories;

/// <summary>
/// Reads from the Nagios DB (<c>ConnectionStrings:Nagios</c>) and writes into the app DB
/// (<c>ConnectionStrings:Default</c>).
/// </summary>
public sealed class NagiosRepository : INagiosRepository
{
    /// <summary>Logical name resolved from <c>ConnectionStrings:Nagios</c>.</summary>
    private const string NagiosConnectionName = "Nagios";

    private const string FetchLinuxSql = """
        SELECT MHSS.HOST, MHSS.UPDATED_DATE AS UPDATEDDATE, MHSS.SERVICE, MHSS.REMARKS
        FROM MM_HOST_SERVICES_STATUS_SUMMARY MHSS
        JOIN MM_ITEM MMI ON MMI.ITEM_NAME = MHSS.HOST
        JOIN MM_CATEGORY MMC ON MMC.ID_MM_CATEGORY = MMI.ID_MM_CATEGORY
        WHERE MMC.ID_MM_CATEGORY = 12;
        """;

    private const string FetchWindowsSql = """
        SELECT MHSS.HOST, MHSS.UPDATED_DATE AS UPDATEDDATE, MHSS.SERVICE, MHSS.REMARKS
        FROM MM_HOST_SERVICES_STATUS_SUMMARY MHSS
        JOIN MM_ITEM MMI ON MMI.ITEM_NAME = MHSS.HOST
        JOIN MM_CATEGORY MMC ON MMC.ID_MM_CATEGORY = MMI.ID_MM_CATEGORY
        WHERE MMC.ID_MM_CATEGORY = 13;
        """;

    private readonly ISqlExecutor _sql;
    private readonly ILogger<NagiosRepository> _logger;

    public NagiosRepository(ISqlExecutor sql, ILogger<NagiosRepository> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var linuxRows = await FetchAsync(FetchLinuxSql, cancellationToken);
        var windowsRows = await FetchAsync(FetchWindowsSql, cancellationToken);

        var linux = NagiosRemarksParser.BuildLinuxSnapshots(linuxRows);
        var windows = NagiosRemarksParser.BuildWindowsSnapshots(windowsRows);

        _logger.LogInformation(
            "Nagios sync: {LinuxRows} linux rows → {LinuxHosts} hosts; {WindowsRows} windows rows → {WindowsHosts} hosts.",
            linuxRows.Count, linux.Count, windowsRows.Count, windows.Count);

        await InsertLinuxAsync(linux, cancellationToken);
        await InsertWindowsAsync(windows, cancellationToken);
    }

    private async Task<IReadOnlyList<NagiosServiceStatusRow>> FetchAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var rows = await _sql.QueryAsync<NagiosServiceStatusRow>(
            sql, CommandType.Text, null, NagiosConnectionName, cancellationToken);

        return rows.ToList();
    }

    private async Task InsertLinuxAsync(
        IReadOnlyList<NagiosLinuxServerItem> items,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.nagios_linux_server
                (name, current_load, swap, disk_usage, ping_ms, total_process, [timestamp])
            VALUES
                (@Name, @CurrentLoad, @Swap, @DiskUsage, @PingMs, @TotalProcess, @Timestamp);
            """;

         var lastByName = await GetLatestTimestampsAsync(
            "dbo.nagios_linux_server", cancellationToken);
            
        if(lastByName.TryGetValue("hostname", out var test) && IsLessThanOrEqualToMinute(items[0].Timestamp, test))
        {
            _logger.LogInformation(
                "Nagios linux insert: skipped (same timestamp). " + items[0].Timestamp);
            return;
        }

        foreach (var item in items)
        {
            await _sql.ExecuteNonQueryAsync(
                sql,
                CommandType.Text,
                [
                    DbStr("@Name", item.Name),
                    DbStr("@CurrentLoad", item.CurrentLoad),
                    DbStr("@Swap", item.Swap),
                    DbStr("@DiskUsage", item.DiskUsage),
                    DbStr("@PingMs", item.PingMs),
                    DbStr("@TotalProcess", item.TotalProcess),
                    DbDate("@Timestamp", item.Timestamp)
                ],
                null,
                cancellationToken);
        }
    }

    private async Task InsertWindowsAsync(
        IReadOnlyList<NagiosWindowServerItem> items,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.nagios_window_server
                (name, cpu_count, cpu_usage, total_memory, used_memory,
                total_disk_c, used_disk_c, total_disk_d, used_disk_d, uptime, [timestamp])
            VALUES
                (@Name, @CpuCount, @CpuUsage, @TotalMemory, @UsedMemory,
                @TotalDiskC, @UsedDiskC, @TotalDiskD, @UsedDiskD, @Uptime, @Timestamp);
            """;

        var lastByName = await GetLatestTimestampsAsync(
            "dbo.nagios_window_server", cancellationToken);

        if(lastByName.TryGetValue("hostname", out var test) && IsLessThanOrEqualToMinute(items[0].Timestamp, test))
        {
            _logger.LogInformation(
                "Nagios window insert: skipped (same timestamp). " + items[0].Timestamp);

            return;
        }

        foreach (var item in items)
        {
            await _sql.ExecuteNonQueryAsync(
                sql,
                CommandType.Text,
                [
                    DbStr("@Name", item.Name),
                    DbStr("@CpuCount", item.CpuCount),
                    DbStr("@CpuUsage", item.CpuUsage),
                    DbStr("@TotalMemory", item.TotalMemory),
                    DbStr("@UsedMemory", item.UsedMemory),
                    DbStr("@TotalDiskC", item.TotalDiskC),
                    DbStr("@UsedDiskC", item.UsedDiskC),
                    DbStr("@TotalDiskD", item.TotalDiskD),
                    DbStr("@UsedDiskD", item.UsedDiskD),
                    DbStr("@Uptime", item.Uptime),
                    DbDate("@Timestamp", item.Timestamp)
                ],
                null,
                cancellationToken);
        }
    }

    private async Task<Dictionary<string, DateTime>> GetLatestTimestampsAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
         // tableName is a fixed internal constant only — never user input.
        var sql = $"""
            SELECT TOP 1
                'hostname' AS Name,
                DATETRUNC(SECOND, [timestamp]) AS Timestamp
            FROM {tableName}
            WHERE name IS NOT NULL
            AND [timestamp] IS NOT NULL
            ORDER BY [timestamp] DESC;
            """;

        var rows = await _sql.QueryAsync<HostTimestampRow>(
            sql,
            CommandType.Text,
            null,
            null,
            cancellationToken);

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Timestamp.HasValue)
            .ToDictionary(
                r => r.Name.Trim(),
                r => r.Timestamp!.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static SqlParameter DbStr(string name, string? value) =>
        new(name, (object?)value ?? DBNull.Value);

    private static SqlParameter DbDate(string name, DateTime? value) =>
        new(name, MalaysiaTime.ForStorage(value));

    private sealed class HostTimestampRow
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
    }

     public static bool IsLessThanOrEqualToMinute(DateTime? value, DateTime? other)
    {
        if (!value.HasValue || !other.HasValue)
            return false;

        var valueMinute = new DateTime(
            value.Value.Year,
            value.Value.Month,
            value.Value.Day,
            value.Value.Hour,
            value.Value.Minute,
            0,
            value.Value.Kind);

        var otherMinute = new DateTime(
            other.Value.Year,
            other.Value.Month,
            other.Value.Day,
            other.Value.Hour,
            other.Value.Minute,
            0,
            other.Value.Kind);

        return valueMinute <= otherMinute;
    }
}
