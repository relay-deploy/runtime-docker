namespace Relay.RuntimeDocker.Domain.Workloads;

public readonly record struct WorkloadExit(int Code, bool OutOfMemory);
