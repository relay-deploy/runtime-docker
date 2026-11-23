using System.ComponentModel.DataAnnotations;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

public sealed class DockerOptions
{
    public const string Section = "Relay:Docker";

    [Required]
    public string Socket { get; init; } = "/var/run/docker.sock";

    [Required]
    [RegularExpression(@"^v\d+\.\d+$")]
    public string ApiVersion { get; init; } = "v1.47";

    [Required]
    public string EdgeNetwork { get; init; } = "relay-edge";
}
