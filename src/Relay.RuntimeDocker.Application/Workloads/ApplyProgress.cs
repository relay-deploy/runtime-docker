namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record ApplyProgress(ApplyStep Step, ulong CompletedBytes, ulong TotalBytes);
