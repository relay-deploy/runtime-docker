using Google.Protobuf;
using Relay.RuntimeDocker.Domain.Lifecycle;
using Proto = Relay.Runtime.V1;

namespace Relay.RuntimeDocker.Infrastructure.Lifecycle;

internal static class WorkloadEventMapping
{
    public static IMessage ToProto(this WorkloadEvent workloadEvent) => workloadEvent switch
    {
        WorkloadCrashed crashed => new Proto.WorkloadCrashed
        {
            AppId = crashed.App.ToString(),
            ProjectId = crashed.Project.ToString(),
            ReleaseId = crashed.Release.ToString(),
            ExitCode = crashed.Exit.Code,
            OomKilled = crashed.Exit.OutOfMemory,
            RestartCount = (uint)crashed.RestartCount,
        },
        WorkloadRecovered recovered => new Proto.WorkloadRecovered
        {
            AppId = recovered.App.ToString(),
            ProjectId = recovered.Project.ToString(),
            ReleaseId = recovered.Release.ToString(),
            RestartCount = (uint)recovered.RestartCount,
        },
        WorkloadStopped stopped => new Proto.WorkloadStopped
        {
            AppId = stopped.App.ToString(),
            ProjectId = stopped.Project.ToString(),
            ReleaseId = stopped.Release.ToString(),
            ExitCode = stopped.ExitCode,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(workloadEvent), workloadEvent.GetType().Name, "Unknown workload event."),
    };
}
