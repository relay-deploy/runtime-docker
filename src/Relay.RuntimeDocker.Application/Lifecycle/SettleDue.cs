namespace Relay.RuntimeDocker.Application.Lifecycle;

internal sealed record SettleDue(string ContainerId, int Generation) : WatchSignal;
