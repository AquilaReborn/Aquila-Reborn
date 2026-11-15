using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.WOU.RemoteHoloParasite;

/// <summary>
/// Маркер на хосте. Автоматически выдаёт ActionSoulSwap при старте.
/// Может быть повешен вручную (в прототипе инъектора) или автоматически — системой паразита.
/// </summary>
[RegisterComponent]
public sealed partial class RemoteHoloParasiteHostComponent : Component
{
    /// <summary>Прототип действия "вселиться", которое вешается на хоста.</summary>
    [DataField]
    public EntProtoId SwapAction = "ActionSoulSwap";

    /// <summary>Сущность выданного действия (нужна, чтобы корректно удалить его при shutdown).</summary>
    [ViewVariables]
    public EntityUid? SwapActionEntity;
}