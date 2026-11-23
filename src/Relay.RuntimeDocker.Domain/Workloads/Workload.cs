namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record Workload(AppId App, ProjectId Project, ReleaseId Release, WorkloadName Name, WorkloadSpec Spec);
