using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Relay.Chassis.Hosting;
using Relay.Chassis.Postgres;
using Relay.RuntimeDocker.Application.Lifecycle;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Infrastructure.Docker;
using Relay.RuntimeDocker.Infrastructure.Lifecycle;
using Relay.RuntimeDocker.Infrastructure.Persistence;

namespace Relay.RuntimeDocker.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddRuntimeDockerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DockerOptions>()
            .Bind(configuration.GetSection(DockerOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<DockerApiClient>(client =>
            {
                client.BaseAddress = new Uri("http://docker.invalid");
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
                DockerSocketHandler.Create(provider.GetRequiredService<IOptions<DockerOptions>>().Value.Socket));

        services.AddSingleton<IContainerEngine, DockerContainerEngine>();
        services.AddHealthChecks().AddCheck<DockerHealthCheck>("docker", tags: [ServiceDefaults.ReadinessTag]);

        services.AddRelayPostgres<RuntimeDockerDbContext>(RuntimeDockerDbContext.Schema);
        services.AddRelayOutbox<RuntimeDockerDbContext>();
        services.AddOptions<LifecycleOptions>().BindConfiguration(LifecycleOptions.Section).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IWorkloadEventPublisher, OutboxWorkloadEventPublisher>();
        services.AddHostedService<WorkloadWatcher>();
        return services;
    }
}
