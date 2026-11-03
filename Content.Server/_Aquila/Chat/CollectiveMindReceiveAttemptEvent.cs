using Content.Shared._Starlight.CollectiveMind;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Chat;

/// <summary>
/// Поднимается на слушателе перед тем, как ему придёт сообщение коллективного разума. Отмена скрывает сообщение от него.
/// </summary>
[ByRefEvent]
public record struct CollectiveMindReceiveAttemptEvent(EntityUid Source, ProtoId<CollectiveMindPrototype> Channel, bool Cancelled = false);
