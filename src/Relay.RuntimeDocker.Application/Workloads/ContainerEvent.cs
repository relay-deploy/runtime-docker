namespace Relay.RuntimeDocker.Application.Workloads;

public sealed record ContainerEvent(string ContainerId, ContainerEventKind Kind, int ExitCode);
