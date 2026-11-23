namespace Relay.RuntimeDocker.Application.Workloads;

public enum ApplyStep
{
    PullingImage,
    ReplacingWorkload,
    StartingWorkload,
}
