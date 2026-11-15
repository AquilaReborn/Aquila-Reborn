using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.WOU.RemoteHoloParasite;

/// <summary>
/// Маркер на самом паразите. Указывает, что сущность — remote holo-parasite.
/// Хранит, какой прототип действия использовать для свопа и возврата,
/// а также кэш уже привязанного хоста, чтобы не перепривязывать каждый тик.
/// </summary>
[RegisterComponent]
public sealed partial class RemoteHoloParasiteComponent : Component
{
    /// <summary>Прототип действия "вселиться". Выдаётся хосту.</summary>
    [DataField]
    public EntProtoId SwapAction = "ActionSoulSwap";

    /// <summary>Прототип действия "вернуться". Выдаётся обеим сторонам после свопа.</summary>
    [DataField]
    public EntProtoId ReturnAction = "ActionSoulSwapReturn";

    /// <summary>Хост, к которому мы уже привязались (кэш).</summary>
    [ViewVariables]
    public EntityUid? LinkedHost;

    /// <summary>Флаг "привязка выполнена", чтобы Update не дёргал EnsureComp каждый тик.</summary>
    [ViewVariables]
    public bool HostLinked;
}