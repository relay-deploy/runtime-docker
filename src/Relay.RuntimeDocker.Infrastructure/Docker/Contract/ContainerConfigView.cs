namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record ContainerConfigView
{
    public string Image { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string>? Labels { get; init; }
}
