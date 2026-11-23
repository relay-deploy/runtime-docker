using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record ObservedContainer(string Id, WorkloadStatus Status);
