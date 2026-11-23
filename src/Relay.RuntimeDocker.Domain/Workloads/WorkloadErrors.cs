using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Workloads;

public static class WorkloadErrors
{
    public static Error NotFound(AppId app) =>
        Error.NotFound("workload.not_found", $"No workload exists for app {app}.");

    public static Error NameConflict(string name) =>
        Error.Conflict("workload.name_conflict", $"A container named {name} already exists for another app.");

    public static Error InvalidId(string field, string? text) =>
        Error.Validation("workload.invalid_id", $"{field} '{text}' is not a valid id.");

    public static Error InvalidName(string? name) =>
        Error.Validation("workload.invalid_name", $"Workload name '{name}' is not a valid DNS label.");

    public static Error InvalidRoute(string? host, uint port) =>
        Error.Validation("workload.invalid_route", $"Route '{host}' on port {port} is not valid.");

    public static Error InvalidEnvironmentVariable(string? name) =>
        Error.Validation("workload.invalid_environment_variable", $"Environment variable name '{name}' is not valid.");
}
