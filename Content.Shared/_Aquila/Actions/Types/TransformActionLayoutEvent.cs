using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Actions.Types;

/// <summary>
/// Restores a transformation layout without resolving the discarded body on the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class TransformActionLayoutEvent(NetEntity source, NetEntity target) : EntityEventArgs
{
    public NetEntity Source = source;
    public NetEntity Target = target;
}
