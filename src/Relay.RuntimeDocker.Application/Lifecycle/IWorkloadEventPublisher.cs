using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Lifecycle;

namespace Relay.RuntimeDocker.Application.Lifecycle;

public interface IWorkloadEventPublisher
{
    Task<Result> Publish(WorkloadEvent workloadEvent, CancellationToken cancellationToken);
}
