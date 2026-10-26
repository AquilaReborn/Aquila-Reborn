namespace Content.Goobstation.Server._Aquila.Changeling;

/// <summary>
/// Сущность, через которую генокрад наблюдает за своим телом, пока им управляет жертва.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingPassengerComponent : Component
{
    /// <summary>
    /// Тело генокрада, за которым он наблюдает.
    /// </summary>
    [ViewVariables]
    public EntityUid Host;
}
