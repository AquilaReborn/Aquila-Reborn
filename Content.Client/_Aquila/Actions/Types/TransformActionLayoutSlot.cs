using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Actions.Types;

/// <summary>
/// Identifies a cached action independently of its discarded client entity.
/// </summary>
public sealed class TransformActionLayoutSlot
{
    public NetEntity Action;
    public EntityUid Entity;
    public EntProtoId? Prototype;
    public NetEntity? Provider;
    public bool Personal;
    public TimeSpan RetainUntil;
}
