using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed class GetWorkload(IContainerEngine engine)
{
    public async Task<Result<WorkloadStatus>> Handle(GetWorkloadQuery query, CancellationToken cancellationToken)
    {
        var existing = await engine.FindContainer(query.App, cancellationToken);
        return existing.Map(container => container.Status);
    }
}
