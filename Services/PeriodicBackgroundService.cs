using tms_template_net8.Data.Repositories;

namespace tms_template_net8.Services;

/// <summary>
/// Hosted background worker that syncs Nagios metrics every 5 minutes.
/// </summary>
public sealed class PeriodicBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PeriodicBackgroundService> _logger;

    public PeriodicBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PeriodicBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} started. Interval: {Interval}.", nameof(PeriodicBackgroundService), Interval);

        // Run once on startup, then every Interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteWorkAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Service} iteration failed.", nameof(PeriodicBackgroundService));
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("{Service} stopped.", nameof(PeriodicBackgroundService));
    }

    private async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var nagios = scope.ServiceProvider.GetRequiredService<INagiosRepository>();
        await nagios.SyncAsync(cancellationToken);
    }
}
