using System.Numerics;
using Content.Shared._Aquila.Audio;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Alert;
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

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private bool _enabled;
    private float _maxRayLength;
    private bool _listenerInVacuum;
    private float _updateTimer;

    /// <summary>
    /// Находится ли слушатель сейчас в вакууме.
    /// </summary>
    public bool ListenerInVacuum => _listenerInVacuum;

    public override void Initialize()
    {
        base.Initialize();

        _audio.GetOcclusionOverride += GetOcclusion;

        Subs.CVar(_cfg, AquilaCVars.AudioEnvironment, value => _enabled = value, true);
        Subs.CVar(_cfg, CVars.AudioRaycastLength, value => _maxRayLength = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _audio.GetOcclusionOverride -= GetOcclusion;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_proto.TryIndex(SettingsId, out var settings))
        {
            _listenerInVacuum = false;
            return;
        }

        _updateTimer -= frameTime;
        if (_updateTimer > 0f)
            return;

        _updateTimer = settings.UpdateInterval;
        _listenerInVacuum = IsListenerInVacuum(settings);
    }

    private float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt)
    {
        var occlusion = 0f;

        if (distance > 0.1f)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta / distance, _audio.OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, ignoredEnt);
        }

        if (!_enabled || !_proto.TryIndex(SettingsId, out var settings))
            return occlusion;

        if (_listenerInVacuum)
            return occlusion + settings.ListenerOcclusion;

        if (distance > 0.1f && IsSpace(new MapCoordinates(listener.Position + delta, listener.MapId)))
            return occlusion + settings.SourceOcclusion;

        return occlusion;
    }

    private bool IsListenerInVacuum(VacuumAudioPrototype settings)
    {
        if (_player.LocalEntity is not { } player)
            return false;

        if (TryComp<AlertsComponent>(player, out var alerts))
        {
            foreach (var state in alerts.Alerts.Values)
            {
                if (state.Type == settings.PressureAlert && state.Severity >= settings.VacuumSeverity)
                    return true;
            }
        }

        return IsSpace(_transform.GetMapCoordinates(player));
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
