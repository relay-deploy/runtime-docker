namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record NetworkingConfigRequest(IReadOnlyDictionary<string, EndpointConfigRequest> EndpointsConfig);
