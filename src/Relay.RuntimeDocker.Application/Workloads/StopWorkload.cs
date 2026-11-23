using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed class StopWorkload(IContainerEngine engine)
{
    public async Task<Result<WorkloadStatus>> Handle(StopWorkloadCommand command, CancellationToken cancellationToken)
    {
        var existing = await engine.FindContainer(command.App, cancellationToken);
        if (existing.IsFailure)
        {
            return existing.Error;
        }

        var stopped = await engine.StopContainer(existing.Value, cancellationToken);
        if (stopped.IsFailure)
        {
            return stopped.Error;
        }

        var observed = await engine.FindContainer(command.App, cancellationToken);
        return observed.Map(container => container.Status);
    }
}
