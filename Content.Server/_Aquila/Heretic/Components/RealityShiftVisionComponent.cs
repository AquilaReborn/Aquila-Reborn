using Content.Server._Aquila.Heretic.Systems;
using Robust.Shared.Audio;

namespace Content.Server._Aquila.Heretic.Components;

/// <summary>
/// Полное зрение еретика, которое держится некоторое время после появления сдвига реальности рядом с ним,
/// и кулдаун на повторное срабатывание.
/// </summary>
[RegisterComponent, Access(typeof(HereticRealityShiftVisionSystem)), AutoGenerateComponentPause]
public sealed partial class RealityShiftVisionComponent : Component
{
    /// <summary>
    /// Звук, который играет еретику при включении полного зрения.
    /// </summary>
    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Goobstation/Hallucinations/i_see_you1.ogg");

    /// <summary>
    /// Сколько полное зрение держится с момента срабатывания: новый сдвиг рядом за время зрения его не продлевает.
    /// </summary>
    [DataField]
    public TimeSpan VisionDuration = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Сколько после окончания зрения еретик не может получить его снова.
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Идёт ли сейчас полное зрение.
    /// </summary>
    [ViewVariables]
    public bool Active;

    /// <summary>
    /// Был ли FOV включён у еретика до срабатывания эффекта: это значение возвращается после его окончания.
    /// </summary>
    [ViewVariables]
    public bool OriginalDrawFov = true;

    /// <summary>
    /// Момент, до которого у еретика держится полное зрение.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Момент, до которого новое полное зрение не выдаётся, пока эффект не идёт.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan CooldownUntil;
}
