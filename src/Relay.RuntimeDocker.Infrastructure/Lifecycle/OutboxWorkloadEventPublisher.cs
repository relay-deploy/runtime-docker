using Microsoft.Extensions.DependencyInjection;
using Relay.Chassis.Postgres;
using Relay.Chassis.Postgres.Outbox;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Application.Lifecycle;
using Relay.RuntimeDocker.Domain.Lifecycle;
using Relay.RuntimeDocker.Infrastructure.Persistence;

namespace Relay.RuntimeDocker.Infrastructure.Lifecycle;

internal sealed class OutboxWorkloadEventPublisher(IServiceScopeFactory scopes) : IWorkloadEventPublisher
{
    public async Task<Result> Publish(WorkloadEvent workloadEvent, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RuntimeDockerDbContext>();
        scope.ServiceProvider.GetRequiredService<IOutbox>().Publish(workloadEvent.ToProto(), workloadEvent.App.Value);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (PostgresFailures.IsUnavailable(exception))
        {
            return LifecycleErrors.OutboxUnavailable(exception.Message);
        }
    }
}
