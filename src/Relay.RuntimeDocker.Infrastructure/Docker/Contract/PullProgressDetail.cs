namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record PullProgressDetail
{
    public long Current { get; init; }

    public long Total { get; init; }
}
