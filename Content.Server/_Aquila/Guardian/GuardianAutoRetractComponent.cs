namespace Content.Server.Aquila.Guardian;

/// <summary>
/// Marks a guardian that should be automatically retracted into its host
/// when the host gets stunned.
/// </summary>
[RegisterComponent]
public sealed partial class GuardianAutoRetractComponent : Component
{
    [DataField]
    public bool RetractOnStun = true;
}
