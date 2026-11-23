using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record WorkloadRoute(string Host, int Port)
{
    public static Result<WorkloadRoute> Create(string? host, uint port) =>
        string.IsNullOrWhiteSpace(host) || port is 0 or > 65535
            ? Result.Failure<WorkloadRoute>(WorkloadErrors.InvalidRoute(host, port))
            : Result.Success(new WorkloadRoute(host, (int)port));
}
