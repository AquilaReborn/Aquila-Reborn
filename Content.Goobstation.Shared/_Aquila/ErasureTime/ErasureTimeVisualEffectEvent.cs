using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Aquila.ErasureTime;

[Serializable, NetSerializable]
public sealed class ErasureTimeVisualEffectEvent : EntityEventArgs
{
    public NetEntity Source;
    public float Radius;
    public float WaveDuration;

    public ErasureTimeVisualEffectEvent(NetEntity source, float radius, float waveDuration)
    {
        Source = source;
        Radius = radius;
        WaveDuration = waveDuration;
    }
}
