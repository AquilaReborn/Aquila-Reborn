using Content.Shared._Aquila.Heretic.Moon.Systems;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Пышное облачение пути Луны. Пока оно надето, часть полученного урона уходит в мозг носителя.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonRegaliaSystem))]
public sealed partial class MoonRegaliaComponent : Component
{
    /// <summary>
    /// Во сколько раз урон, переведённый в мозговой, превышает полученный.
    /// </summary>
    [DataField]
    public float BrainDamageMultiplier = 1.15f;

    /// <summary>
    /// Носитель облачения, пока оно надето. Нужен для снятия эффектов при удалении облачения.
    /// </summary>
    [DataField]
    public EntityUid? Wearer;
}
