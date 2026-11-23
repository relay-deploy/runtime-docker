using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public readonly record struct ReleaseId(Guid Value)
{
    public static Result<ReleaseId> Parse(string? text) =>
        Guid.TryParse(text, out var value)
            ? Result.Success(new ReleaseId(value))
            : Result.Failure<ReleaseId>(WorkloadErrors.InvalidId("release_id", text));

    public override string ToString() => Value.ToString();
}
