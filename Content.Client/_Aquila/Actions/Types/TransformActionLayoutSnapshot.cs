namespace Content.Client._Aquila.Actions.Types;

/// <summary>
/// Keeps one body's slot order after its entities have been deleted.
/// </summary>
public sealed class TransformActionLayoutSnapshot
{
    public EntityUid Owner;
    public NetEntity Entity;
    public bool UserModified;
    public List<TransformActionLayoutSlot?> Slots = new();
}
