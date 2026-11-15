using Robust.Shared.Prototypes;

namespace Content.Server.Guardian;

public sealed partial class GuardianHostComponent
{
    /// <summary>
    /// Действие, открывающее радиальное меню приказов стражу носителя.
    /// </summary>
    [DataField]
    public EntProtoId ActionOrders = "ActionGuardianOrders";

    /// <summary>
    /// Выданное носителю действие меню приказов.
    /// </summary>
    [DataField]
    public EntityUid? ActionOrdersEntity;
}
