using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.ProfileImage.Events;

/// <summary>
/// Sent by the server to a single client with the profile image of the requested entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class ProfileImageResponseEvent : EntityEventArgs
{
    /// <summary>
    /// Entity the profile image belongs to.
    /// </summary>
    public readonly NetEntity Target;

    /// <summary>
    /// Encoded PNG data of the profile image, or <see langword="null"/> when the entity has no image.
    /// </summary>
    public readonly byte[]? Png;

    /// <summary>
    /// Creates a new response with the profile image of the given entity.
    /// </summary>
    /// <param name="target">Entity the profile image belongs to.</param>
    /// <param name="png">Encoded PNG data of the profile image, or <see langword="null"/> when there is none.</param>
    public ProfileImageResponseEvent(NetEntity target, byte[]? png)
    {
        Target = target;
        Png = png;
    }
}
