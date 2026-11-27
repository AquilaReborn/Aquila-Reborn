namespace Content.Goobstation.Shared._Aquila.Illusion.Components;

/// <summary>
/// Связывает ограниченную иллюзию с владельцем её общего лимита.
/// </summary>
[RegisterComponent, Access(typeof(SharedIllusionSystem))]
public sealed partial class BudgetedIllusionComponent : Component
{
    /// <summary>
    /// Владелец общего лимита этой копии.
    /// </summary>
    public EntityUid? BudgetOwner;
}
