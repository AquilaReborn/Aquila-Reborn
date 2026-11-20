using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.ProfileImage.Events;

/// <summary>
/// Sent by the client to request the profile image of an examined entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestProfileImageEvent : EntityEventArgs
{
    /// <summary>
    /// Entity whose profile image is requested.
    /// </summary>
    public readonly NetEntity Target;

    /// <summary>
    /// Creates a new request for the profile image of the given entity.
    /// </summary>
    /// <param name="target">Entity whose profile image is requested.</param>
    public RequestProfileImageEvent(NetEntity target)
    {
        Target = target;
    }
}
