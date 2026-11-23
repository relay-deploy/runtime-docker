using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.RuntimeDocker.Application.Lifecycle;

namespace Relay.RuntimeDocker.Infrastructure.Lifecycle;

internal sealed partial class WorkloadWatcher(
    WatchWorkloads watchWorkloads,
    IOptions<LifecycleOptions> options,
    TimeProvider time,
    ILogger<WorkloadWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var command = new WatchWorkloadsCommand(options.Value.StabilityWindow, options.Value.RetryDelay);
        while (!stoppingToken.IsCancellationRequested)
        {
            var watched = await watchWorkloads.Handle(command, stoppingToken);
            if (watched.IsFailure)
            {
                LogWatchInterrupted(watched.Error.Code, watched.Error.Message, command.RetryDelay);
            }

            await Task.Delay(command.RetryDelay, time, stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lost the Docker event stream ({Code}: {Reason}), reconnecting in {Delay}")]
    private partial void LogWatchInterrupted(string code, string reason, TimeSpan delay);
}
