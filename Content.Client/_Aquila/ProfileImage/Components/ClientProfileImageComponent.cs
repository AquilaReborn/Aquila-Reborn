using Content.Client.Examine;
using Content.Client._Aquila.ProfileImage.Systems;

namespace Content.Client._Aquila.ProfileImage.Components;

/// <summary>
/// Stores the profile image that the server has sent for an entity, including the negative answer.
/// </summary>
[RegisterComponent]
[Access(typeof(ProfileImageClientSystem), typeof(ExamineSystem), Other = AccessPermissions.None)]
public sealed partial class ClientProfileImageComponent : Component
{
    /// <summary>
    /// Encoded PNG data of the profile image, or <see langword="null"/> when the entity has no image.
    /// </summary>
    public byte[]? Png;
}
