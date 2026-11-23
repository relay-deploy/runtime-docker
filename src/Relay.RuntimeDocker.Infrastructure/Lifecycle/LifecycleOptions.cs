using System.ComponentModel.DataAnnotations;

namespace Relay.RuntimeDocker.Infrastructure.Lifecycle;

public sealed class LifecycleOptions
{
    public const string Section = "Relay:Lifecycle";

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan StabilityWindow { get; init; } = TimeSpan.FromSeconds(10);

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);
}
