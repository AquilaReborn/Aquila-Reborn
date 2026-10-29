using Content.Shared.Aquila.Guardian;

namespace Content.Server.Guardian;

public sealed partial class GuardianComponent
{
    /// <summary>
    /// Флаг для того чтобы сраные паразиты не крашили игру пока носитель сидит на транспорте.
    /// </summary>
    [DataField]
    public bool IsBeginRepositioned;

    /// <summary>
    /// Текущий приказ стражу от носителя, по нему работает HTN-поведение стража.
    /// </summary>
    [DataField]
    public GuardianOrderType CurrentOrder = GuardianOrderType.Follow;
}
