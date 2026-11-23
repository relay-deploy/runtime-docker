namespace Relay.RuntimeDocker.Infrastructure.Docker.Contract;

internal sealed record CreateNetworkRequest(string Name, string Driver, IReadOnlyDictionary<string, string> Labels);
