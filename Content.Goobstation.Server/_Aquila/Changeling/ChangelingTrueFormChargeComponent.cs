using Robust.Shared.Audio;

namespace Content.Goobstation.Server._Aquila.Changeling;

/// <summary>
///     Stores the settings of the changeling true form transformation.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class ChangelingTrueFormChargeComponent : Component
{
    /// <summary>
    ///     How long the transformation into the true form takes.
    /// </summary>
    [DataField]
    public TimeSpan TransformDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Sound played when the transformation starts.
    /// </summary>
    [DataField]
    public SoundSpecifier TransformSound = new SoundCollectionSpecifier("ChangelingHorrorBones");
}
