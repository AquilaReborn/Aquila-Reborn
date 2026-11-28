using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Audio;

namespace Content.Server._Aquila.Heretic.Moon.Components;

/// <summary>
/// Обращённый вознёсшимся еретиком пути Луны: постоянный союзник в безумии.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAscensionSystem))]
public sealed partial class MoonLunaticComponent : Component
{
    /// <summary>
    /// Сколько длится снятие лунатизма библией.
    /// </summary>
    [DataField]
    public TimeSpan CleanseDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Звук, с которым при снятии лунатизма рассыпается амулет.
    /// </summary>
    [DataField]
    public SoundSpecifier CleanseSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
}
