using Content.Goobstation.Maths.FixedPoint;
using Content.Server._Aquila.Heretic.Conjurations.Components;
using Content.Server.Heretic.Components;
using Content.Shared._Aquila.Heretic.Conjurations;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Conjurations.Systems;

/// <summary>
/// Ведёт Зубастый тигель: пассивно наполняет массу, принимает органы и части тел, слушается кодекса
/// и хватки Мансуса, а при полной массе варит одно из зелий.
/// </summary>
public sealed class MawedCrucibleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId CodexCicatrix = "CodexCicatrix";
    private static readonly EntProtoId HereticEldritchFlask = "HereticEldritchFlask";
    private static readonly ProtoId<ReagentPrototype> EldritchEssence = "EldritchEssence";
    private const string DrinkSolution = "drink";

    private static readonly LocId FullMessage = "mawed-crucible-full";
    private static readonly LocId EmptyMessage = "mawed-crucible-empty";
    private static readonly LocId NotFullMessage = "mawed-crucible-not-full";
    private static readonly LocId FedMessage = "mawed-crucible-fed";
    private static readonly LocId VitalMessage = "mawed-crucible-vital";
    private static readonly LocId BiteMessage = "mawed-crucible-bite";
    private static readonly LocId AnchorOnMessage = "mawed-crucible-anchor-on";
    private static readonly LocId AnchorOffMessage = "mawed-crucible-anchor-off";
    private static readonly LocId AnchorFailMessage = "mawed-crucible-anchor-fail";
    private static readonly LocId BrewVerbMessage = "mawed-crucible-brew-verb";
    private static readonly LocId BrewedMessage = "mawed-crucible-brewed";
    private static readonly LocId SolutionFullMessage = "mawed-crucible-solution-full";
    private static readonly LocId FlaskEmptyMessage = "mawed-crucible-flask-empty";
    private static readonly LocId FlaskFilledMessage = "mawed-crucible-flask-filled";

    /// <summary>
    /// Подключает инициализацию, кормление и альтернативные действия тигля.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MawedCrucibleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MawedCrucibleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MawedCrucibleComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<MawedCrucibleComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    /// <summary>
    /// Пассивно добавляет тиглям массу до максимума.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MawedCrucibleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Mass >= comp.MaxMass || now < comp.NextRefill)
                continue;

            comp.Mass++;
            comp.NextRefill = now + comp.RefillInterval;
            _audio.PlayPvs(comp.RefillSound, uid);
            UpdateVisuals(uid, comp);
        }
    }

    private void OnMapInit(Entity<MawedCrucibleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Mass = ent.Comp.StartMass;
        ent.Comp.NextRefill = _timing.CurTime + ent.Comp.RefillInterval;
        UpdateVisuals(ent.Owner, ent.Comp);
    }

    private void OnInteractUsing(Entity<MawedCrucibleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryInteract(ent.AsNullable(), args.User, args.Used);
    }

    private void OnInteractHand(Entity<MawedCrucibleComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || _heretic.IsHereticOrGhoul(args.User))
            return;

        args.Handled = TryBite(ent, args.User);
    }

    /// <summary>
    /// Разбирает предмет, которым тронули тигель: кодекс и хватка переключают якорь, фляга доливает эссенцию,
    /// органы и части тел идут на корм, всем остальным занимается укус.
    /// </summary>
    public bool TryInteract(Entity<MawedCrucibleComponent?> ent, EntityUid user, EntityUid used)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!_heretic.IsHereticOrGhoul(user))
            return TryBite((ent.Owner, ent.Comp), user);

        if (HasComp<MansusGraspComponent>(used) || Prototype(used)?.ID == CodexCicatrix.Id)
            return TryToggleAnchor((ent.Owner, ent.Comp), user);

        if (Prototype(used)?.ID == HereticEldritchFlask.Id)
            return TryFillFromFlask((ent.Owner, ent.Comp), user, used);

        if (HasComp<BodyPartComponent>(used) || HasComp<OrganComponent>(used))
            return TryFeed((ent.Owner, ent.Comp), user, used);

        return false;
    }

    private bool TryBite(Entity<MawedCrucibleComponent> ent, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString(BiteMessage), ent.Owner, user);
        _damageable.TryChangeDamage(user, ent.Comp.BiteDamage, origin: ent.Owner);
        return true;
    }

    private bool TryToggleAnchor(Entity<MawedCrucibleComponent> ent, EntityUid user)
    {
        var xform = Transform(ent);

        if (xform.Anchored)
        {
            _transform.Unanchor(ent, xform);
            _popup.PopupEntity(Loc.GetString(AnchorOffMessage), ent.Owner, user);
        }
        else
        {
            if (!_transform.AnchorEntity(ent, xform))
            {
                _popup.PopupEntity(Loc.GetString(AnchorFailMessage), ent.Owner, user);
                return false;
            }

            _popup.PopupEntity(Loc.GetString(AnchorOnMessage), ent.Owner, user);
        }

        _audio.PlayPvs(ent.Comp.AnchorSound, ent.Owner);
        return true;
    }

    private bool TryFeed(Entity<MawedCrucibleComponent> ent, EntityUid user, EntityUid used)
    {
        if (HasComp<BrainComponent>(used) || HasComp<HeartComponent>(used))
        {
            _popup.PopupEntity(Loc.GetString(VitalMessage), ent.Owner, user);
            return true;
        }

        if (ent.Comp.Mass >= ent.Comp.MaxMass)
        {
            _popup.PopupEntity(Loc.GetString(FullMessage), ent.Owner, user);
            return true;
        }

        var name = Name(used);
        QueueDel(used);
        ent.Comp.Mass++;
        _audio.PlayPvs(ent.Comp.FeedSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString(FedMessage, ("item", name)), ent.Owner, user);
        UpdateVisuals(ent.Owner, ent.Comp);
        return true;
    }

    private bool TryFillFromFlask(Entity<MawedCrucibleComponent> ent, EntityUid user, EntityUid flask)
    {
        if (ent.Comp.Mass < ent.Comp.MaxMass)
        {
            _popup.PopupEntity(Loc.GetString(NotFullMessage), ent.Owner, user);
            return true;
        }

        if (!_solutions.TryGetSolution(ent.Owner, DrinkSolution, out var targetSoln, out var target))
            return false;

        if (!_solutions.TryGetSolution(flask, DrinkSolution, out var flaskSoln, out var flaskSolution))
            return false;

        var essence = new ReagentId(EldritchEssence.Id, null);
        var amount = FixedPoint2.Min(flaskSolution.GetReagentQuantity(essence), target.AvailableVolume);

        if (amount <= FixedPoint2.Zero)
        {
            var message = target.AvailableVolume <= FixedPoint2.Zero ? SolutionFullMessage : FlaskEmptyMessage;
            _popup.PopupEntity(Loc.GetString(message), ent.Owner, user);
            return true;
        }

        _solutions.TryAddReagent(targetSoln.Value, essence, amount, out var accepted);
        if (accepted <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString(SolutionFullMessage), ent.Owner, user);
            return true;
        }

        _solutions.RemoveReagent(flaskSoln.Value, essence, accepted);
        ent.Comp.Mass--;
        ent.Comp.NextRefill = _timing.CurTime + ent.Comp.RefillInterval;
        _audio.PlayPvs(ent.Comp.FeedSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString(FlaskFilledMessage), ent.Owner, user);
        UpdateVisuals(ent.Owner, ent.Comp);
        return true;
    }

    private void OnGetVerbs(Entity<MawedCrucibleComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_heretic.IsHereticOrGhoul(args.User))
            return;

        if (ent.Comp.Mass < ent.Comp.MaxMass)
            return;

        var user = args.User;
        foreach (var brew in ent.Comp.Brews)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString(BrewVerbMessage, ("brew", Loc.GetString("ent-" + brew.Id))),
                Act = () => Brew(ent, user, brew),
                Priority = 2,
            });
        }
    }

    private void Brew(Entity<MawedCrucibleComponent> ent, EntityUid user, EntProtoId brew)
    {
        if (ent.Comp.Mass < ent.Comp.MaxMass)
        {
            _popup.PopupEntity(Loc.GetString(EmptyMessage), ent.Owner, user);
            return;
        }

        SpawnAtPosition(brew, Transform(ent).Coordinates);
        ent.Comp.Mass = 0;
        ent.Comp.NextRefill = _timing.CurTime + ent.Comp.RefillInterval;
        _audio.PlayPvs(ent.Comp.BrewSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString(BrewedMessage, ("brew", Loc.GetString("ent-" + brew.Id))), ent.Owner, user);
        UpdateVisuals(ent.Owner, ent.Comp);
    }

    private void UpdateVisuals(EntityUid uid, MawedCrucibleComponent comp)
    {
        var state = comp.Mass > 0 ? comp.FullState : comp.EmptyState;
        _appearance.SetData(uid, CrucibleVisuals.State, state);
    }
}
