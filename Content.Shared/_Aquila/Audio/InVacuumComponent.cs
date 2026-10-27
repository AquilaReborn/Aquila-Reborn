using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Audio;

/// <summary>
/// Помечает существо, которое находится в вакууме. Выставляется сервером и нужна клиенту для приглушения звуков.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InVacuumComponent : Component;
