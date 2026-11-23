using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Relay.Chassis.Postgres;

namespace Relay.RuntimeDocker.Infrastructure.Persistence;

public sealed class RuntimeDockerDbContextFactory : IDesignTimeDbContextFactory<RuntimeDockerDbContext>
{
    public RuntimeDockerDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<RuntimeDockerDbContext>();
        builder.UseRelayPostgres("Host=localhost", RuntimeDockerDbContext.Schema);
        return new RuntimeDockerDbContext(builder.Options);
    }
}
