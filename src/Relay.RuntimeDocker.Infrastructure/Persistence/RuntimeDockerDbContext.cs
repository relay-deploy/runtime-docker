using Microsoft.EntityFrameworkCore;
using Relay.Chassis.Postgres;
using Relay.Chassis.Postgres.Outbox;

namespace Relay.RuntimeDocker.Infrastructure.Persistence;

public sealed class RuntimeDockerDbContext(DbContextOptions<RuntimeDockerDbContext> options) : DbContext(options), IOutboxContext
{
    public const string Schema = "runtime_docker";

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.MapRelayOutbox();
    }
}
