using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.Guardian;

[Serializable, NetSerializable]
public enum GuardianOrderType : byte
{
    Follow,
    AttackTarget,
    AttackAll
}
