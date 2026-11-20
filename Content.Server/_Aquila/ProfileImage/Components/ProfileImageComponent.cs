using Content.Server._Aquila.ProfileImage.Systems;
using Content.Server.Station.Systems;

namespace Content.Server._Aquila.ProfileImage.Components;

/// <summary>
/// Stores the PNG image that is shown to other players when the character is examined.
/// </summary>
[RegisterComponent]
[Access(typeof(StationSpawningSystem), typeof(ProfileImageSystem), Other = AccessPermissions.None)]
public sealed partial class ProfileImageComponent : Component
{
    /// <summary>
    /// Encoded PNG data of the profile image.
    /// </summary>
    public byte[]? Png;
}
