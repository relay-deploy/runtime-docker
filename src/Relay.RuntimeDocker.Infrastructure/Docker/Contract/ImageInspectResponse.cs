namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record ImageInspectResponse
{
    public string Id { get; init; } = string.Empty;

    public IReadOnlyList<string>? RepoDigests { get; init; }
}
