using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Lifecycle;

public static class LifecycleErrors
{
    public static Error OutboxUnavailable(string detail) =>
        Error.Unavailable("lifecycle.outbox_unavailable", $"Workload events can't be recorded: {detail}");
}
