namespace Relay.RuntimeDocker.Application.Lifecycle;

public sealed record WatchWorkloadsCommand(TimeSpan StabilityWindow, TimeSpan RetryDelay);
