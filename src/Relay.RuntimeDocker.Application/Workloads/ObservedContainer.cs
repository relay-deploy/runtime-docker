using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record ObservedContainer(string Id, ProjectId Project, WorkloadStatus Status, WorkloadExit LastExit);
