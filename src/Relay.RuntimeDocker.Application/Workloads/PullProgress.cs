namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record PullProgress(ulong CompletedBytes, ulong TotalBytes);
