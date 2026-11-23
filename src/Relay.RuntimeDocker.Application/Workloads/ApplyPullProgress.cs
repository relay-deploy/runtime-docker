namespace Relay.RuntimeDocker.Application.Workloads;

internal sealed class ApplyPullProgress(IProgress<ApplyProgress> progress) : IProgress<PullProgress>
{
    public void Report(PullProgress value) =>
        progress.Report(new ApplyProgress(ApplyStep.PullingImage, value.CompletedBytes, value.TotalBytes));
}
