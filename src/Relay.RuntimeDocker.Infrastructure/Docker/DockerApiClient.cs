using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Relay.Chassis.Primitives;
using Relay.RuntimeDocker.Application.Workloads;
using Relay.RuntimeDocker.Domain.Docker;
using Relay.RuntimeDocker.Domain.Images;
using Relay.RuntimeDocker.Domain.Workloads;
using Relay.RuntimeDocker.Infrastructure.Docker.Contract;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

internal sealed class DockerApiClient(HttpClient http, IOptions<DockerOptions> options)
{
    public async Task<Result> Ping(CancellationToken cancellationToken)
    {
        var sent = await Send(HttpMethod.Get, "/_ping", null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        return response.IsSuccessStatusCode ? Result.Success() : await Failure(response, cancellationToken);
    }

    public async Task<Result<ImageInspectResponse>> InspectImage(string reference, CancellationToken cancellationToken)
    {
        var sent = await Send(HttpMethod.Get, $"/images/{reference}/json", null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ImageErrors.NotFound(reference);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await Failure(response, cancellationToken);
        }

        var image = await response.Content.ReadFromJsonAsync(DockerJsonContext.Default.ImageInspectResponse, cancellationToken);
        return image!;
    }

    public async Task<Result> PullImage(ImageReference image, IProgress<PullProgress> progress, CancellationToken cancellationToken)
    {
        var path = $"/images/create?fromImage={Uri.EscapeDataString(image.Repository)}&tag={Uri.EscapeDataString(image.PullTag)}";
        var sent = await Send(HttpMethod.Post, path, null, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ImageErrors.NotFound(image.Pinned);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await Failure(response, cancellationToken);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var layers = new Dictionary<string, PullProgressDetail>();
        var reported = new PullProgress(0, 0);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var entry = JsonSerializer.Deserialize(line, DockerJsonContext.Default.PullLine);
            if (entry?.Error is { } failure)
            {
                return PullFailure(image, failure);
            }

            if (entry is { Id: { } layer, ProgressDetail.Total: > 0 })
            {
                layers[layer] = entry.ProgressDetail;
                var current = new PullProgress(
                    (ulong)layers.Values.Sum(detail => detail.Current),
                    (ulong)layers.Values.Sum(detail => detail.Total));
                if (current != reported)
                {
                    reported = current;
                    progress.Report(current);
                }
            }
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<string>>> ListContainerIds(
        IReadOnlyList<string> labelFilters,
        CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string[]> { ["label"] = [.. labelFilters] };
        var encoded = Uri.EscapeDataString(JsonSerializer.Serialize(filters, DockerJsonContext.Default.DictionaryStringStringArray));
        var sent = await Send(HttpMethod.Get, $"/containers/json?all=true&filters={encoded}", null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (!response.IsSuccessStatusCode)
        {
            return await Failure(response, cancellationToken);
        }

        var summaries = await response.Content.ReadFromJsonAsync(DockerJsonContext.Default.ListContainerSummary, cancellationToken);
        return Result.Success<IReadOnlyList<string>>([.. summaries!.Select(summary => summary.Id)]);
    }

    public async Task<Result<ContainerInspectResponse>> InspectContainer(string id, CancellationToken cancellationToken)
    {
        var sent = await Send(HttpMethod.Get, $"/containers/{id}/json", null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return DockerErrors.ContainerNotFound(id);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await Failure(response, cancellationToken);
        }

        var container = await response.Content.ReadFromJsonAsync(DockerJsonContext.Default.ContainerInspectResponse, cancellationToken);
        return container!;
    }

    public async Task<Result<string>> CreateContainer(
        string name,
        CreateContainerRequest request,
        CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(request, DockerJsonContext.Default.CreateContainerRequest);
        var sent = await Send(HttpMethod.Post, $"/containers/create?name={Uri.EscapeDataString(name)}", content, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ImageErrors.NotFound(request.Image);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return WorkloadErrors.NameConflict(name);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await Failure(response, cancellationToken);
        }

        var created = await response.Content.ReadFromJsonAsync(DockerJsonContext.Default.CreateContainerResponse, cancellationToken);
        return created!.Id;
    }

    public Task<Result> StartContainer(string id, CancellationToken cancellationToken) =>
        Command(HttpMethod.Post, $"/containers/{id}/start", id, cancellationToken);

    public Task<Result> RestartContainer(string id, CancellationToken cancellationToken) =>
        Command(HttpMethod.Post, $"/containers/{id}/restart", id, cancellationToken);

    public Task<Result> StopContainer(string id, CancellationToken cancellationToken) =>
        Command(HttpMethod.Post, $"/containers/{id}/stop", id, cancellationToken);

    public Task<Result> RemoveContainer(string id, CancellationToken cancellationToken) =>
        Command(HttpMethod.Delete, $"/containers/{id}?force=true", id, cancellationToken);

    public async Task<Result> EnsureNetwork(string name, CancellationToken cancellationToken)
    {
        var inspected = await Send(HttpMethod.Get, $"/networks/{Uri.EscapeDataString(name)}", null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (inspected.IsFailure)
        {
            return inspected.Error;
        }

        using var inspectResponse = inspected.Value;
        if (inspectResponse.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        if (inspectResponse.StatusCode != HttpStatusCode.NotFound)
        {
            return await Failure(inspectResponse, cancellationToken);
        }

        var labels = new Dictionary<string, string> { [ContainerLabels.Managed] = "true" };
        var content = JsonContent.Create(new CreateNetworkRequest(name, "bridge", labels), DockerJsonContext.Default.CreateNetworkRequest);
        var created = await Send(HttpMethod.Post, "/networks/create", content, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (created.IsFailure)
        {
            return created.Error;
        }

        using var createResponse = created.Value;
        return createResponse.IsSuccessStatusCode || createResponse.StatusCode == HttpStatusCode.Conflict
            ? Result.Success()
            : await Failure(createResponse, cancellationToken);
    }

    private static Error PullFailure(ImageReference image, string failure) =>
        failure.Contains("not found", StringComparison.OrdinalIgnoreCase)
        || failure.Contains("manifest unknown", StringComparison.OrdinalIgnoreCase)
        || failure.Contains("pull access denied", StringComparison.OrdinalIgnoreCase)
            ? ImageErrors.NotFound(image.Pinned)
            : ImageErrors.PullFailed(image.Pinned, failure);

    private static async Task<Error> Failure(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return DockerErrors.RequestFailed((int)response.StatusCode, body.Trim());
    }

    private async Task<Result> Command(HttpMethod method, string path, string id, CancellationToken cancellationToken)
    {
        var sent = await Send(method, path, null, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        using var response = sent.Value;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return DockerErrors.ContainerNotFound(id);
        }

        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified
            ? Result.Success()
            : await Failure(response, cancellationToken);
    }

    private async Task<Result<HttpResponseMessage>> Send(
        HttpMethod method,
        string path,
        HttpContent? content,
        HttpCompletionOption completion,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"/{options.Value.ApiVersion}{path}") { Content = content };
        try
        {
            return await http.SendAsync(request, completion, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return DockerErrors.Unavailable(exception.Message);
        }
    }
}
