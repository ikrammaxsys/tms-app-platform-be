namespace tms_template_net8.Data.Repositories;

/// <summary>
/// Primary access for reading Nagios data and writing it into the app (Default) database.
/// </summary>
public interface INagiosRepository
{
    /// <summary>
    /// End-to-end sync: fetch HOST/SERVICE/REMARKS from Nagios, parse, insert into local tables.
    /// </summary>
    Task SyncAsync(CancellationToken cancellationToken = default);
}
