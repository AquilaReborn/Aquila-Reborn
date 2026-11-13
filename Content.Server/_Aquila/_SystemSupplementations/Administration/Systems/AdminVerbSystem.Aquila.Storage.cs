using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly EntityLookupSystem _adminSpawnLookup = default!;
    [Dependency] private readonly SharedEntityStorageSystem _adminSpawnStorage = default!;
    [Dependency] private readonly SharedContainerSystem _adminSpawnContainers = default!;

    private static readonly EntProtoId AdminSpawnCrate = "CrateGeneric";
    private static readonly EntProtoId AdminSpawnCloset = "ClosetBase";
    private const float AdminSpawnStorageRange = 0.5f;

    private bool TryGetAdminSpawnStorage(ICommonSession player, [NotNullWhen(true)] out EntityUid? storage)
    {
        storage = null;
        if (!_adminManager.HasAdminFlag(player, AdminFlags.Spawn)
            || player.AttachedEntity is not { } ghost
            || TerminatingOrDeleted(ghost)
            || !HasComp<GhostComponent>(ghost))
        {
            return false;
        }

        var coordinates = Transform(ghost).Coordinates;
        var nearestDistance = float.PositiveInfinity;
        foreach (var (uid, component) in _adminSpawnLookup.GetEntitiesInRange<EntityStorageComponent>(coordinates, AdminSpawnStorageRange))
        {
            if (!CanUseAdminSpawnStorage(uid, component)
                || !coordinates.TryDistance(EntityManager, Transform(uid).Coordinates, out var distance)
                || distance > AdminSpawnStorageRange
                || distance >= nearestDistance
                || MetaData(uid).EntityPrototype is not { } prototype)
            {
                continue;
            }

            if (!_prototypeManager.EnumerateAllParents<EntityPrototype>(prototype.ID, includeSelf: true)
                .Any(parent => parent.id == AdminSpawnCrate.Id || parent.id == AdminSpawnCloset.Id))
            {
                continue;
            }

            storage = uid;
            nearestDistance = distance;
        }

        return storage.HasValue;
    }

    private bool TryPlaceAdminSpawnInsideStorage(EntityUid mob, EntityUid? storage)
    {
        return storage is { } uid
            && TryComp<EntityStorageComponent>(uid, out var component)
            && CanUseAdminSpawnStorage(uid, component)
            && _adminSpawnStorage.CanInsert(mob, uid, component)
            && !component.Open
            && _adminSpawnStorage.Insert(mob, uid, component)
            && component.Contents.Contains(mob);
    }

    private bool CanUseAdminSpawnStorage(EntityUid uid, EntityStorageComponent component)
    {
        return !TerminatingOrDeleted(uid)
            && !component.Open
            && component.Contents is { } contents
            && contents.ContainedEntities.Count < component.Capacity
            && !_adminSpawnContainers.IsEntityInContainer(uid);
    }
}
