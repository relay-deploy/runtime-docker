namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record HostConfigRequest
{
    public long? Memory { get; init; }

    public long? NanoCpus { get; init; }

    public required RestartPolicyRequest RestartPolicy { get; init; }

    public required string NetworkMode { get; init; }
}
