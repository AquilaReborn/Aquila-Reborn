using Robust.Shared.Map;
using Robust.Shared.Containers;

namespace Content.Server.Aquila.TimeRewind;

[RegisterComponent]
public sealed partial class PendingRewindComponent : Component
{
    public TimeSpan EndTime;
    public TimeSpan EffectDuration;
    public List<RewindSnapshot> Snapshots = new();
    public List<ItemSnapshot> Items = new();
}

public struct RewindSnapshot
{
    public EntityUid Entity;
    public EntityCoordinates Coordinates;
    public Angle Rotation;
    public EntityUid? BuckledTo;
}

public enum ItemContainerKind
{
    Inventory,
    Hand,
    Storage,
}

public struct ItemSnapshot
{
    public EntityUid Item;
    public ItemContainerKind Kind;

    public EntityUid Owner;

    public string? SlotOrHand;

    public BaseContainer? StorageContainer;
}
