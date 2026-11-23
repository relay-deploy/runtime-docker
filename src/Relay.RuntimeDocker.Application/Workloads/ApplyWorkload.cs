using Microsoft.Extensions.Logging;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public sealed partial class ApplyWorkload(IContainerEngine engine, ILogger<ApplyWorkload> logger)
{
    public async Task<Result<WorkloadStatus>> Handle(ApplyWorkloadCommand command, CancellationToken cancellationToken)
    {
        var workload = command.Workload;

        command.Progress.Report(new ApplyProgress(ApplyStep.PullingImage, 0, 0));
        var pulled = await engine.PullImage(workload.Spec.Image, new ApplyPullProgress(command.Progress), cancellationToken);
        if (pulled.IsFailure)
        {
            return pulled.Error;
        }

        command.Progress.Report(new ApplyProgress(ApplyStep.ReplacingWorkload, 0, 0));
        var replaced = await ReplaceExisting(workload.App, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        var networks = NetworksFor(workload);
        foreach (var network in networks)
        {
            var ensured = await engine.EnsureNetwork(network, cancellationToken);
            if (ensured.IsFailure)
            {
                return ensured.Error;
            }
        }

        command.Progress.Report(new ApplyProgress(ApplyStep.StartingWorkload, 0, 0));
        var started = await engine.CreateAndStartContainer(workload, networks, cancellationToken);
        if (started.IsFailure)
        {
            return started.Error;
        }

        LogApplied(workload.Release, workload.App, started.Value.Status.Phase);
        return started.Value.Status;
    }

    private async Task<Result> ReplaceExisting(AppId app, CancellationToken cancellationToken)
    {
        var existing = await engine.FindContainer(app, cancellationToken);
        if (existing.IsFailure)
        {
            return existing.Error.Kind == ErrorKind.NotFound ? Result.Success() : existing.Error;
        }

        var stopped = await engine.StopContainer(existing.Value, cancellationToken);
        if (stopped.IsFailure)
        {
            return stopped;
        }

        return await engine.RemoveContainer(existing.Value, cancellationToken);
    }

    private List<string> NetworksFor(Workload workload)
    {
        var networks = new List<string> { WorkloadNetworks.ForProject(workload.Project) };
        if (workload.Spec.Routes.Count > 0)
        {
            networks.Add(engine.EdgeNetwork);
        }

        return networks;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied release {ReleaseId} of app {AppId} as {Phase}")]
    private partial void LogApplied(ReleaseId releaseId, AppId appId, WorkloadPhase phase);
}
