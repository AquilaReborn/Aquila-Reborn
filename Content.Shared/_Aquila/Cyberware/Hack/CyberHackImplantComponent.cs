using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._Aquila.Cyberware.Hack;

/// <summary>
/// Имплант взлома: взламывает устройства как емаг, без зарядов, но за счёт здоровья носителя.
/// Сам эффект взлома задаётся компонентом Emag на импланте.
/// </summary>
[RegisterComponent, Access(typeof(CyberHackSystem))]
public sealed partial class CyberHackImplantComponent : Component
{
    /// <summary>
    /// Сколько длится взлом.
    /// </summary>
    [DataField]
    public TimeSpan HackTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// С какого расстояния можно продолжать взлом.
    /// </summary>
    [DataField]
    public float Range = 3.5f;

    /// <summary>
    /// Урон носителю за каждый взлом.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Сколько напряжения добавляет каждый взлом.
    /// </summary>
    [DataField]
    public float Strain = 8f;

    /// <summary>
    /// Звук начала взлома.
    /// </summary>
    [DataField]
    public SoundSpecifier? StartSound;
}
