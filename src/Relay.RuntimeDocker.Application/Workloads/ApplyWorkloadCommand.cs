using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record ApplyWorkloadCommand(Workload Workload, IProgress<ApplyProgress> Progress);
