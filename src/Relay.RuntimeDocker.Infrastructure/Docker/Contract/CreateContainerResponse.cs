namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record CreateContainerResponse
{
    public string Id { get; init; } = string.Empty;
}
