using System.Linq;
using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Cloning;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Heretic;
using Content.Shared._EinsteinEngines.HeightAdjust;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat;
using Content.Shared.Cloning;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Parallax;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Content.Shared.Store.Components;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Aquila.Heretic;

public sealed class MansusRealmSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly HeightAdjustSystem _heightAdjust = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TileSystem _tileSystem = default!;

    private static readonly ProtoId<CloningSettingsPrototype> BaseCloneSettings = "BaseClone";
    private static readonly EntProtoId ForcedSleep = "StatusEffectForcedSleeping";
    private static readonly LocId VictimSentMessage = "mansus-trial-victim-sent";
    private static readonly LocId ReportPassedMessage = "mansus-trial-report-passed";
    private static readonly LocId ReportFailedMessage = "mansus-trial-report-failed";
    private static readonly LocId RealmMapName = "mansus-realm-map-name";
    private static readonly LocId RealmArrival = "mansus-realm-arrival";
    private static readonly LocId RealmFall = "mansus-realm-fall";
    private static readonly LocId RealmLeaving = "mansus-realm-leaving";
    private static readonly LocId RealmReturn = "mansus-realm-return";
    private static readonly LocId ReturnMessageLoc = "influence-base-message";
    private const int ReturnMessageSize = 22;
    private const int RuneGlyphs = 12;

    private readonly HashSet<EntityUid> _pvsVisionBuffer = new();
    private readonly HashSet<EntityUid> _pvsAnchorBuffer = new();
    private readonly List<EntityUid> _pvsEntitiesBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MansusVictimComponent, MobStateChangedEvent>(OnVictimMobStateChanged);
        SubscribeLocalEvent<MansusReturnVisionComponent, ExpandPvsEvent>(OnExpandPvs);
    }

    /// <summary>
    /// Отправляет жертву на испытание Мансуса и создаёт для него отдельный мир.
    /// </summary>
    /// <param name="victim">Тело, которое отправляют на испытание.</param>
    /// <param name="returnStation">Станция, куда вернётся жертва, может быть null.</param>
    /// <param name="heretic">Еретик, который получит отчёт об исходе испытания.</param>
    /// <returns><see langword="true"/>, если жертву удалось отправить.</returns>
    public bool TrySendToMansus(EntityUid victim, EntityUid? returnStation, EntityUid? heretic = null)
    {
        if (HasComp<MansusVictimComponent>(victim) || TryGetOpenRealm(victim, out _))
            return false;

        if (!_mind.TryGetMind(victim, out var mindId, out var mind)
            || mind.UserId is not { } userId
            || !_player.TryGetSessionById(userId, out var victimSession)
            || victimSession.Status != SessionStatus.InGame
            || !TryComp<HumanoidAppearanceComponent>(victim, out var humanoid)
            || !_proto.TryIndex(humanoid.Species, out var species))
            return false;

        if (mind.VisitingEntity != null)
            _mind.UnVisit(mindId, mind);

        var now = _timing.CurTime;
        var mapUid = _map.CreateMap(out var mapId);
        var realm = EnsureComp<MansusRealmComponent>(mapUid);
        _meta.SetEntityName(mapUid, Loc.GetString(RealmMapName));

        var light = EnsureComp<MapLightComponent>(mapUid);
        light.AmbientLightColor = realm.AmbientLight;
        Dirty(mapUid, light);

        var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
        air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
        air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
        _atmos.SetMapAtmosphere(mapUid, false, air);

        var parallax = EnsureComp<ParallaxComponent>(mapUid);
        parallax.Parallax = realm.Parallax;
        Dirty(mapUid, parallax);

        var grid = _map.CreateGridEntity(mapId);
        RemComp<ImplicitRoofComponent>(grid);

        var gravity = EnsureComp<GravityComponent>(grid);
        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(grid, gravity);

        var tileDef = _proto.Index(realm.PlatformTile);
        var tiles = new List<(Vector2i, Tile)>();
        var half = realm.PlatformSize / 2;
        for (var x = -half; x < realm.PlatformSize - half; x++)
        {
            for (var y = -half; y < realm.PlatformSize - half; y++)
            {
                tiles.Add((new Vector2i(x, y), new Tile(tileDef.TileId, variant: _tileSystem.PickVariant(tileDef))));
            }
        }

        _map.SetTiles(grid, grid.Comp, tiles);

        EnsureComp<GasTileOverlayComponent>(grid);
        var gridAtmos = EnsureComp<GridAtmosphereComponent>(grid);
        _atmos.RebuildGridAtmosphere((grid, gridAtmos, grid.Comp));

        var edge = realm.PlatformSize / 2f - 0.5f;
        var perimeter = edge * 8f;
        for (var i = 0; i < realm.RuneCount; i++)
        {
            var distance = (edge + i * perimeter / realm.RuneCount) % perimeter;
            var rune = SpawnAttachedTo(realm.DormantRune, new EntityCoordinates(grid, PerimeterPoint(distance, edge)));
            _appearance.SetData(rune, MansusRuneVisuals.Glyph, i % RuneGlyphs + 1);
            realm.Runes.Add(rune);
        }

        var copy = SpawnAttachedTo(species.Prototype, new EntityCoordinates(grid, Vector2.Zero));
        _humanoid.CloneAppearance(victim, copy, humanoid);
        _cloning.CloneComponents(victim, copy, BaseCloneSettings);
        RemComp<StoreComponent>(copy);

        _heightAdjust.SetScale(copy, new Vector2(humanoid.Width, humanoid.Height));
        _meta.SetEntityName(copy, Name(victim));
        foreach (var slot in realm.CarriedSlots)
        {
            if (_inventory.TryUnequip(victim, slot, out var worn, silent: true, force: true))
            {
                if (!_inventory.TryEquip(copy, worn.Value, slot, silent: true, force: true))
                    SpawnEquippedCopy(copy, worn.Value, slot);

                continue;
            }

            if (_inventory.TryGetSlotEntity(victim, slot, out var item))
                SpawnEquippedCopy(copy, item.Value, slot);
        }

        var victimComp = EnsureComp<MansusVictimComponent>(copy);
        victimComp.ArrivalTime = now;
        Dirty(copy, victimComp);
        EnsureComp<MansusTouchedComponent>(mindId);
        var ritualCoords = Transform(victim).Coordinates;
        _mind.TransferTo(mindId, copy, mind: mind);

        realm.Victim = copy;
        realm.Mind = mindId;
        realm.Grid = grid;
        realm.ReturnStation = returnStation;
        realm.RitualCoords = ritualCoords;
        realm.Heretic = heretic;
        realm.StartTime = now;
        realm.NextHand = now + realm.GracePeriod;

        if (_player.TryGetSessionByEntity(copy, out var session))
            realm.MusicStream = _audio.PlayGlobal(realm.Music, session)?.Entity;

        _popup.PopupEntity(Loc.GetString(RealmArrival), copy, copy, PopupType.LargeCaution);

        var sentMessage = Loc.GetString(VictimSentMessage);
        _chat.ChatMessageToOne(ChatChannel.Server, sentMessage, sentMessage, default, false, victimSession.Channel, canCoalesce: false);
        return true;
    }

    private bool TryGetOpenRealm(EntityUid victim, out Entity<MansusRealmComponent> realm)
    {
        var query = EntityQueryEnumerator<MansusRealmComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Finished || comp.Victim != victim)
                continue;

            realm = (uid, comp);
            return true;
        }

        realm = default;
        return false;
    }

    private void SpawnEquippedCopy(EntityUid target, EntityUid item, string slot)
    {
        if (Prototype(item) is not { } proto)
            return;

        var copy = SpawnAttachedTo(proto.ID, Transform(target).Coordinates);
        if (!_inventory.TryEquip(target, copy, slot, silent: true, force: true))
            QueueDel(copy);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MansusRealmComponent>();
        while (query.MoveNext(out var uid, out var realm))
        {
            if (realm.Finished)
                continue;

            if (TerminatingOrDeleted(realm.Victim))
            {
                Finish((uid, realm), false);
                continue;
            }

            if (Transform(realm.Victim).GridUid != realm.Grid)
            {
                _popup.PopupEntity(Loc.GetString(RealmFall), realm.Victim, realm.Victim, PopupType.LargeCaution);
                Finish((uid, realm), false);
                continue;
            }

            if (realm.LeaveTime is { } leaveTime)
            {
                var leaveProgress = (float) ((now - leaveTime) / realm.LeaveDuration);
                if (leaveProgress >= 1f)
                {
                    Finish((uid, realm), true);
                    continue;
                }

                _audio.SetVolume(realm.MusicStream, MathHelper.Lerp(realm.Music.Params.Volume, realm.MusicFadeVolume, leaveProgress));
                continue;
            }

            var elapsed = now - realm.StartTime - realm.GracePeriod;
            if (elapsed >= realm.SurviveDuration)
            {
                StartLeaving((uid, realm), now);
                continue;
            }

            var progress = Math.Clamp((float) (elapsed / realm.SurviveDuration), 0f, 1f);
            LightRunes(realm, (int) (progress * realm.RuneCount));
            var intervalSeconds = MathHelper.Lerp((float) realm.StartInterval.TotalSeconds, (float) realm.EndInterval.TotalSeconds, progress);

            if (now >= realm.NextHand)
            {
                SpawnPattern(realm, MansusPattern.Single);
                realm.NextHand = now + TimeSpan.FromSeconds(intervalSeconds);
            }

            if (elapsed >= realm.PatternUnlockTime && now >= realm.NextHeavy)
            {
                var heavy = PickHeavyPattern(realm, elapsed);
                var (cost, lockout) = SpawnPattern(realm, heavy);
                var heavyDelay = TimeSpan.FromSeconds(intervalSeconds * cost * realm.HeavyIntervalMultiplier);
                realm.NextHeavy = now + (heavyDelay > lockout ? heavyDelay : lockout);

                var lockoutEnd = now + lockout;
                if (heavy == MansusPattern.Wall && realm.NextHand < lockoutEnd)
                    realm.NextHand = lockoutEnd;
            }
        }

        var visionQuery = EntityQueryEnumerator<MansusReturnVisionComponent, TransformComponent>();
        while (visionQuery.MoveNext(out var visionUid, out var vision, out var xform))
        {
            if (now - vision.StartedAt >= vision.ExpireAfter)
            {
                RemCompDeferred<MansusReturnVisionComponent>(visionUid);
                continue;
            }

            if (TerminatingOrDeleted(visionUid) || _mobState.IsDead(visionUid))
            {
                RemCompDeferred<MansusReturnVisionComponent>(visionUid);
                continue;
            }

            if (vision.Anchor is not { } anchor)
            {
                RemCompDeferred<MansusReturnVisionComponent>(visionUid);
                continue;
            }

            if (!Exists(anchor.EntityId))
            {
                RemCompDeferred<MansusReturnVisionComponent>(visionUid);
                continue;
            }

            if (!xform.Coordinates.TryDistance(EntityManager, _transform, anchor, out var distance)
                || distance > vision.Radius)
            {
                vision.InRangeSince = null;
                continue;
            }

            if (vision.InRangeSince is not { } inRangeSince)
            {
                vision.InRangeSince = now;
                continue;
            }

            if (now - inRangeSince >= vision.ClearDelay)
                RemCompDeferred<MansusReturnVisionComponent>(visionUid);
        }
    }

    private MansusPattern PickHeavyPattern(MansusRealmComponent realm, TimeSpan elapsed)
    {
        var options = new List<(MansusPattern Pattern, float Weight)>
        {
            (MansusPattern.Fan, realm.FanWeight),
            (MansusPattern.Pincer, realm.PincerWeight),
        };

        if (elapsed >= realm.WallUnlockTime)
            options.Add((MansusPattern.Wall, realm.WallWeight));

        var roll = _random.NextFloat(options.Sum(o => o.Weight));
        foreach (var (option, weight) in options)
        {
            if (roll < weight)
                return option;

            roll -= weight;
        }

        return options[^1].Pattern;
    }

    private (float Cost, TimeSpan Lockout) SpawnPattern(MansusRealmComponent realm, MansusPattern pattern)
    {
        var speed = _random.NextFloat(realm.MinHandSpeed, realm.MaxHandSpeed);
        var handLockout = TimeSpan.FromSeconds((realm.SpawnRadius - realm.PlatformSize / 2f) / speed) + realm.LockoutPadding;
        switch (pattern)
        {
            case MansusPattern.Fan:
            {
                var start = RandomEdge(realm);
                var direction = (RandomPlatformPoint(realm) - start).Normalized();
                for (var i = -1; i <= 1; i++)
                {
                    var spread = new Angle(MathHelper.DegreesToRadians(realm.FanAngle * i)).RotateVec(direction);
                    SpawnHand(realm, start, spread, speed);
                }

                return (realm.FanCost, handLockout);
            }
            case MansusPattern.Pincer:
            {
                var target = VictimPosition(realm);
                var offset = _random.NextAngle().ToVec() * realm.SpawnRadius;
                SpawnHand(realm, target + offset, -offset.Normalized(), speed);
                SpawnHand(realm, target - offset, offset.Normalized(), speed);
                return (realm.PincerCost, handLockout);
            }
            case MansusPattern.Wall:
            {
                var direction = _random.NextAngle().ToVec();
                var side = new Vector2(-direction.Y, direction.X);
                var reach = realm.PlatformSize * 0.75f;
                var count = (int) (reach * 2f / realm.WallSpacing) + 1;
                var gapMin = count / 4;
                var gapMax = Math.Max(gapMin + 1, count * 3 / 4 - realm.WallGap);
                var gapStart = _random.Next(gapMin, gapMax);
                for (var i = 0; i < count; i++)
                {
                    if (i >= gapStart && i < gapStart + realm.WallGap)
                        continue;

                    var lateral = -reach + i * realm.WallSpacing;
                    SpawnHand(realm, -direction * realm.SpawnRadius + side * lateral, direction, speed);
                }

                return (realm.WallCost, handLockout);
            }
            default:
            {
                var start = RandomEdge(realm);
                SpawnHand(realm, start, (RandomPlatformPoint(realm) - start).Normalized(), speed);
                return (1f, TimeSpan.Zero);
            }
        }
    }

    private static Vector2 PerimeterPoint(float distance, float edge)
    {
        var side = edge * 2f;
        if (distance < side)
            return new Vector2(-edge + distance, edge);

        if (distance < side * 2f)
            return new Vector2(edge, edge - (distance - side));

        if (distance < side * 3f)
            return new Vector2(edge - (distance - side * 2f), -edge);

        return new Vector2(-edge, -edge + (distance - side * 3f));
    }

    private void LightRunes(MansusRealmComponent realm, int target)
    {
        while (realm.LitRunes < target && realm.LitRunes < realm.Runes.Count)
        {
            var rune = realm.Runes[realm.LitRunes];
            var coords = Transform(rune).Coordinates;
            QueueDel(rune);
            var lit = SpawnAttachedTo(realm.LitRune, coords);
            _appearance.SetData(lit, MansusRuneVisuals.Glyph, realm.LitRunes % RuneGlyphs + 1);
            realm.Runes[realm.LitRunes] = lit;
            realm.LitRunes++;
        }
    }

    private Vector2 RandomEdge(MansusRealmComponent realm)
    {
        return _random.NextAngle().ToVec() * realm.SpawnRadius;
    }

    private Vector2 RandomPlatformPoint(MansusRealmComponent realm)
    {
        var spread = realm.PlatformSize / 2 - 1;
        return new Vector2(_random.NextFloat(-spread, spread), _random.NextFloat(-spread, spread));
    }

    private Vector2 VictimPosition(MansusRealmComponent realm)
    {
        return Transform(realm.Victim).LocalPosition;
    }

    private void SpawnHand(MansusRealmComponent realm, Vector2 start, Vector2 direction, float speed)
    {
        var hand = SpawnAttachedTo(_random.Pick(realm.HandPrototypes), new EntityCoordinates(realm.Grid, start));

        var angle = direction.ToWorldAngle();
        if (TryComp<ProjectileComponent>(hand, out var projectile))
            angle += projectile.Angle;

        _transform.SetLocalRotation(hand, angle);

        if (TryComp<MansusHandComponent>(hand, out var arm))
        {
            arm.Origin = _transform.GetWorldPosition(hand);
            Dirty(hand, arm);
        }

        _physics.SetLinearVelocity(hand, direction * speed);

        var despawn = EnsureComp<TimedDespawnComponent>(hand);
        despawn.Lifetime = realm.SpawnRadius * 2.5f / speed + 1f;
    }

    private void StartLeaving(Entity<MansusRealmComponent> realm, TimeSpan now)
    {
        realm.Comp.LeaveTime = now;
        LightRunes(realm.Comp, realm.Comp.RuneCount);

        if (TryComp<MansusVictimComponent>(realm.Comp.Victim, out var victim))
        {
            victim.LeaveTime = now;
            victim.FadeOutDuration = realm.Comp.LeaveDuration;
            Dirty(realm.Comp.Victim, victim);
        }

        var hands = EntityQueryEnumerator<MansusHandComponent, TransformComponent>();
        while (hands.MoveNext(out var hand, out _, out var xform))
        {
            if (xform.MapUid == realm.Owner)
                QueueDel(hand);
        }

        _popup.PopupEntity(Loc.GetString(RealmLeaving), realm.Comp.Victim, realm.Comp.Victim, PopupType.Large);
    }

    private void OnVictimMobStateChanged(Entity<MansusVictimComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive
            || Transform(ent).MapUid is not { } mapUid
            || !TryComp<MansusRealmComponent>(mapUid, out var realm)
            || realm.Finished)
            return;

        Finish((mapUid, realm), false);
    }

    private void OnExpandPvs(Entity<MansusReturnVisionComponent> ent, ref ExpandPvsEvent args)
    {
        var flags = LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries;

        _pvsVisionBuffer.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, _pvsVisionBuffer, flags);

        _pvsAnchorBuffer.Clear();
        if (ent.Comp.Anchor is { } anchor && Exists(anchor.EntityId))
            _lookup.GetEntitiesInRange(anchor, ent.Comp.Radius, _pvsAnchorBuffer, flags);

        if (_pvsVisionBuffer.Count == 0 && _pvsAnchorBuffer.Count == 0)
            return;

        if (args.Entities == null)
        {
            _pvsEntitiesBuffer.Clear();
            args.Entities = _pvsEntitiesBuffer;
        }

        args.Entities.AddRange(_pvsVisionBuffer);
        args.Entities.AddRange(_pvsAnchorBuffer);
    }

    private void Finish(Entity<MansusRealmComponent> realm, bool survived)
    {
        realm.Comp.Finished = true;
        realm.Comp.MusicStream = _audio.Stop(realm.Comp.MusicStream);
        var victim = realm.Comp.Victim;

        if (survived && !TerminatingOrDeleted(victim))
        {
            ReturnVictim(realm.Comp, victim);
            ReportToHeretic(realm.Comp, true, victim);
        }
        else
        {
            ReturnFailedVictim(realm, victim);
            if (!TerminatingOrDeleted(victim))
                ReportToHeretic(realm.Comp, false, victim);
        }

        QueueDel(realm);
    }

    private void ReturnVictim(MansusRealmComponent realm, EntityUid victim)
    {
        RemComp<MansusVictimComponent>(victim);

        var coords = GetReturnCoordinates(realm);

        _transform.SetCoordinates(victim, coords);
        _transform.AttachToGridOrMap(victim);

        var vision = EnsureComp<MansusReturnVisionComponent>(victim);
        vision.Anchor = realm.RitualCoords;
        vision.StartedAt = _timing.CurTime;
        vision.InRangeSince = null;

        SpawnAttachedTo(realm.ReturnEffect, coords);
        _audio.PlayPvs(realm.ReturnSound, coords);

        _status.TryAddStatusEffectDuration(victim, ForcedSleep, realm.SleepDuration);

        if (!_player.TryGetSessionByEntity(victim, out var session))
            return;

        var message = Loc.GetString(RealmReturn);
        var size = ReturnMessageSize;
        var wrapped = Loc.GetString(ReturnMessageLoc, ("size", size), ("text", message));
        SharedChatSystem.UpdateFontSize(size, ref message, ref wrapped);
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, session.Channel, canCoalesce: false);
    }

    private void ReturnFailedVictim(Entity<MansusRealmComponent> realm, EntityUid victim)
    {
        EntityCoordinates? coords = null;
        if (!TerminatingOrDeleted(victim))
        {
            coords = GetReturnCoordinates(realm.Comp);

            _transform.SetCoordinates(victim, coords.Value);
            _transform.AttachToGridOrMap(victim);

            if (!_mobState.IsDead(victim))
                _mobState.ChangeMobState(victim, MobState.Dead);

            RemComp<MansusVictimComponent>(victim);

            SpawnAttachedTo(realm.Comp.ReturnEffect, coords.Value);
            _audio.PlayPvs(realm.Comp.ReturnSound, coords.Value);
        }

        ReleaseMind(realm, coords);
    }

    private void ReleaseMind(Entity<MansusRealmComponent> realm, EntityCoordinates? bodyCoords)
    {
        if (!TryComp<MindComponent>(realm.Comp.Mind, out var mind))
            return;

        if (mind.VisitingEntity is { } visiting)
        {
            if (Transform(visiting).MapUid == realm.Owner
                && TryComp<GhostComponent>(visiting, out var ghost))
            {
                _transform.SetCoordinates(visiting, bodyCoords ?? _gameTicker.GetObserverSpawnPoint());
                _ghost.SetCanReturnToBody((visiting, ghost), false);
            }

            return;
        }

        if (mind.OwnedEntity != realm.Comp.Victim)
            return;

        if (bodyCoords is not { } coords)
        {
            _ghost.SpawnGhost((realm.Comp.Mind, mind));
            return;
        }

        if (_ghost.SpawnGhost((realm.Comp.Mind, mind), coords, true) is not { } spawned
            || !TryComp<GhostComponent>(spawned, out var spawnedGhost))
            return;

        _ghost.SetCanReturnToBody((spawned, spawnedGhost), false);
    }

    private void ReportToHeretic(MansusRealmComponent realm, bool survived, EntityUid victim)
    {
        if (realm.Heretic is not { } heretic
            || TerminatingOrDeleted(victim)
            || !_mind.TryGetMind(heretic, out _, out var hereticMind)
            || hereticMind.UserId is not { } userId
            || !_player.TryGetSessionById(userId, out var session))
            return;

        var location = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(victim));
        var message = Loc.GetString(survived ? ReportPassedMessage : ReportFailedMessage,
            ("victim", Name(victim)),
            ("location", location));
        _chat.ChatMessageToOne(ChatChannel.Server, message, message, default, false, session.Channel, canCoalesce: false);
    }

    private EntityCoordinates GetReturnCoordinates(MansusRealmComponent realm)
    {
        return TryFindReturnCoordinates(realm.ReturnStation, out var found)
            ? found
            : _gameTicker.GetObserverSpawnPoint();
    }

    private bool TryFindReturnCoordinates(EntityUid? station, out EntityCoordinates coords)
    {
        coords = EntityCoordinates.Invalid;

        station ??= _station.GetStations().FirstOrNull();
        if (station is not { } stationUid
            || _station.GetLargestGrid(stationUid) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var mapUid = Transform(gridUid).MapUid;
        var aabb = grid.LocalAABB;

        for (var i = 0; i < 30; i++)
        {
            var index = new Vector2i(
                _random.Next((int) aabb.Left, (int) aabb.Right),
                _random.Next((int) aabb.Bottom, (int) aabb.Top));

            if (!_map.TryGetTileRef(gridUid, grid, index, out var tileRef)
                || tileRef.Tile.IsEmpty
                || _turf.IsSpace(tileRef)
                || _atmos.IsTileSpace(gridUid, mapUid, index)
                || _atmos.IsTileAirBlockedCached(gridUid, index))
                continue;

            coords = _map.GridTileToLocal(gridUid, grid, index);
            return true;
        }

        return false;
    }
}
