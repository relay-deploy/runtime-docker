using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Images;

public static class ImageErrors
{
    public static Error NotFound(string reference) =>
        Error.NotFound("image.not_found", $"Image {reference} was not found.");

    public static Error Invalid(string? reference) =>
        Error.Validation("image.invalid_reference", $"Image reference '{reference}' is not valid.");

    public static Error PullFailed(string reference, string detail) =>
        Error.Unavailable("image.pull_failed", $"Pulling {reference} failed: {detail}");
}
