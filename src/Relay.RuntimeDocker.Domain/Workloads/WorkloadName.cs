using System.Text.RegularExpressions;
using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed partial record WorkloadName
{
    private WorkloadName(string value) => Value = value;

    public string Value { get; }

    public static Result<WorkloadName> Create(string? name) =>
        name is not null && DnsLabel().IsMatch(name)
            ? Result.Success(new WorkloadName(name))
            : Result.Failure<WorkloadName>(WorkloadErrors.InvalidName(name));

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$")]
    private static partial Regex DnsLabel();
}
