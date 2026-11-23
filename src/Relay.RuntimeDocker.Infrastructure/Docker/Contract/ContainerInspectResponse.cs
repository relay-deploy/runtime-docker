namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record ContainerInspectResponse
{
    public string Id { get; init; } = string.Empty;

    public string Image { get; init; } = string.Empty;

    public int RestartCount { get; init; }

    public ContainerStateView State { get; init; } = new();

    public ContainerConfigView Config { get; init; } = new();
}
