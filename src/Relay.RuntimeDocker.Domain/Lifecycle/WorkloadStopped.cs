using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public sealed record WorkloadStopped(AppId App, ProjectId Project, ReleaseId Release, int ExitCode)
    : WorkloadEvent(App, Project, Release);
