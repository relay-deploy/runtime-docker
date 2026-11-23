namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record EventActor
{
    public string Id { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
