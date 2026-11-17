using Content.Shared._Aquila.Administration;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Administration;

/// <summary>
/// Сохранённый набор параметров админского спавна: снаряжение, свой комплект и импланты.
/// </summary>
[DataDefinition]
public sealed partial class AdminCharacterSpawnTemplate
{
    /// <summary>
    /// Стартовое снаряжение персонажа.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>Комплект, снятый с тела цели.</summary>
    [DataField]
    public AdminCharacterSpawnCustomGear? CustomGear;

    /// <summary>
    /// Подкожные импланты.
    /// </summary>
    [DataField]
    public List<EntProtoId> Implants = new();
}
