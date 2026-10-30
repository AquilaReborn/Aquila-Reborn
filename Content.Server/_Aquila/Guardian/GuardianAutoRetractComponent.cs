namespace Content.Server.Aquila.Guardian;

/// <summary>
/// Страж, который автоматически возвращается в носителя, когда того оглушают.
/// </summary>
[RegisterComponent, Access(typeof(GuardianAutoRetractSystem))]
public sealed partial class GuardianAutoRetractComponent : Component
{
    /// <summary>
    /// Возвращать ли стража при оглушении носителя.
    /// </summary>
    [DataField]
    public bool RetractOnStun = true;
}
