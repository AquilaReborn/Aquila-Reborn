using Robust.Shared.Audio;

namespace Content.Goobstation.Server._Aquila.Changeling;

/// <summary>
/// Хранит заряд истинной формы генокрада: повторно её можно принять только после нового поглощения.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingTrueFormChargeComponent : Component
{
    /// <summary>
    /// Сколько существ было поглощено на момент последнего превращения. -1, если превращений ещё не было.
    /// </summary>
    [DataField]
    public int AbsorbedAtLastUse = -1;

    /// <summary>
    /// Сколько длится превращение в истинную форму.
    /// </summary>
    [DataField]
    public TimeSpan TransformDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Звук начала превращения.
    /// </summary>
    [DataField]
    public SoundSpecifier TransformSound = new SoundCollectionSpecifier("ChangelingHorrorBones");
}
