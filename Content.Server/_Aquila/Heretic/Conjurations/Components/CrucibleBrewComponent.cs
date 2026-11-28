using Content.Server._Aquila.Heretic.Conjurations.Systems;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Conjurations.Components;

/// <summary>
/// Зелье из Зубастого тигля: при использовании в руке выдаёт статус-эффект и исчезает.
/// </summary>
[RegisterComponent, Access(typeof(CrucibleBrewSystem))]
public sealed partial class CrucibleBrewComponent : Component
{
    /// <summary>
    /// Статус-эффект, который получает выпивший зелье.
    /// </summary>
    [DataField]
    public EntProtoId StatusEffect;

    /// <summary>
    /// Длительность статус-эффекта.
    /// </summary>
    [DataField]
    public TimeSpan Duration;

    /// <summary>
    /// Звук, который проигрывается при употреблении.
    /// </summary>
    [DataField]
    public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");

    /// <summary>
    /// Требовать ли службу Мансусу: не-еретик и не-гуль получает отказ и урон.
    /// </summary>
    [DataField]
    public bool RequireHeretic = true;

    /// <summary>
    /// Урон, который получает не-еретик за попытку выпить зелье.
    /// </summary>
    [DataField]
    public DamageSpecifier RejectDamage = new()
    {
        DamageDict =
        {
            { "Toxin", 2 },
        },
    };

    /// <summary>
    /// Статус, при наличии которого зелье нельзя выпить.
    /// </summary>
    [DataField]
    public EntProtoId? BlockedByStatus;
}
