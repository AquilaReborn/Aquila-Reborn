using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.Guardian;

[Serializable, NetSerializable]
public sealed class GuardianSetOrderEvent : EntityEventArgs
{
    public readonly GuardianOrderType Order;

    public GuardianSetOrderEvent(GuardianOrderType order)
    {
        Order = order;
    }
}
