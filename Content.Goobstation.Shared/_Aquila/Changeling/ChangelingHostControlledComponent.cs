using Content.Shared._Starlight.CollectiveMind;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Changeling;

/// <summary>
/// Тело генокрада, которым сейчас управляет поглощённая жертва. Генокрад в это время наблюдает изнутри.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingHostControlledComponent : Component
{
    /// <summary>
    /// Сущность, через которую генокрад наблюдает за телом.
    /// </summary>
    [ViewVariables]
    public EntityUid? Passenger;

    /// <summary>
    /// Разум жертвы, которой передано управление.
    /// </summary>
    [ViewVariables]
    public EntityUid? VictimMind;

    /// <summary>
    /// Способности генокрада, перенесённые с тела к наблюдающему генокраду, чтобы жертва их не видела.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> HiddenActions = new();

    /// <summary>
    /// Каналы коллективного разума тела до передачи управления.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<CollectiveMindPrototype>>? HiddenChannels;
}
