using Content.Shared.Aquila.Guardian;

namespace Content.Server.Guardian;

public sealed partial class GuardianComponent
{
    /// <summary>
    /// Текущий приказ стражу от носителя, по нему работает HTN-поведение стража.
    /// </summary>
    [DataField]
    public GuardianOrderType CurrentOrder = GuardianOrderType.Follow;
}
