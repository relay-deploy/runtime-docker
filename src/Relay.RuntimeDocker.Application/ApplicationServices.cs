using Microsoft.Extensions.DependencyInjection;
using Relay.RuntimeDocker.Application.Workloads;

namespace Relay.RuntimeDocker.Application;

public static class ApplicationServices
{
    public static IServiceCollection AddRuntimeDockerApplication(this IServiceCollection services) => services
        .AddSingleton<ApplyWorkload>()
        .AddSingleton<RestartWorkload>()
        .AddSingleton<StopWorkload>()
        .AddSingleton<RemoveWorkload>()
        .AddSingleton<GetWorkload>()
        .AddSingleton<ListWorkloads>();
}
