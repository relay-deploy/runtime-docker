using System.Text.Json.Serialization;
using Relay.RuntimeDocker.Infrastructure.Docker.Contract;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ImageInspectResponse))]
[JsonSerializable(typeof(PullLine))]
[JsonSerializable(typeof(EventMessage))]
[JsonSerializable(typeof(List<ContainerSummary>))]
[JsonSerializable(typeof(ContainerInspectResponse))]
[JsonSerializable(typeof(CreateContainerRequest))]
[JsonSerializable(typeof(CreateContainerResponse))]
[JsonSerializable(typeof(CreateNetworkRequest))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
internal sealed partial class DockerJsonContext : JsonSerializerContext;
