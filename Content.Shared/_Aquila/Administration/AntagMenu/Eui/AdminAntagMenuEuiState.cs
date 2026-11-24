using Content.Shared._Aquila.Administration.AntagMenu.DataDefinitions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Eui;

/// <summary>
/// Состояние меню антагонистов: разумы игроков, их роли и цели.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuEuiState : EuiStateBase
{
    /// <summary>
    /// Разумы игроков и текущих антагонистов.
    /// </summary>
    public List<AdminAntagMenuMindData> Minds = new();

    /// <summary>
    /// Прототипы антагонистов, для которых существует прототип роли разума.
    /// </summary>
    public List<string> GrantableAntags = new();
}
