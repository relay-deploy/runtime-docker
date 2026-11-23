namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record ResourceLimits(ulong MemoryLimitBytes, uint CpuLimitMillicores);
