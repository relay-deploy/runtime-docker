using Google.Protobuf.WellKnownTypes;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Domain.Images;
using Relay.RuntimeDocker.Domain.Workloads;
using Proto = Relay.Runtime.V1;

namespace Relay.RuntimeDocker.Api.Workloads;

internal static class WorkloadMapping
{
    public static Result<Workload> ToDomain(this Proto.Workload workload)
    {
        var app = AppId.Parse(workload.AppId);
        var project = ProjectId.Parse(workload.ProjectId);
        var release = ReleaseId.Parse(workload.ReleaseId);
        var name = WorkloadName.Create(workload.Name);
        var spec = (workload.Spec ?? new Proto.WorkloadSpec()).ToDomain();

        foreach (var parsed in new Result[] { app, project, release, name, spec })
        {
            if (parsed.IsFailure)
            {
                return parsed.Error;
            }
        }

        return new Workload(app.Value, project.Value, release.Value, name.Value, spec.Value);
    }

    public static Proto.ApplyProgress ToProto(this ApplyProgress progress) => new()
    {
        Step = progress.Step switch
        {
            ApplyStep.PullingImage => Proto.ApplyStep.PullingImage,
            ApplyStep.ReplacingWorkload => Proto.ApplyStep.ReplacingWorkload,
            ApplyStep.StartingWorkload => Proto.ApplyStep.StartingWorkload,
            _ => Proto.ApplyStep.Unspecified,
        },
        CompletedBytes = progress.CompletedBytes,
        TotalBytes = progress.TotalBytes,
    };

    public static Proto.WorkloadStatus ToProto(this WorkloadStatus status) => new()
    {
        AppId = status.App.ToString(),
        ReleaseId = status.Release.ToString(),
        Phase = status.Phase switch
        {
            WorkloadPhase.Pending => Proto.WorkloadPhase.Pending,
            WorkloadPhase.Running => Proto.WorkloadPhase.Running,
            WorkloadPhase.Restarting => Proto.WorkloadPhase.Restarting,
            WorkloadPhase.Stopped => Proto.WorkloadPhase.Stopped,
            WorkloadPhase.Crashed => Proto.WorkloadPhase.Crashed,
            _ => Proto.WorkloadPhase.Unspecified,
        },
        ImageDigest = status.ImageDigest,
        RestartCount = (uint)status.RestartCount,
        ObservedAt = Timestamp.FromDateTimeOffset(status.ObservedAt),
    };

    private static Result<WorkloadSpec> ToDomain(this Proto.WorkloadSpec spec)
    {
        var image = ImageReference.Create(spec.Image?.Reference, spec.Image?.Digest);
        if (image.IsFailure)
        {
            return image.Error;
        }

        var environment = new List<EnvironmentVariable>();
        foreach (var variable in spec.Environment)
        {
            var parsed = EnvironmentVariable.Create(variable.Name, variable.Value);
            if (parsed.IsFailure)
            {
                return parsed.Error;
            }

            environment.Add(parsed.Value);
        }

        var routes = new List<WorkloadRoute>();
        foreach (var route in spec.Routes)
        {
            var parsed = WorkloadRoute.Create(route.Host, route.Port);
            if (parsed.IsFailure)
            {
                return parsed.Error;
            }

            routes.Add(parsed.Value);
        }

        var resources = new ResourceLimits(
            spec.Resources?.MemoryLimitBytes ?? 0,
            spec.Resources?.CpuLimitMillicores ?? 0);
        return new WorkloadSpec(image.Value, environment, routes, resources);
    }
}
