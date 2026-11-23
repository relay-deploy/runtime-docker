using Relay.Chassis.Hosting;
using Relay.RuntimeDocker.Api.Workloads;
using Relay.RuntimeDocker.Application;
using Relay.RuntimeDocker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddRelayServiceDefaults();
builder.Services.AddRuntimeDockerApplication();
builder.Services.AddRuntimeDockerInfrastructure(builder.Configuration);

var app = builder.Build();
app.MapRelayServiceDefaults();
app.MapGrpcService<RuntimeEndpoint>();

await app.RunAsync();
