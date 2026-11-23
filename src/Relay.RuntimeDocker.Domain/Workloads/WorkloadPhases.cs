namespace Relay.RuntimeDocker.Domain.Workloads;

public static class WorkloadPhases
{
    public static WorkloadPhase FromContainerState(string? state, bool oomKilled) => state switch
    {
        "running" => WorkloadPhase.Running,
        "restarting" => WorkloadPhase.Restarting,
        "paused" or "removing" => WorkloadPhase.Stopped,
        "exited" => oomKilled ? WorkloadPhase.Crashed : WorkloadPhase.Stopped,
        "dead" => WorkloadPhase.Crashed,
        _ => WorkloadPhase.Pending,
    };
}
