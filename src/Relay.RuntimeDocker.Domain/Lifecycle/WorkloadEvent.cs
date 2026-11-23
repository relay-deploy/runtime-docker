using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public abstract record WorkloadEvent(AppId App, ProjectId Project, ReleaseId Release);
