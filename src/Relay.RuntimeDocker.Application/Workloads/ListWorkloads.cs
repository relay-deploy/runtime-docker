using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed class ListWorkloads(IContainerEngine engine)
{
    public async Task<Result<IReadOnlyList<WorkloadStatus>>> Handle(ListWorkloadsQuery query, CancellationToken cancellationToken)
    {
        var containers = await engine.ListContainers(query.Project, cancellationToken);
        return containers.Map<IReadOnlyList<WorkloadStatus>>(found => found.Select(container => container.Status).ToList());
    }
}
