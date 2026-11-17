using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration;

/// <summary>Снаряжение, снятое с тела цели для админского спавна.</summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class AdminCharacterSpawnCustomGear : IEquipmentLoadout
{
    /// <summary>Предметы, которые нужно надеть в указанные слоты.</summary>
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <summary>Предметы, которые нужно выдать в свободные руки.</summary>
    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <summary>Предметы, которые нужно положить в экипированные хранилища по слотам.</summary>
    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();

    /// <summary>Создаёт независимую копию снаряжения со всеми списками предметов.</summary>
    public AdminCharacterSpawnCustomGear Copy()
    {
        return new AdminCharacterSpawnCustomGear
        {
            Equipment = Equipment == null
                ? new Dictionary<string, EntProtoId>()
                : new Dictionary<string, EntProtoId>(Equipment),
            Inhand = Inhand == null
                ? new List<EntProtoId>()
                : new List<EntProtoId>(Inhand),
            Storage = Storage == null
                ? new Dictionary<string, List<EntProtoId>>()
                : Storage.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value == null ? new List<EntProtoId>() : new List<EntProtoId>(entry.Value)),
        };
    }
}
