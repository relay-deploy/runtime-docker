using System.Threading.Channels;
using Grpc.Core;
using Relay.Chassis.Grpc;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Domain.Workloads;
using Proto = Relay.Runtime.V1;

namespace Relay.RuntimeDocker.Api.Workloads;

internal sealed class RuntimeEndpoint(
    ApplyWorkload applyWorkload,
    RestartWorkload restartWorkload,
    StopWorkload stopWorkload,
    RemoveWorkload removeWorkload,
    GetWorkload getWorkload,
    ListWorkloads listWorkloads) : Proto.RuntimeService.RuntimeServiceBase
{
    public override async Task ApplyWorkload(
        Proto.ApplyWorkloadRequest request,
        IServerStreamWriter<Proto.ApplyWorkloadResponse> responseStream,
        ServerCallContext context)
    {
        var workload = request.Workload.ToDomain().OrThrow();
        var updates = Channel.CreateUnbounded<Proto.ApplyWorkloadResponse>(new UnboundedChannelOptions { SingleReader = true });
        var command = new ApplyWorkloadCommand(workload, new ChannelProgress(updates.Writer));
        var applying = Task.Run(
            async () =>
            {
                try
                {
                    return await applyWorkload.Handle(command, context.CancellationToken);
                }
                finally
                {
                    updates.Writer.Complete();
                }
            },
            CancellationToken.None);

        await foreach (var update in updates.Reader.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(update, context.CancellationToken);
        }

        var status = (await applying).OrThrow();
        await responseStream.WriteAsync(new Proto.ApplyWorkloadResponse { Status = status.ToProto() }, context.CancellationToken);
    }

    public override async Task<Proto.RestartWorkloadResponse> RestartWorkload(
        Proto.RestartWorkloadRequest request,
        ServerCallContext context)
    {
        var app = AppId.Parse(request.AppId).OrThrow();
        var status = (await restartWorkload.Handle(new RestartWorkloadCommand(app), context.CancellationToken)).OrThrow();
        return new Proto.RestartWorkloadResponse { Status = status.ToProto() };
    }

    public override async Task<Proto.StopWorkloadResponse> StopWorkload(
        Proto.StopWorkloadRequest request,
        ServerCallContext context)
    {
        var app = AppId.Parse(request.AppId).OrThrow();
        var status = (await stopWorkload.Handle(new StopWorkloadCommand(app), context.CancellationToken)).OrThrow();
        return new Proto.StopWorkloadResponse { Status = status.ToProto() };
    }

    public override async Task<Proto.RemoveWorkloadResponse> RemoveWorkload(
        Proto.RemoveWorkloadRequest request,
        ServerCallContext context)
    {
        var app = AppId.Parse(request.AppId).OrThrow();
        (await removeWorkload.Handle(new RemoveWorkloadCommand(app), context.CancellationToken)).OrThrow();
        return new Proto.RemoveWorkloadResponse();
    }

    public override async Task<Proto.GetWorkloadResponse> GetWorkload(
        Proto.GetWorkloadRequest request,
        ServerCallContext context)
    {
        var app = AppId.Parse(request.AppId).OrThrow();
        var status = (await getWorkload.Handle(new GetWorkloadQuery(app), context.CancellationToken)).OrThrow();
        return new Proto.GetWorkloadResponse { Status = status.ToProto() };
    }

    public override async Task<Proto.ListWorkloadsResponse> ListWorkloads(
        Proto.ListWorkloadsRequest request,
        ServerCallContext context)
    {
        ProjectId? project = string.IsNullOrEmpty(request.ProjectId) ? null : ProjectId.Parse(request.ProjectId).OrThrow();
        var statuses = (await listWorkloads.Handle(new ListWorkloadsQuery(project), context.CancellationToken)).OrThrow();
        return new Proto.ListWorkloadsResponse { Workloads = { statuses.Select(status => status.ToProto()) } };
    }
}
