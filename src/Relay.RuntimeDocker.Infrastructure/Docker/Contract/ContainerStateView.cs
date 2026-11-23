namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record ContainerStateView
{
    public string Status { get; init; } = string.Empty;

    public bool OOMKilled { get; init; }
}
