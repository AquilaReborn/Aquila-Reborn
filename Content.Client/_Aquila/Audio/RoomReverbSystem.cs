using System.Numerics;
using Content.Shared._Aquila.Audio;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Maps;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Audio;

public sealed class RoomReverbSystem : EntitySystem
{
    private static readonly ProtoId<RoomReverbPrototype> SettingsId = "Default";
    private static readonly Vector2i[] Directions = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly VacuumAudioSystem _vacuum = default!;

    private EntityQuery<OccluderComponent> _occluderQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    private readonly Queue<Vector2i> _queue = new();
    private readonly HashSet<Vector2i> _visited = new();

    private Entity<AudioEffectComponent>? _effect;
    private Entity<AudioAuxiliaryComponent>? _auxiliary;
    private ReverbProperties _current;
    private ReverbProperties _target;
    private float _analyzeTimer;
    private float _applyTimer;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _occluderQuery = GetEntityQuery<OccluderComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        Subs.CVar(_cfg, AquilaCVars.AudioEnvironment, OnReverbToggled, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_effect is { } effect)
            QueueDel(effect);

        if (_auxiliary is { } auxiliary)
            QueueDel(auxiliary);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || !_proto.TryIndex(SettingsId, out var settings))
            return;

        var dry = GetDry(settings);
        EnsureEffect(dry, out var effect, out var auxiliary);

        _analyzeTimer -= frameTime;
        if (_analyzeTimer <= 0f)
        {
            _analyzeTimer = settings.AnalyzeInterval;
            _target = AnalyzeEnvironment(settings, dry);
        }

        _applyTimer -= frameTime;
        if (_applyTimer > 0f)
            return;

        _applyTimer = settings.ApplyInterval;
        _current = Lerp(_current, _target, 1f - MathF.Exp(-settings.SmoothRate * settings.ApplyInterval));

        _audio.SetEffectPreset(effect, effect.Comp, _current);
        _audio.SetEffect(auxiliary, auxiliary.Comp, effect);
        AttachSources(auxiliary, settings.MaxSourceDistance);
    }

    private void OnReverbToggled(bool enabled)
    {
        _enabled = enabled;

        if (!enabled)
            DetachSources();
    }

    private void EnsureEffect(ReverbProperties dry, out Entity<AudioEffectComponent> effect, out Entity<AudioAuxiliaryComponent> auxiliary)
    {
        if (_effect is { } existingEffect
            && _auxiliary is { } existingAuxiliary
            && !TerminatingOrDeleted(existingEffect)
            && !TerminatingOrDeleted(existingAuxiliary))
        {
            effect = existingEffect;
            auxiliary = existingAuxiliary;
            return;
        }

        var createdEffect = _audio.CreateEffect();
        var createdAuxiliary = _audio.CreateAuxiliary();

        effect = (createdEffect.Entity, createdEffect.Component);
        auxiliary = (createdAuxiliary.Entity, createdAuxiliary.Component);
        _effect = effect;
        _auxiliary = auxiliary;
        _current = dry;
        _target = dry;

        _audio.SetEffect(auxiliary, auxiliary.Comp, effect);
    }

    private void AttachSources(EntityUid auxiliary, float maxDistance)
    {
        var listener = _eye.CurrentEye.Position;
        var maxDistanceSquared = maxDistance * maxDistance;

        var query = AllEntityQuery<AudioComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var audio, out var xform))
        {
            if (!IsClientSide(uid) || audio.Global || audio.Params.Loop || audio.Auxiliary != null || xform.MapID != listener.MapId)
                continue;

            if ((_transform.GetWorldPosition(xform) - listener.Position).LengthSquared() > maxDistanceSquared)
                continue;

            _audio.SetAuxiliary(uid, audio, auxiliary);
        }
    }

    private void DetachSources()
    {
        if (_auxiliary is not { } auxiliary)
            return;

        var query = AllEntityQuery<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            if (audio.Auxiliary == auxiliary.Owner)
                _audio.SetAuxiliary(uid, audio, null);
        }
    }

    private ReverbProperties AnalyzeEnvironment(RoomReverbPrototype settings, ReverbProperties dry)
    {
        var position = _eye.CurrentEye.Position;
        if (_vacuum.ListenerInVacuum
            || position.MapId == MapId.Nullspace
            || !_mapManager.TryFindGridAt(position, out var gridUid, out var grid))
            return dry;

        var start = _map.TileIndicesFor(gridUid, grid, position);
        if (!IsOpenTile(gridUid, grid, start, out var objects))
            return dry;

        _queue.Clear();
        _visited.Clear();
        _queue.Enqueue(start);
        _visited.Add(start);

        var tiles = 1;
        var min = start;
        var max = start;

        while (_queue.TryDequeue(out var tile) && tiles < settings.MaxTiles)
        {
            foreach (var direction in Directions)
            {
                var neighbour = tile + direction;
                if (!_visited.Add(neighbour) || !IsOpenTile(gridUid, grid, neighbour, out var neighbourObjects))
                    continue;

                _queue.Enqueue(neighbour);
                tiles++;
                objects += neighbourObjects;
                min = Vector2i.ComponentMin(min, neighbour);
                max = Vector2i.ComponentMax(max, neighbour);
            }
        }

        var length = Math.Max(max.X - min.X, max.Y - min.Y) + 1f;
        var averageWidth = tiles / length;

        var sizeFactor = Math.Clamp((tiles - settings.SmallRoomTiles) / (settings.HallTiles - settings.SmallRoomTiles), 0f, 1f);
        var corridorFactor = Math.Clamp((settings.CorridorMaxWidth - averageWidth) / settings.CorridorWidthFade, 0f, 1f)
            * Math.Clamp((length - settings.CorridorMinLength) / settings.CorridorLengthFade, 0f, 1f);
        var clutter = Math.Clamp(objects / (float) tiles / settings.ClutterDensity, 0f, 1f);

        var smallRoom = ToReverb(_proto.Index(settings.SmallRoom));
        var mediumRoom = ToReverb(_proto.Index(settings.MediumRoom));
        var hall = ToReverb(_proto.Index(settings.Hall));
        var shortPassage = ToReverb(_proto.Index(settings.ShortPassage));
        var longPassage = ToReverb(_proto.Index(settings.LongPassage));

        var room = sizeFactor < 0.5f
            ? Lerp(smallRoom, mediumRoom, sizeFactor * 2f)
            : Lerp(mediumRoom, hall, sizeFactor * 2f - 1f);

        var passageFactor = Math.Clamp((length - settings.CorridorMinLength) / (settings.LongPassageLength - settings.CorridorMinLength), 0f, 1f);
        var corridor = Lerp(shortPassage, longPassage, passageFactor);

        var result = Lerp(room, corridor, corridorFactor);
        result.Gain = Math.Clamp(result.Gain * settings.Wetness * (1f - settings.ClutterDamping * clutter), 0f, 1f);
        return result;
    }

    private bool IsOpenTile(EntityUid gridUid, MapGridComponent grid, Vector2i indices, out int objects)
    {
        objects = 0;
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tileRef) || _turf.IsSpace(tileRef))
            return false;

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_occluderQuery.TryComp(uid, out var occluder) && occluder.Enabled)
                return false;

            if (_physicsQuery.TryComp(uid, out var physics) && physics.CanCollide)
                objects++;
        }

        return true;
    }

    private ReverbProperties GetDry(RoomReverbPrototype settings)
    {
        return ToReverb(_proto.Index(settings.SmallRoom)) with { Gain = 0f };
    }

    private static ReverbProperties ToReverb(AudioPresetPrototype preset)
    {
        return new ReverbProperties
        {
            Density = preset.Density,
            Diffusion = preset.Diffusion,
            Gain = preset.Gain,
            GainHF = preset.GainHF,
            GainLF = preset.GainLF,
            DecayTime = preset.DecayTime,
            DecayHFRatio = preset.DecayHFRatio,
            DecayLFRatio = preset.DecayLFRatio,
            ReflectionsGain = preset.ReflectionsGain,
            ReflectionsDelay = preset.ReflectionsDelay,
            ReflectionsPan = preset.ReflectionsPan,
            LateReverbGain = preset.LateReverbGain,
            LateReverbDelay = preset.LateReverbDelay,
            LateReverbPan = preset.LateReverbPan,
            EchoTime = preset.EchoTime,
            EchoDepth = preset.EchoDepth,
            ModulationTime = preset.ModulationTime,
            ModulationDepth = preset.ModulationDepth,
            AirAbsorptionGainHF = preset.AirAbsorptionGainHF,
            HFReference = preset.HFReference,
            LFReference = preset.LFReference,
            RoomRolloffFactor = preset.RoomRolloffFactor,
            DecayHFLimit = preset.DecayHFLimit,
        };
    }

    private static ReverbProperties Lerp(ReverbProperties a, ReverbProperties b, float t)
    {
        return new ReverbProperties
        {
            Density = MathHelper.Lerp(a.Density, b.Density, t),
            Diffusion = MathHelper.Lerp(a.Diffusion, b.Diffusion, t),
            Gain = MathHelper.Lerp(a.Gain, b.Gain, t),
            GainHF = MathHelper.Lerp(a.GainHF, b.GainHF, t),
            GainLF = MathHelper.Lerp(a.GainLF, b.GainLF, t),
            DecayTime = MathHelper.Lerp(a.DecayTime, b.DecayTime, t),
            DecayHFRatio = MathHelper.Lerp(a.DecayHFRatio, b.DecayHFRatio, t),
            DecayLFRatio = MathHelper.Lerp(a.DecayLFRatio, b.DecayLFRatio, t),
            ReflectionsGain = MathHelper.Lerp(a.ReflectionsGain, b.ReflectionsGain, t),
            ReflectionsDelay = MathHelper.Lerp(a.ReflectionsDelay, b.ReflectionsDelay, t),
            ReflectionsPan = Vector3.Lerp(a.ReflectionsPan, b.ReflectionsPan, t),
            LateReverbGain = MathHelper.Lerp(a.LateReverbGain, b.LateReverbGain, t),
            LateReverbDelay = MathHelper.Lerp(a.LateReverbDelay, b.LateReverbDelay, t),
            LateReverbPan = Vector3.Lerp(a.LateReverbPan, b.LateReverbPan, t),
            EchoTime = MathHelper.Lerp(a.EchoTime, b.EchoTime, t),
            EchoDepth = MathHelper.Lerp(a.EchoDepth, b.EchoDepth, t),
            ModulationTime = MathHelper.Lerp(a.ModulationTime, b.ModulationTime, t),
            ModulationDepth = MathHelper.Lerp(a.ModulationDepth, b.ModulationDepth, t),
            AirAbsorptionGainHF = MathHelper.Lerp(a.AirAbsorptionGainHF, b.AirAbsorptionGainHF, t),
            HFReference = MathHelper.Lerp(a.HFReference, b.HFReference, t),
            LFReference = MathHelper.Lerp(a.LFReference, b.LFReference, t),
            RoomRolloffFactor = MathHelper.Lerp(a.RoomRolloffFactor, b.RoomRolloffFactor, t),
            DecayHFLimit = t < 0.5f ? a.DecayHFLimit : b.DecayHFLimit,
        };
    }
}
