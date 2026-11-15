using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.WOU.RemoteHoloParasite;

/// <summary>
/// Висит на обеих сторонах (хост и паразит) во время активного свопа.
/// Хранит партнёра для возврата и entity-id выданного return-действия.
/// </summary>
[RegisterComponent]
public sealed partial class SoulSwappedComponent : Component
{
    /// <summary>Вторая сторона свопа.</summary>
    [ViewVariables]
    public EntityUid Partner;

    /// <summary>Прототип return-действия (копируется из RemoteHoloParasiteComponent).</summary>
    [ViewVariables]
    public EntProtoId ReturnAction = "ActionSoulSwapReturn";

    /// <summary>Сущность выданного return-действия на этом теле (если разум есть).</summary>
    [ViewVariables]
    public EntityUid? ReturnActionEntity;
}