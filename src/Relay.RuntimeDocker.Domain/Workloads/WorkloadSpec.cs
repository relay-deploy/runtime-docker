using Relay.RuntimeDocker.Domain.Images;

namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record WorkloadSpec(
    ImageReference Image,
    IReadOnlyList<EnvironmentVariable> Environment,
    IReadOnlyList<WorkloadRoute> Routes,
    ResourceLimits Resources);
