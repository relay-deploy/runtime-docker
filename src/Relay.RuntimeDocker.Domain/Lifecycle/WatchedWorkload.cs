using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public sealed class WatchedWorkload
{
    private WatchedPhase phase;
    private int restartCount;
    private WorkloadExit lastExit;
    private bool ranOutOfMemory;

    private WatchedWorkload(string containerId, ProjectId project, WorkloadStatus status, WorkloadExit lastExit)
    {
        ContainerId = containerId;
        App = status.App;
        Project = project;
        Release = status.Release;
        phase = status.Phase == WorkloadPhase.Restarting ? WatchedPhase.Crashed : SettledPhaseOf(status.Phase) ?? WatchedPhase.Running;
        restartCount = status.RestartCount;
        this.lastExit = lastExit;
    }

    public string ContainerId { get; }

    public AppId App { get; }

    public ProjectId Project { get; }

    public ReleaseId Release { get; }

    public int Generation { get; private set; }

    public static WatchedWorkload Baseline(string containerId, ProjectId project, WorkloadStatus status, WorkloadExit lastExit) =>
        new(containerId, project, status, lastExit);

    public int Changed() => ++Generation;

    public void RanOutOfMemory() => ranOutOfMemory = true;

    public WorkloadCrashed? Exited(WorkloadExit exit, WorkloadStatus observed)
    {
        lastExit = exit with { OutOfMemory = exit.OutOfMemory || ranOutOfMemory };
        ranOutOfMemory = false;
        var restartedByEngine = observed.RestartCount > restartCount;
        restartCount = observed.RestartCount;
        if (!restartedByEngine)
        {
            return null;
        }

        phase = WatchedPhase.Crashed;
        return new WorkloadCrashed(App, Project, Release, lastExit, restartCount);
    }

    public WorkloadEvent? Settle(int generation, WorkloadStatus observed)
    {
        if (generation != Generation || observed.RestartCount != restartCount)
        {
            return null;
        }

        ranOutOfMemory = false;
        if (SettledPhaseOf(observed.Phase) is not { } settled || settled == phase)
        {
            return null;
        }

        phase = settled;
        return settled == WatchedPhase.Running
            ? new WorkloadRecovered(App, Project, Release, restartCount)
            : new WorkloadStopped(App, Project, Release, lastExit.Code);
    }

    private static WatchedPhase? SettledPhaseOf(WorkloadPhase observed) => observed switch
    {
        WorkloadPhase.Running => WatchedPhase.Running,
        WorkloadPhase.Stopped or WorkloadPhase.Crashed => WatchedPhase.Stopped,
        _ => null,
    };
}
