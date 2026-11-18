using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.MartialArts.Components;

/// <summary>
/// Метка, что корпоративное дзюдо закреплено за сотрудником службы безопасности системой, а не выдано предметом.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SystemGrantedJudoComponent : Component
{
}
