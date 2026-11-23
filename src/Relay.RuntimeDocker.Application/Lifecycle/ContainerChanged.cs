using Relay.RuntimeDocker.Application.Workloads;

namespace Relay.RuntimeDocker.Application.Lifecycle;

internal sealed record ContainerChanged(ContainerEvent Event) : WatchSignal;
