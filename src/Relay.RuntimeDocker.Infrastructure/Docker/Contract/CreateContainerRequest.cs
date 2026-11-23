namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record CreateContainerRequest
{
    public required string Image { get; init; }

    public required IReadOnlyList<string> Env { get; init; }

    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    public required HostConfigRequest HostConfig { get; init; }

    public required NetworkingConfigRequest NetworkingConfig { get; init; }
}
