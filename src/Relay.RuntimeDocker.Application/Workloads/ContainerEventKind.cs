namespace Relay.RuntimeDocker.Application.Workloads;

public enum ContainerEventKind
{
    Created,
    Started,
    OutOfMemory,
    Died,
    Stopped,
    Destroyed,
}
