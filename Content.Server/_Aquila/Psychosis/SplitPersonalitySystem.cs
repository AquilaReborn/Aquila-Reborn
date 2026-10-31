using System.Diagnostics.CodeAnalysis;
using Content.Server._Aquila.Psychosis.Components;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared._Aquila.Psychosis;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Actions;
using Content.Shared.Emoting;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Psychosis;

/// <summary>
/// Раздвоение личности: две личности делят одно тело и меняются местами по взаимному согласию.
/// </summary>
public sealed class SplitPersonalitySystem : EntitySystem
{
    private static readonly EntProtoId AlterPrototype = "SplitPersonalityAlter";
    private static readonly EntProtoId PassengerPrototype = "SplitPersonalityPassenger";
    private static readonly EntProtoId SwapAction = "ActionSplitPersonalitySwap";
    private static readonly ProtoId<CollectiveMindPrototype> InnerVoice = "SplitPersonalityInnerVoice";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimTime = TimeSpan.FromMinutes(5);

    [Dependency] private readonly CollectiveMindUpdateSystem _collectiveMind = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PsychosisMedicineSystem _medicine = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SplitPersonalityComponent, SplitPersonalitySwapActionEvent>(OnHostSwapAction);
        SubscribeLocalEvent<SplitPersonalityComponent, MobStateChangedEvent>(OnHostStateChanged);
        SubscribeLocalEvent<SplitPersonalityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SplitPersonalityPassengerComponent, SplitPersonalitySwapActionEvent>(OnPassengerSwapAction);
        SubscribeLocalEvent<SplitPersonalityPassengerComponent, MindAddedMessage>(OnPassengerMindAdded);
        SubscribeLocalEvent<SplitPersonalityPassengerComponent, SpeakAttemptEvent>(OnPassengerSpeakAttempt);
        SubscribeLocalEvent<SplitPersonalityPassengerComponent, EmoteAttemptEvent>(OnPassengerEmoteAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SplitPersonalityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextCheck)
                continue;

            comp.NextCheck = now + CheckInterval;

            if (comp.AlterMind == null && now > comp.ClaimDeadline)
            {
                RemCompDeferred<SplitPersonalityComponent>(uid);
                continue;
            }

            _medicine.Check(uid, out var suppressed, out var curing);
            if (suppressed)
                ReturnControl(uid, comp);

            if (!curing)
                continue;

            comp.CureProgress += (float) CheckInterval.TotalSeconds;
            if (comp.CureProgress < comp.CureTime)
                continue;

            _popup.PopupEntity(Loc.GetString("split-personality-cured"), uid, uid, PopupType.Medium);
            RemCompDeferred<SplitPersonalityComponent>(uid);
        }
    }

    /// <summary>
    /// Вызывает у существа раздвоение личности. Вторую личность займёт призрак.
    /// </summary>
    public bool TryAfflict(EntityUid host)
    {
        if (HasComp<SplitPersonalityComponent>(host)
            || !_mind.TryGetMind(host, out var mind, out _))
            return false;

        var comp = AddComp<SplitPersonalityComponent>(host);
        comp.OriginalMind = mind;
        comp.ClaimDeadline = _timing.CurTime + ClaimTime;
        comp.NextCheck = _timing.CurTime + CheckInterval;
        comp.Passenger = SpawnPassenger(host, AlterPrototype);

        _actions.AddAction(host, ref comp.SwapAction, SwapAction);

        if (!TryComp<CollectiveMindComponent>(host, out var collective))
        {
            collective = AddComp<CollectiveMindComponent>(host);
            comp.AddedCollectiveMind = true;
        }

        collective.Channels.Add(InnerVoice);
        UpdateCollectiveMind(host, collective);
        return true;
    }

    /// <summary>
    /// Обрабатывает ответ на просьбу поменяться местами.
    /// </summary>
    public void AnswerRequest(EntityUid host, bool accepted)
    {
        if (!TryComp<SplitPersonalityComponent>(host, out var comp))
            return;

        var requester = comp.PendingRequester;
        comp.PendingRequester = null;

        if (!accepted)
        {
            if (requester != null && !TerminatingOrDeleted(requester.Value))
                _popup.PopupEntity(Loc.GetString("split-personality-swap-denied"), requester.Value, requester.Value);
            return;
        }

        _medicine.Check(host, out var suppressed, out _);
        if (suppressed || !TrySwap(host, comp))
        {
            if (requester != null && !TerminatingOrDeleted(requester.Value))
                _popup.PopupEntity(Loc.GetString("split-personality-swap-unavailable"), requester.Value, requester.Value);
        }
    }

    private void OnHostSwapAction(Entity<SplitPersonalityComponent> ent, ref SplitPersonalitySwapActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        RequestSwap(ent, ent.Comp, args.Performer);
    }

    private void OnPassengerSwapAction(Entity<SplitPersonalityPassengerComponent> ent, ref SplitPersonalitySwapActionEvent args)
    {
        if (args.Handled || !TryComp<SplitPersonalityComponent>(ent.Comp.Host, out var comp))
            return;

        args.Handled = true;
        RequestSwap(ent.Comp.Host, comp, args.Performer);
    }

    private void RequestSwap(EntityUid host, SplitPersonalityComponent comp, EntityUid requester)
    {
        if (comp.AlterMind == null)
        {
            _popup.PopupEntity(Loc.GetString("split-personality-swap-no-alter"), requester, requester);
            return;
        }

        _medicine.Check(host, out var suppressed, out _);
        if (suppressed)
        {
            _popup.PopupEntity(Loc.GetString("split-personality-swap-suppressed"), requester, requester);
            return;
        }

        if (comp.PendingRequester != null)
        {
            _popup.PopupEntity(Loc.GetString("split-personality-swap-pending"), requester, requester);
            return;
        }

        var other = requester == host ? comp.Passenger : host;
        if (other == null || !TryGetSession(other.Value, out var session))
        {
            _popup.PopupEntity(Loc.GetString("split-personality-swap-unavailable"), requester, requester);
            return;
        }

        comp.PendingRequester = requester;
        _eui.OpenEui(new SplitPersonalitySwapEui(this, host), session);
        _popup.PopupEntity(Loc.GetString("split-personality-swap-requested"), requester, requester);
    }

    private bool TrySwap(EntityUid host, SplitPersonalityComponent comp)
    {
        if (comp.Passenger is not { } oldPassenger
            || TerminatingOrDeleted(host)
            || !_mind.TryGetMind(host, out var bodyMind, out var bodyMindComp)
            || !_mind.TryGetMind(oldPassenger, out var innerMind, out var innerMindComp))
            return false;

        var newPassenger = SpawnPassenger(host, PassengerPrototype);
        _mind.TransferTo(bodyMind, newPassenger, mind: bodyMindComp);
        _mind.TransferTo(innerMind, host, ghostCheckOverride: true, mind: innerMindComp);

        comp.Passenger = newPassenger;
        QueueDel(oldPassenger);

        _popup.PopupEntity(Loc.GetString("split-personality-swap-surfaced"), host, host, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("split-personality-swap-receded"), newPassenger, newPassenger);
        return true;
    }

    /// <summary>
    /// Возвращает тело изначальной личности без её согласия: при лечении, подавлении лекарством или смерти.
    /// </summary>
    private void ReturnControl(EntityUid host, SplitPersonalityComponent comp)
    {
        if (comp.OriginalMind is not { } original
            || comp.Passenger is not { } passenger
            || !_mind.TryGetMind(passenger, out var innerMind, out _)
            || innerMind != original)
            return;

        TrySwap(host, comp);
    }

    private void OnHostStateChanged(Entity<SplitPersonalityComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            ReturnControl(ent, ent.Comp);
    }

    private void OnShutdown(Entity<SplitPersonalityComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ReturnControl(ent, ent.Comp);

        if (ent.Comp.Passenger is { } passenger && !TerminatingOrDeleted(passenger))
        {
            if (_mind.TryGetMind(passenger, out var mind, out var mindComp))
            {
                _ghost.SpawnGhost((mind, mindComp), ent.Owner);
                _popup.PopupEntity(Loc.GetString("split-personality-faded"), ent, ent, PopupType.Medium);
            }

            QueueDel(passenger);
        }

        _actions.RemoveAction(ent.Comp.SwapAction);

        if (!TryComp<CollectiveMindComponent>(ent, out var collective))
            return;

        if (ent.Comp.AddedCollectiveMind)
        {
            RemComp(ent, collective);
            return;
        }

        collective.Channels.Remove(InnerVoice);
        UpdateCollectiveMind(ent, collective);
    }

    private void OnPassengerMindAdded(Entity<SplitPersonalityPassengerComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<SplitPersonalityComponent>(ent.Comp.Host, out var comp)
            || comp.AlterMind != null
            || args.Mind.Owner == comp.OriginalMind)
            return;

        comp.AlterMind = args.Mind.Owner;
        _popup.PopupEntity(Loc.GetString("split-personality-alter-awakened"), ent.Comp.Host, ent.Comp.Host, PopupType.LargeCaution);
    }

    private void OnPassengerSpeakAttempt(Entity<SplitPersonalityPassengerComponent> ent, ref SpeakAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPassengerEmoteAttempt(Entity<SplitPersonalityPassengerComponent> ent, ref EmoteAttemptEvent args)
    {
        args.Cancel();
    }

    private EntityUid SpawnPassenger(EntityUid host, EntProtoId prototype)
    {
        var passenger = Spawn(prototype, Transform(host).Coordinates);
        _transform.SetParent(passenger, host);
        EnsureComp<SplitPersonalityPassengerComponent>(passenger).Host = host;

        var collective = EnsureComp<CollectiveMindComponent>(passenger);
        collective.Channels.Add(InnerVoice);
        UpdateCollectiveMind(passenger, collective);
        return passenger;
    }

    private void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        _collectiveMind.UpdateCollectiveMind(uid, collective);
        Dirty(uid, collective);
    }

    private bool TryGetSession(EntityUid uid, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return _mind.TryGetMind(uid, out _, out var mind)
            && mind.UserId is { } userId
            && _player.TryGetSessionById(userId, out session);
    }
}
