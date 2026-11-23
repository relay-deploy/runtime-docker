namespace Relay.RuntimeDocker.Domain.Workloads;

public sealed record WorkloadStatus(
    AppId App,
    ReleaseId Release,
    WorkloadPhase Phase,
    string ImageDigest,
    int RestartCount,
    DateTimeOffset ObservedAt);
