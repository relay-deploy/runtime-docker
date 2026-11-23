using System.Globalization;
using Microsoft.Extensions.Options;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Domain.Images;
using Relay.RuntimeDocker.Domain.Workloads;
using Relay.RuntimeDocker.Infrastructure.Docker.Contract;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

internal sealed class DockerContainerEngine(DockerApiClient client, IOptions<DockerOptions> options, TimeProvider time)
    : IContainerEngine
{
    private const long NanoCpusPerMillicore = 1_000_000;

    private static readonly Dictionary<string, string[]> eventFilters = new()
    {
        ["type"] = ["container"],
        ["label"] = [$"{ContainerLabels.Managed}=true"],
        ["event"] = ["create", "start", "oom", "die", "stop", "destroy"],
    };

    public string EdgeNetwork => options.Value.EdgeNetwork;

    public async Task<Result<string>> PullImage(
        ImageReference image,
        IProgress<PullProgress> progress,
        CancellationToken cancellationToken)
    {
        var pulled = await client.PullImage(image, progress, cancellationToken);
        if (pulled.IsFailure)
        {
            return pulled.Error;
        }

        var inspected = await client.InspectImage(image.Pinned, cancellationToken);
        return inspected.Map(inspect => DigestOf(inspect, image.Repository));
    }

    public async Task<Result<ObservedContainer>> FindContainer(AppId app, CancellationToken cancellationToken)
    {
        var ids = await client.ListContainerIds([$"{ContainerLabels.Managed}=true", $"{ContainerLabels.AppId}={app}"], cancellationToken);
        if (ids.IsFailure)
        {
            return ids.Error;
        }

        return ids.Value.Count == 0
            ? WorkloadErrors.NotFound(app)
            : await ObserveContainer(ids.Value[0], cancellationToken);
    }

    public async Task<Result<IReadOnlyList<ObservedContainer>>> ListContainers(
        ProjectId? project,
        CancellationToken cancellationToken)
    {
        var filters = new List<string> { $"{ContainerLabels.Managed}=true" };
        if (project is { } projectId)
        {
            filters.Add($"{ContainerLabels.ProjectId}={projectId}");
        }

        var ids = await client.ListContainerIds(filters, cancellationToken);
        if (ids.IsFailure)
        {
            return ids.Error;
        }

        var observed = new List<ObservedContainer>();
        foreach (var id in ids.Value)
        {
            var container = await ObserveContainer(id, cancellationToken);
            if (container.IsFailure)
            {
                return container.Error;
            }

            observed.Add(container.Value);
        }

        return Result.Success<IReadOnlyList<ObservedContainer>>(observed);
    }

    public Task<Result> EnsureNetwork(string name, CancellationToken cancellationToken) =>
        client.EnsureNetwork(name, cancellationToken);

    public async Task<Result<ObservedContainer>> CreateAndStartContainer(
        Workload workload,
        IReadOnlyList<string> networks,
        CancellationToken cancellationToken)
    {
        var created = await client.CreateContainer(workload.Name.Value, BuildRequest(workload, networks), cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var started = await client.StartContainer(created.Value, cancellationToken);
        if (started.IsFailure)
        {
            await client.RemoveContainer(created.Value, CancellationToken.None);
            return started.Error;
        }

        return await ObserveContainer(created.Value, cancellationToken);
    }

    public Task<Result> RestartContainer(ObservedContainer container, CancellationToken cancellationToken) =>
        client.RestartContainer(container.Id, cancellationToken);

    public Task<Result> StopContainer(ObservedContainer container, CancellationToken cancellationToken) =>
        client.StopContainer(container.Id, cancellationToken);

    public async Task<Result> RemoveContainer(ObservedContainer container, CancellationToken cancellationToken)
    {
        var removed = await client.RemoveContainer(container.Id, cancellationToken);
        return removed.IsFailure && removed.Error.Code == "docker.container_not_found" ? Result.Success() : removed;
    }

    public Task<Result> WatchEvents(DateTimeOffset since, Action<ContainerEvent> observe, CancellationToken cancellationToken) =>
        client.WatchEvents(
            since,
            eventFilters,
            message =>
            {
                if (ToContainerEvent(message) is { } change)
                {
                    observe(change);
                }
            },
            cancellationToken);

    public async Task<Result<ObservedContainer>> ObserveContainer(string id, CancellationToken cancellationToken)
    {
        var inspected = await client.InspectContainer(id, cancellationToken);
        if (inspected.IsFailure)
        {
            return inspected.Error;
        }

        var container = inspected.Value;
        var labels = container.Config.Labels ?? new Dictionary<string, string>();
        var app = AppId.Parse(labels.GetValueOrDefault(ContainerLabels.AppId));
        var project = ProjectId.Parse(labels.GetValueOrDefault(ContainerLabels.ProjectId));
        var release = ReleaseId.Parse(labels.GetValueOrDefault(ContainerLabels.ReleaseId));
        foreach (var parsed in new Result[] { app, project, release })
        {
            if (parsed.IsFailure)
            {
                return parsed.Error;
            }
        }

        var image = await client.InspectImage(container.Image, cancellationToken);
        var repository = ImageReference.Create(container.Config.Image, null);
        var digest = image.IsSuccess
            ? DigestOf(image.Value, repository.IsSuccess ? repository.Value.Repository : string.Empty)
            : container.Image;
        var phase = WorkloadPhases.FromContainerState(container.State.Status, container.State.OOMKilled);
        var status = new WorkloadStatus(app.Value, release.Value, phase, digest, container.RestartCount, time.GetUtcNow());
        return new ObservedContainer(container.Id, project.Value, status, new WorkloadExit(container.State.ExitCode, container.State.OOMKilled));
    }

    private static ContainerEvent? ToContainerEvent(EventMessage message)
    {
        ContainerEventKind? kind = message.Action switch
        {
            "create" => ContainerEventKind.Created,
            "start" => ContainerEventKind.Started,
            "oom" => ContainerEventKind.OutOfMemory,
            "die" => ContainerEventKind.Died,
            "stop" => ContainerEventKind.Stopped,
            "destroy" => ContainerEventKind.Destroyed,
            _ => null,
        };

        if (kind is null || string.IsNullOrEmpty(message.Actor.Id))
        {
            return null;
        }

        var exitCode = int.TryParse(message.Actor.Attributes?.GetValueOrDefault("exitCode"), CultureInfo.InvariantCulture, out var code) ? code : 0;
        return new ContainerEvent(message.Actor.Id, kind.Value, exitCode);
    }

    private static string DigestOf(ImageInspectResponse image, string repository)
    {
        var digests = image.RepoDigests ?? [];
        var match = digests.FirstOrDefault(digest => digest.StartsWith($"{repository}@", StringComparison.Ordinal))
            ?? (digests.Count > 0 ? digests[0] : null);
        return match is null ? image.Id : match[(match.IndexOf('@', StringComparison.Ordinal) + 1)..];
    }

    private static CreateContainerRequest BuildRequest(Workload workload, IReadOnlyList<string> networks)
    {
        var spec = workload.Spec;
        var endpoints = networks.ToDictionary(
            network => network,
            network => new EndpointConfigRequest
            {
                Aliases = network == networks[0] ? [workload.Name.Value] : null,
            });

        return new CreateContainerRequest
        {
            Image = spec.Image.Pinned,
            Env = [.. spec.Environment.Select(variable => $"{variable.Name}={variable.Value}")],
            Labels = BuildLabels(workload, networks.Count > 1 ? networks[^1] : null),
            HostConfig = new HostConfigRequest
            {
                Memory = spec.Resources.MemoryLimitBytes == 0 ? null : (long)spec.Resources.MemoryLimitBytes,
                NanoCpus = spec.Resources.CpuLimitMillicores == 0 ? null : spec.Resources.CpuLimitMillicores * NanoCpusPerMillicore,
                RestartPolicy = new RestartPolicyRequest("unless-stopped"),
                NetworkMode = networks[0],
            },
            NetworkingConfig = new NetworkingConfigRequest(endpoints),
        };
    }

    private static Dictionary<string, string> BuildLabels(Workload workload, string? edgeNetwork)
    {
        var labels = new Dictionary<string, string>
        {
            [ContainerLabels.Managed] = "true",
            [ContainerLabels.AppId] = workload.App.ToString(),
            [ContainerLabels.ProjectId] = workload.Project.ToString(),
            [ContainerLabels.ReleaseId] = workload.Release.ToString(),
        };

        if (edgeNetwork is null)
        {
            return labels;
        }

        var name = workload.Name.Value;
        labels["traefik.enable"] = "true";
        labels["traefik.docker.network"] = edgeNetwork;
        for (var index = 0; index < workload.Spec.Routes.Count; index++)
        {
            var route = workload.Spec.Routes[index];
            labels[$"traefik.http.routers.{name}-{index}.rule"] = $"Host(`{route.Host}`)";
            labels[$"traefik.http.routers.{name}-{index}.service"] = $"{name}-{index}";
            labels[$"traefik.http.services.{name}-{index}.loadbalancer.server.port"] = route.Port.ToString(CultureInfo.InvariantCulture);
        }

        return labels;
    }
}
