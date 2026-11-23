using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

internal sealed class DockerHealthCheck(DockerApiClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var pinged = await client.Ping(cancellationToken);
        return pinged.IsSuccess ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(pinged.Error.Message);
    }
}
