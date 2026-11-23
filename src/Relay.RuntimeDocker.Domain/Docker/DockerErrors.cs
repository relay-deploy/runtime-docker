using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Docker;

public static class DockerErrors
{
    public static Error Unavailable(string detail) =>
        Error.Unavailable("docker.unavailable", $"The Docker engine is unavailable: {detail}");

    public static Error RequestFailed(int statusCode, string detail) =>
        Error.Unavailable("docker.request_failed", $"The Docker engine answered {statusCode}: {detail}");

    public static Error ContainerNotFound(string container) =>
        Error.NotFound("docker.container_not_found", $"Container {container} was not found.");
}
