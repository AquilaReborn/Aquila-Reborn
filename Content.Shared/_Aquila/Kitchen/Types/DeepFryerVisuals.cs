using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Kitchen;

[Serializable, NetSerializable]
public enum DeepFryerVisuals : byte
{
    /// <summary>
    /// bool: машина сейчас жарит.
    /// </summary>
    IsFrying
}

[Serializable, NetSerializable]
public enum DeepFryerVisualLayers : byte
{
    Base
}
