namespace Relay.RuntimeDocker.Domain.Workloads;

public enum WorkloadPhase
{
    Pending,
    Running,
    Restarting,
    Stopped,
    Crashed,
}
