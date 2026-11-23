using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public sealed record WorkloadRecovered(AppId App, ProjectId Project, ReleaseId Release, int RestartCount)
    : WorkloadEvent(App, Project, Release);
