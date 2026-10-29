using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Components;

[Serializable, NetSerializable]
public enum TurnstileStates : byte
{
    Idle,
    Deny,
    Spin,
}
