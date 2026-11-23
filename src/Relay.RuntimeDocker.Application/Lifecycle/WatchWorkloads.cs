using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Domain.Lifecycle;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Lifecycle;

public sealed partial class WatchWorkloads(
    IContainerEngine engine,
    IWorkloadEventPublisher publisher,
    TimeProvider time,
    ILogger<WatchWorkloads> logger)
{
    private static readonly TimeSpan replayMargin = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, WatchedWorkload> watched = [];

    public async Task<Result> Handle(WatchWorkloadsCommand command, CancellationToken cancellationToken)
    {
        var since = time.GetUtcNow() - replayMargin;
        var signals = Channel.CreateUnbounded<WatchSignal>(new UnboundedChannelOptions { SingleReader = true });
        var caughtUp = await CatchUp(command, signals.Writer, cancellationToken);
        if (caughtUp.IsFailure)
        {
            return caughtUp;
        }

        var watching = Watch(since, signals.Writer, cancellationToken);
        await foreach (var signal in signals.Reader.ReadAllAsync(cancellationToken))
        {
            var handled = signal switch
            {
                ContainerChanged changed => await Follow(changed.Event, command, signals.Writer, cancellationToken),
                SettleDue due => await Settle(due, command, cancellationToken),
                _ => Result.Success(),
            };

            if (handled.IsFailure)
            {
                LogSignalFailed(handled.Error.Code, handled.Error.Message);
            }
        }

        return await watching;
    }

    private async Task<Result> Watch(DateTimeOffset since, ChannelWriter<WatchSignal> signals, CancellationToken cancellationToken)
    {
        try
        {
            return await engine.WatchEvents(since, change => signals.TryWrite(new ContainerChanged(change)), cancellationToken);
        }
        finally
        {
            signals.TryComplete();
        }
    }

    private async Task<Result> CatchUp(WatchWorkloadsCommand command, ChannelWriter<WatchSignal> signals, CancellationToken cancellationToken)
    {
        var listed = await engine.ListContainers(null, cancellationToken);
        if (listed.IsFailure)
        {
            return listed.Error;
        }

        var live = listed.Value.Select(container => container.Id).ToHashSet();
        foreach (var gone in watched.Keys.Where(id => !live.Contains(id)).ToList())
        {
            watched.Remove(gone);
        }

        foreach (var container in listed.Value)
        {
            if (watched.TryGetValue(container.Id, out var workload))
            {
                await PublishIfAny(workload.Exited(container.LastExit, container.Status), command, cancellationToken);
            }
            else
            {
                workload = Baseline(container);
            }

            ScheduleSettle(workload, command, signals, cancellationToken);
        }

        LogCaughtUp(watched.Count);
        return Result.Success();
    }

    private Task<Result> Follow(
        ContainerEvent change,
        WatchWorkloadsCommand command,
        ChannelWriter<WatchSignal> signals,
        CancellationToken cancellationToken) => change.Kind switch
        {
            ContainerEventKind.Destroyed => Task.FromResult(Forget(change.ContainerId)),
            ContainerEventKind.OutOfMemory => RanOutOfMemory(change.ContainerId, command, signals, cancellationToken),
            ContainerEventKind.Died => Died(change, command, signals, cancellationToken),
            _ => Changed(change.ContainerId, command, signals, cancellationToken),
        };

    private async Task<Result> RanOutOfMemory(
        string containerId,
        WatchWorkloadsCommand command,
        ChannelWriter<WatchSignal> signals,
        CancellationToken cancellationToken)
    {
        var workload = await Watched(containerId, cancellationToken);
        if (workload.IsFailure)
        {
            return Forget(containerId, workload.Error);
        }

        workload.Value.RanOutOfMemory();
        ScheduleSettle(workload.Value, command, signals, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> Died(
        ContainerEvent change,
        WatchWorkloadsCommand command,
        ChannelWriter<WatchSignal> signals,
        CancellationToken cancellationToken)
    {
        var workload = await Watched(change.ContainerId, cancellationToken);
        if (workload.IsFailure)
        {
            return Forget(change.ContainerId, workload.Error);
        }

        var observed = await engine.ObserveContainer(change.ContainerId, cancellationToken);
        if (observed.IsFailure)
        {
            return Forget(change.ContainerId, observed.Error);
        }

        var exit = new WorkloadExit(change.ExitCode, observed.Value.LastExit.OutOfMemory);
        await PublishIfAny(workload.Value.Exited(exit, observed.Value.Status), command, cancellationToken);
        ScheduleSettle(workload.Value, command, signals, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> Changed(
        string containerId,
        WatchWorkloadsCommand command,
        ChannelWriter<WatchSignal> signals,
        CancellationToken cancellationToken)
    {
        var workload = await Watched(containerId, cancellationToken);
        if (workload.IsFailure)
        {
            return Forget(containerId, workload.Error);
        }

        ScheduleSettle(workload.Value, command, signals, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> Settle(SettleDue due, WatchWorkloadsCommand command, CancellationToken cancellationToken)
    {
        if (!watched.TryGetValue(due.ContainerId, out var workload) || workload.Generation != due.Generation)
        {
            return Result.Success();
        }

        var observed = await engine.ObserveContainer(due.ContainerId, cancellationToken);
        if (observed.IsFailure)
        {
            return Forget(due.ContainerId, observed.Error);
        }

        await PublishIfAny(workload.Settle(due.Generation, observed.Value.Status), command, cancellationToken);
        return Result.Success();
    }

    private async Task<Result<WatchedWorkload>> Watched(string containerId, CancellationToken cancellationToken)
    {
        if (watched.TryGetValue(containerId, out var workload))
        {
            return workload;
        }

        var observed = await engine.ObserveContainer(containerId, cancellationToken);
        return observed.Map(Baseline);
    }

    private WatchedWorkload Baseline(ObservedContainer container)
    {
        var workload = WatchedWorkload.Baseline(container.Id, container.Project, container.Status, container.LastExit);
        watched[container.Id] = workload;
        return workload;
    }

    private Result Forget(string containerId)
    {
        watched.Remove(containerId);
        return Result.Success();
    }

    private Result Forget(string containerId, Error error)
    {
        watched.Remove(containerId);
        return error.Kind == ErrorKind.NotFound ? Result.Success() : error;
    }

    private void ScheduleSettle(
        WatchedWorkload workload,
        WatchWorkloadsCommand command,
        ChannelWriter<WatchSignal> signals,
        CancellationToken cancellationToken)
    {
        var due = new SettleDue(workload.ContainerId, workload.Changed());
        _ = Task.Delay(command.StabilityWindow, time, cancellationToken).ContinueWith(
            _ => signals.TryWrite(due),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }

    private async Task PublishIfAny(WorkloadEvent? workloadEvent, WatchWorkloadsCommand command, CancellationToken cancellationToken)
    {
        if (workloadEvent is null)
        {
            return;
        }

        while (true)
        {
            var published = await publisher.Publish(workloadEvent, cancellationToken);
            if (published.IsSuccess)
            {
                LogPublished(workloadEvent.GetType().Name, workloadEvent.App, workloadEvent.Release);
                return;
            }

            if (published.Error.Kind != ErrorKind.Unavailable)
            {
                LogDropped(workloadEvent.GetType().Name, workloadEvent.App, published.Error.Code, published.Error.Message);
                return;
            }

            LogPublishDeferred(workloadEvent.GetType().Name, workloadEvent.App, published.Error.Message, command.RetryDelay);
            await Task.Delay(command.RetryDelay, time, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Watching {Count} workloads")]
    private partial void LogCaughtUp(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "{EventType} for app {AppId} on release {ReleaseId}")]
    private partial void LogPublished(string eventType, AppId appId, ReleaseId releaseId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't record {EventType} for app {AppId} ({Reason}), retrying in {Delay}")]
    private partial void LogPublishDeferred(string eventType, AppId appId, string reason, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dropped {EventType} for app {AppId} ({Code}: {Reason})")]
    private partial void LogDropped(string eventType, AppId appId, string code, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't follow a container change ({Code}: {Reason})")]
    private partial void LogSignalFailed(string code, string reason);
}
