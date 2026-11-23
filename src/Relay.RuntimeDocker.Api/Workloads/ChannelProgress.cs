using System.Threading.Channels;
using Relay.RuntimeDocker.Application.Workloads;
using Proto = Relay.Runtime.V1;

namespace Relay.RuntimeDocker.Api.Workloads;

internal sealed class ChannelProgress(ChannelWriter<Proto.ApplyWorkloadResponse> writer) : IProgress<ApplyProgress>
{
    public void Report(ApplyProgress value) =>
        writer.TryWrite(new Proto.ApplyWorkloadResponse { Progress = value.ToProto() });
}
