namespace Relay.RuntimeDocker.Domain.Workloads;

public static class WorkloadNetworks
{
    public static string ForProject(ProjectId project) => $"relay-project-{project}";
}
