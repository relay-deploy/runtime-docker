namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record EventMessage
{
    public string Type { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public EventActor Actor { get; init; } = new();
}
