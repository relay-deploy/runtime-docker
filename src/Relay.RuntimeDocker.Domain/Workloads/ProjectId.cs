using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public readonly record struct ProjectId(Guid Value)
{
    public static Result<ProjectId> Parse(string? text) =>
        Guid.TryParse(text, out var value)
            ? Result.Success(new ProjectId(value))
            : Result.Failure<ProjectId>(WorkloadErrors.InvalidId("project_id", text));

    public override string ToString() => Value.ToString();
}
