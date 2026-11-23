using Relay.Chassis.Primitives;

namespace Relay.RuntimeDocker.Domain.Images;

public sealed record ImageReference
{
    private ImageReference(string reference, string repository, string tag, string? digest)
    {
        Reference = reference;
        Repository = repository;
        Tag = tag;
        Digest = digest;
    }

    public string Reference { get; }

    public string Repository { get; }

    public string Tag { get; }

    public string? Digest { get; }

    public string Pinned => Digest is null ? Reference : $"{Repository}@{Digest}";

    public string PullTag => Digest ?? Tag;

    public static Result<ImageReference> Create(string? reference, string? digest)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Any(char.IsWhiteSpace))
        {
            return ImageErrors.Invalid(reference);
        }

        var name = reference;
        var at = name.IndexOf('@', StringComparison.Ordinal);
        var embeddedDigest = at >= 0 ? name[(at + 1)..] : null;
        if (at >= 0)
        {
            name = name[..at];
        }

        var colon = name.LastIndexOf(':');
        var hasTag = colon > name.LastIndexOf('/');
        var repository = hasTag ? name[..colon] : name;
        var tag = hasTag ? name[(colon + 1)..] : "latest";
        var resolvedDigest = string.IsNullOrWhiteSpace(digest) ? embeddedDigest : digest;
        return new ImageReference(reference, repository, tag, resolvedDigest);
    }

    public override string ToString() => Pinned;
}
