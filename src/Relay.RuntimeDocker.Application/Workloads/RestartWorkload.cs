using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed class RestartWorkload(IContainerEngine engine)
{
    public async Task<Result<WorkloadStatus>> Handle(RestartWorkloadCommand command, CancellationToken cancellationToken)
    {
        var existing = await engine.FindContainer(command.App, cancellationToken);
        if (existing.IsFailure)
        {
            return existing.Error;
        }

        var restarted = await engine.RestartContainer(existing.Value, cancellationToken);
        if (restarted.IsFailure)
        {
            return restarted.Error;
        }

        var observed = await engine.FindContainer(command.App, cancellationToken);
        return observed.Map(container => container.Status);
    }
}
