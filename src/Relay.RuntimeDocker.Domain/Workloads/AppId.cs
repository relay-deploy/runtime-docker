using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public readonly record struct AppId(Guid Value)
{
    public static Result<AppId> Parse(string? text) =>
        Guid.TryParse(text, out var value)
            ? Result.Success(new AppId(value))
            : Result.Failure<AppId>(WorkloadErrors.InvalidId("app_id", text));

    public override string ToString() => Value.ToString();
}
