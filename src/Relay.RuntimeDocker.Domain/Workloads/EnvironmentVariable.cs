using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record EnvironmentVariable(string Name, string Value)
{
    public static Result<EnvironmentVariable> Create(string? name, string? value) =>
        string.IsNullOrWhiteSpace(name) || name.Contains('=', StringComparison.Ordinal)
            ? Result.Failure<EnvironmentVariable>(WorkloadErrors.InvalidEnvironmentVariable(name))
            : Result.Success(new EnvironmentVariable(name, value ?? string.Empty));
}
