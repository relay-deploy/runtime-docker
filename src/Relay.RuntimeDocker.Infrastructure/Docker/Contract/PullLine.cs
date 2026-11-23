namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record PullLine
{
    public string? Id { get; init; }

    public string? Error { get; init; }

    public PullProgressDetail? ProgressDetail { get; init; }
}
