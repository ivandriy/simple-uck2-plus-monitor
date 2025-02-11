using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleUCK2PlusMonitor.Services.Monitoring;
using SimpleUCK2PlusMonitor.Services.Options;

namespace SimpleUCK2PlusMonitor.Services;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerOptions _options;

    public Worker(IServiceScopeFactory serviceScopeFactory, IOptions<WorkerOptions> options, ILogger<Worker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service started");
        await RunMonitoring();
       
        using PeriodicTimer timer = new(_options.PullingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunMonitoring();
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogCritical("Worker service is stopping due to OperationCanceledException: {ExMessage}",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Worker service is stopping due to unexpected exception: {ExMessage}",
                ex.Message);
        }
    }

    private async Task RunMonitoring()
    {
        _logger.LogInformation("Try to update monitoring data");
        using var scope = _serviceScopeFactory.CreateScope();
        var monitoringService =
            scope.ServiceProvider.GetRequiredService<IMonitoringService>();
        try
        {
            await monitoringService.GetData();
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to update monitoring data due to exception: {ExMessage}", ex.Message);
        }
        
    }
}