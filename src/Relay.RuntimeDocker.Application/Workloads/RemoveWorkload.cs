using Microsoft.Extensions.Logging;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed partial class RemoveWorkload(IContainerEngine engine, ILogger<RemoveWorkload> logger)
{
    public async Task<Result> Handle(RemoveWorkloadCommand command, CancellationToken cancellationToken)
    {
        var existing = await engine.FindContainer(command.App, cancellationToken);
        if (existing.IsFailure)
        {
            return existing.Error;
        }

        var removed = await engine.RemoveContainer(existing.Value, cancellationToken);
        if (removed.IsSuccess)
        {
            LogRemoved(command.App);
        }

        return removed;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed the workload of app {AppId}")]
    private partial void LogRemoved(AppId appId);
}
