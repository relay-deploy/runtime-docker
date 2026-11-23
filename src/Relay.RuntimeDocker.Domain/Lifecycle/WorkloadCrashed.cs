using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public sealed record WorkloadCrashed(AppId App, ProjectId Project, ReleaseId Release, WorkloadExit Exit, int RestartCount)
    : WorkloadEvent(App, Project, Release);
