namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record ContainerSummary
{
    public string Id { get; init; } = string.Empty;
}
