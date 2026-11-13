using Content.Shared.Eui;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration;

/// <summary>
/// Запрос на создание персонажа с выбранными параметрами.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminCharacterSpawnRequest : EuiMessageBase
{
    /// <summary>
    /// Номер слота сохранённого персонажа.
    /// </summary>
    public int? Slot;

    /// <summary>
    /// Использовать слот цели вместо слота администратора.
    /// </summary>
    public bool UseTargetSlot;

    /// <summary>
    /// Идентификатор пресета внешности.
    /// </summary>
    public ProtoId<HumanoidProfilePrototype>? Preset;

    /// <summary>
    /// Идентификатор стартового снаряжения.
    /// </summary>
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// Имя, заданное администратором.
    /// </summary>
    public string? Name;

    /// <summary>
    /// Поместить персонажа в закрытый ящик или шкаф рядом с призраком администратора.
    /// </summary>
    public bool SpawnInsideStorage;
}
