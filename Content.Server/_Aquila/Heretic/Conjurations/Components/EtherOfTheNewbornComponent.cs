using Robust.Shared.Audio;

namespace Content.Server._Aquila.Heretic.Conjurations.Components;

/// <summary>
/// Одноразовый Эфир Новорождённого: при использовании в руке исцеляет, снимает импланты и усыпляет.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class EtherOfTheNewbornComponent : Component
{
    /// <summary>
    /// Длительность принудительного сна после применения.
    /// </summary>
    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Звук, который проигрывается при применении.
    /// </summary>
    [DataField]
    public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}
