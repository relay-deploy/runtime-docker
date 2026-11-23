using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Domain.Images;
using Relay.RuntimeDocker.Domain.Workloads;

namespace Relay.RuntimeDocker.Application.Workloads;

public interface IContainerEngine
{
    string EdgeNetwork { get; }

    Task<Result<string>> PullImage(ImageReference image, IProgress<PullProgress> progress, CancellationToken cancellationToken);

    Task<Result<ObservedContainer>> FindContainer(AppId app, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ObservedContainer>>> ListContainers(ProjectId? project, CancellationToken cancellationToken);

    Task<Result> EnsureNetwork(string name, CancellationToken cancellationToken);

    Task<Result<ObservedContainer>> CreateAndStartContainer(
        Workload workload,
        IReadOnlyList<string> networks,
        CancellationToken cancellationToken);

    Task<Result> RestartContainer(ObservedContainer container, CancellationToken cancellationToken);

    Task<Result> StopContainer(ObservedContainer container, CancellationToken cancellationToken);

    Task<Result> RemoveContainer(ObservedContainer container, CancellationToken cancellationToken);
}
