namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record EndpointConfigRequest
{
    public IReadOnlyList<string>? Aliases { get; init; }
}
