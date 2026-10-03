using Content.Server.Explosion.EntitySystems;
using Content.Shared._Aquila.EventContent.Sir_Felix;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.EventContent.Sir_Felix;

public sealed class OrbitalStrikeSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly VerbCategory CountCategory = new("orbital-strike-category-count", null);
    private static readonly VerbCategory RadiusCategory = new("orbital-strike-category-radius", null);
    private static readonly VerbCategory ModeCategory = new("orbital-strike-category-mode", null);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalStrikeComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<OrbitalStrikeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnUseInHand(Entity<OrbitalStrikeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (HasComp<OrbitalStrikeActiveComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("orbital-strike-popup-busy"), ent, args.User);
            return;
        }

        var active = EnsureComp<OrbitalStrikeActiveComponent>(ent);
        active.Center = _transform.GetMapCoordinates(args.User);
        active.Radius = ent.Comp.CurrentRadius;
        active.Mode = ent.Comp.Modes[Math.Clamp(ent.Comp.CurrentMode, 0, ent.Comp.Modes.Count - 1)];
        active.Remaining = ent.Comp.CurrentPodCount;
        active.NextSpawn = _timing.CurTime;

        _popup.PopupEntity(Loc.GetString("orbital-strike-popup-launch", ("count", active.Remaining)), ent, args.User);
    }

    private void OnGetVerbs(Entity<OrbitalStrikeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var priority = 0;

        for (var i = 0; i < ent.Comp.PodCounts.Count; i++)
        {
            var count = ent.Comp.PodCounts[i];
            AddVerb(ref args, CountCategory, ref priority,
                Loc.GetString("orbital-strike-verb-count", ("count", count), ("selected", ent.Comp.CurrentPodCount == count)),
                () => ent.Comp.CurrentPodCount = count,
                Loc.GetString("orbital-strike-popup-count", ("count", count)),
                ent,
                user);
        }

        foreach (var radius in ent.Comp.Radii)
        {
            AddVerb(ref args, RadiusCategory, ref priority,
                Loc.GetString("orbital-strike-verb-radius", ("radius", radius), ("selected", MathHelper.CloseTo(ent.Comp.CurrentRadius, radius))),
                () => ent.Comp.CurrentRadius = radius,
                Loc.GetString("orbital-strike-popup-radius", ("radius", radius)),
                ent,
                user);
        }

        for (var i = 0; i < ent.Comp.Modes.Count; i++)
        {
            var index = i;
            var mode = Loc.GetString(ent.Comp.Modes[i].Name);
            AddVerb(ref args, ModeCategory, ref priority,
                Loc.GetString("orbital-strike-verb-mode", ("mode", mode), ("selected", ent.Comp.CurrentMode == index)),
                () => ent.Comp.CurrentMode = index,
                Loc.GetString("orbital-strike-popup-mode", ("mode", mode)),
                ent,
                user);
        }
    }

    private void AddVerb(
        ref GetVerbsEvent<AlternativeVerb> args,
        VerbCategory category,
        ref int priority,
        string text,
        Action select,
        string popup,
        EntityUid item,
        EntityUid user)
    {
        args.Verbs.Add(new AlternativeVerb
        {
            Text = text,
            Category = category,
            Priority = priority--,
            Act = () =>
            {
                select();
                _popup.PopupEntity(popup, item, user);
            },
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var strikes = EntityQueryEnumerator<OrbitalStrikeActiveComponent, OrbitalStrikeComponent>();
        while (strikes.MoveNext(out var uid, out var active, out var strike))
        {
            if (curTime < active.NextSpawn)
                continue;

            SpawnPod(active, strike, curTime);

            active.NextSpawn = curTime + strike.SpawnInterval;
            if (--active.Remaining <= 0)
                RemCompDeferred<OrbitalStrikeActiveComponent>(uid);
        }

        var pods = EntityQueryEnumerator<OrbitalStrikePodComponent>();
        while (pods.MoveNext(out var uid, out var pod))
        {
            if (curTime < pod.ExplodeAt)
                continue;

            _explosion.QueueExplosion(
                _transform.GetMapCoordinates(uid),
                ExplosionSystem.DefaultExplosionPrototypeId,
                pod.Mode.Intensity,
                pod.Mode.Slope,
                pod.Mode.MaxTileIntensity,
                null,
                maxTileBreak: 0);

            QueueDel(uid);
        }
    }

    private void SpawnPod(OrbitalStrikeActiveComponent active, OrbitalStrikeComponent strike, TimeSpan curTime)
    {
        var offset = _random.NextAngle().ToVec() * active.Radius * MathF.Sqrt(_random.NextFloat());
        var coordinates = new MapCoordinates(active.Center.Position + offset, active.Center.MapId);

        var pod = Spawn(strike.PodPrototype, coordinates);
        var podComp = EnsureComp<OrbitalStrikePodComponent>(pod);
        podComp.Mode = active.Mode;
        podComp.ExplodeAt = curTime + strike.ExplosionDelay;
    }
}
