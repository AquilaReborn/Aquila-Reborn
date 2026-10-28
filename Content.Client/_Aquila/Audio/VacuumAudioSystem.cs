using System.Numerics;
using Content.Shared._Aquila.Audio;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Maps;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Audio;

public sealed class VacuumAudioSystem : EntitySystem
{
    private static readonly ProtoId<VacuumAudioPrototype> SettingsId = "Default";
    private const float MinOcclusionDistance = 0.1f;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        _audio.GetOcclusionOverride += GetOcclusion;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _audio.GetOcclusionOverride -= GetOcclusion;
    }

    /// <summary>
    /// Проверяет, находится ли сущность в вакууме или в открытом космосе.
    /// </summary>
    public bool IsInVacuum(EntityUid uid)
    {
        return HasComp<InVacuumComponent>(uid) || IsSpace(_transform.GetMapCoordinates(uid));
    }

    private float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt)
    {
        var occlusion = 0f;

        if (distance > MinOcclusionDistance)
        {
            var rayLength = MathF.Min(distance, _cfg.GetCVar(CVars.AudioRaycastLength));
            var ray = new CollisionRay(listener.Position, delta / distance, _audio.OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, ignoredEnt);
        }

        if (!_cfg.GetCVar(AquilaCVars.AudioEnvironment) || !_proto.TryIndex(SettingsId, out var settings))
            return occlusion;

        if (_player.LocalEntity is { } player && IsInVacuum(player))
            return occlusion + settings.ListenerOcclusion;

        if (IsSourceInVacuum(ignoredEnt) || distance > MinOcclusionDistance && IsSpace(new MapCoordinates(listener.Position + delta, listener.MapId)))
            return occlusion + settings.SourceOcclusion;

        return occlusion;
    }

    private bool IsSourceInVacuum(EntityUid? source)
    {
        while (source is { } uid && TryComp(uid, out TransformComponent? xform))
        {
            if (HasComp<InVacuumComponent>(uid))
                return true;

            if (xform.GridUid == uid || xform.MapUid == uid)
                return false;

            source = xform.ParentUid;
        }

        return false;
    }

    private bool IsSpace(MapCoordinates coordinates)
    {
        if (coordinates.MapId == MapId.Nullspace)
            return false;

        if (!_mapManager.TryFindGridAt(coordinates, out var gridUid, out var grid))
            return true;

        var indices = _map.TileIndicesFor(gridUid, grid, coordinates);
        return !_map.TryGetTileRef(gridUid, grid, indices, out var tileRef) || _turf.IsSpace(tileRef);
    }
}
