using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Shared._Aquila.Changeling;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Polymorph.Systems;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Emoting;
using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Changeling;

public sealed class ChangelingHostControlSystem : EntitySystem
{
    private static readonly EntProtoId PassengerPrototype = "ChangelingPassenger";
    private static readonly ProtoId<CollectiveMindPrototype> InnerVoiceChannel = "ChangelingInnerVoice";

    [Dependency] private readonly CollectiveMindUpdateSystem _collectiveMind = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _pendingRequests = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingAbsorbedEvent>(OnAbsorbed);
        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingOfferHostControlActionEvent>(OnOfferControl);
        SubscribeLocalEvent<ChangelingVictimMindsComponent, PolymorphedEvent>(OnVictimMindsPolymorphed);
        SubscribeLocalEvent<ChangelingPassengerComponent, ChangelingReturnHostControlActionEvent>(OnTakeControl);
        SubscribeLocalEvent<ChangelingPassengerComponent, SpeakAttemptEvent>(OnPassengerSpeakAttempt);
        SubscribeLocalEvent<ChangelingPassengerComponent, EmoteAttemptEvent>(OnPassengerEmoteAttempt);
        SubscribeLocalEvent<ChangelingHostControlledComponent, MobStateChangedEvent>(OnHostStateChanged);
    }

    private void OnAbsorbed(Entity<ChangelingIdentityComponent> ent, ref ChangelingAbsorbedEvent args)
    {
        if (!TryComp<DnaComponent>(args.Target, out var dna)
            || dna.DNA is not { } dnaString
            || !TryGetVictimMind(args.Target, out var victimMind))
            return;

        var victims = EnsureComp<ChangelingVictimMindsComponent>(ent);
        victims.Minds[dnaString] = victimMind.Value;
    }

    private void OnVictimMindsPolymorphed(Entity<ChangelingVictimMindsComponent> ent, ref PolymorphedEvent args)
    {
        _polymorph.CopyPolymorphComponent<ChangelingVictimMindsComponent>(ent, args.NewEntity);
    }

    private void OnOfferControl(Entity<ChangelingIdentityComponent> ent, ref ChangelingOfferHostControlActionEvent args)
    {
        if (args.Handled)
            return;

        if (_pendingRequests.Contains(ent))
        {
            _popup.PopupEntity(Loc.GetString("changeling-host-control-pending"), ent, ent);
            return;
        }

        if (!TryGetFormVictim(ent, out var victimMind))
        {
            _popup.PopupEntity(Loc.GetString("changeling-host-control-not-victim"), ent, ent);
            return;
        }

        if (!CanTakeBody(victimMind.Value, out var session))
        {
            _popup.PopupEntity(Loc.GetString("changeling-host-control-unavailable"), ent, ent);
            return;
        }

        _pendingRequests.Add(ent);
        _euiManager.OpenEui(new ChangelingHostControlEui(this, ent, victimMind.Value), session);
        _popup.PopupEntity(Loc.GetString("changeling-host-control-offered"), ent, ent);

        args.Handled = true;
    }

    /// <summary>
    /// Обрабатывает ответ жертвы на предложение генокрада взять управление телом.
    /// </summary>
    public void AnswerRequest(EntityUid host, EntityUid victimMind, bool accepted)
    {
        _pendingRequests.Remove(host);

        if (TerminatingOrDeleted(host))
            return;

        if (!accepted)
        {
            _popup.PopupEntity(Loc.GetString("changeling-host-control-denied"), host, host);
            return;
        }

        if (!TryGiveControl(host, victimMind))
            _popup.PopupEntity(Loc.GetString("changeling-host-control-unavailable"), host, host);
    }

    private bool TryGiveControl(EntityUid host, EntityUid victimMind)
    {
        if (HasComp<ChangelingHostControlledComponent>(host)
            || HasComp<ChangelingTrueFormComponent>(host)
            || _mobState.IsDead(host)
            || !TryGetFormVictim(host, out var formVictim)
            || formVictim != victimMind
            || !CanTakeBody(victimMind, out _)
            || !_mind.TryGetMind(host, out var lingMind, out var lingMindComp))
            return false;

        var passenger = Spawn(PassengerPrototype, Transform(host).Coordinates);
        _transform.SetParent(passenger, host);
        EnsureComp<ChangelingPassengerComponent>(passenger).Host = host;

        var control = EnsureComp<ChangelingHostControlledComponent>(host);
        control.Passenger = passenger;
        control.VictimMind = victimMind;
        Dirty(host, control);

        HideChangelingActions(host, passenger, control);
        SetupInnerVoice(host, passenger, control);

        _mind.TransferTo(lingMind, passenger, mind: lingMindComp);
        _mind.TransferTo(victimMind, host, ghostCheckOverride: true);

        _popup.PopupEntity(Loc.GetString("changeling-host-control-given-victim"), host, host, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("changeling-host-control-given-changeling"), passenger, passenger);
        return true;
    }

    private void OnTakeControl(Entity<ChangelingPassengerComponent> ent, ref ChangelingReturnHostControlActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ReturnControl(ent.Comp.Host);
    }

    private void OnPassengerSpeakAttempt(Entity<ChangelingPassengerComponent> ent, ref SpeakAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPassengerEmoteAttempt(Entity<ChangelingPassengerComponent> ent, ref EmoteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnHostStateChanged(Entity<ChangelingHostControlledComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        ReturnControl(ent);
    }

    private void ReturnControl(EntityUid host)
    {
        if (!TryComp<ChangelingHostControlledComponent>(host, out var control))
            return;

        var passenger = control.Passenger;
        var victimMind = control.VictimMind;
        var hiddenActions = control.HiddenActions;
        var hiddenChannels = control.HiddenChannels;
        RemComp<ChangelingHostControlledComponent>(host);

        RestoreChangelingActions(host, hiddenActions);
        RestoreChannels(host, hiddenChannels);

        if (victimMind != null
            && _mind.TryGetMind(host, out var currentMind, out var currentMindComp)
            && currentMind == victimMind)
        {
            _ghost.SpawnGhost((currentMind, currentMindComp), host);
        }

        if (passenger == null)
            return;

        if (_mind.TryGetMind(passenger.Value, out var lingMind, out var lingMindComp))
        {
            _mind.TransferTo(lingMind, host, mind: lingMindComp);
            _popup.PopupEntity(Loc.GetString("changeling-host-control-taken"), host, host, PopupType.LargeCaution);
        }

        QueueDel(passenger);
    }

    private void HideChangelingActions(EntityUid host, EntityUid passenger, ChangelingHostControlledComponent control)
    {
        foreach (var action in _actions.GetActions(host).ToList())
        {
            if (!HasComp<ChangelingActionComponent>(action))
                continue;

            if (_actions.AddActionDirect(passenger, new Entity<ActionComponent?>(action.Owner, action.Comp)))
                control.HiddenActions.Add(action.Owner);
        }
    }

    private void RestoreChangelingActions(EntityUid host, List<EntityUid> actions)
    {
        foreach (var action in actions)
        {
            if (TerminatingOrDeleted(action))
                continue;

            _actions.AddActionDirect(host, new Entity<ActionComponent?>(action, null));
        }
    }

    private void SetupInnerVoice(EntityUid host, EntityUid passenger, ChangelingHostControlledComponent control)
    {
        var passengerMind = EnsureComp<CollectiveMindComponent>(passenger);

        if (TryComp<CollectiveMindComponent>(host, out var hostMind))
        {
            control.HiddenChannels = new(hostMind.Channels);
            passengerMind.Channels.UnionWith(hostMind.Channels);
            hostMind.Channels.Clear();
        }
        else
        {
            hostMind = AddComp<CollectiveMindComponent>(host);
        }

        hostMind.Channels.Add(InnerVoiceChannel);
        passengerMind.Channels.Add(InnerVoiceChannel);

        UpdateCollectiveMind(host, hostMind);
        UpdateCollectiveMind(passenger, passengerMind);
    }

    private void RestoreChannels(EntityUid host, HashSet<ProtoId<CollectiveMindPrototype>>? channels)
    {
        if (channels == null)
        {
            RemComp<CollectiveMindComponent>(host);
            return;
        }

        if (!TryComp<CollectiveMindComponent>(host, out var hostMind))
            return;

        hostMind.Channels.Clear();
        hostMind.Channels.UnionWith(channels);
        UpdateCollectiveMind(host, hostMind);
    }

    private void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        _collectiveMind.UpdateCollectiveMind(uid, collective);
        Dirty(uid, collective);
    }

    private bool TryGetFormVictim(EntityUid host, [NotNullWhen(true)] out EntityUid? victimMind)
    {
        victimMind = null;
        if (!TryComp<ChangelingVictimMindsComponent>(host, out var victims)
            || !TryComp<DnaComponent>(host, out var dna)
            || dna.DNA is not { } dnaString
            || !victims.Minds.TryGetValue(dnaString, out var mind))
            return false;

        victimMind = mind;
        return true;
    }

    private bool CanTakeBody(EntityUid victimMind, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        if (!TryComp<MindComponent>(victimMind, out var mind)
            || mind.UserId is not { } userId
            || !_player.TryGetSessionById(userId, out session))
            return false;

        return mind.CurrentEntity is not { } current || HasComp<GhostComponent>(current);
    }

    private bool TryGetVictimMind(EntityUid target, [NotNullWhen(true)] out EntityUid? mindId)
    {
        if (_mind.TryGetMind(target, out var foundMind, out _))
        {
            mindId = foundMind;
            return true;
        }

        var netTarget = GetNetEntity(target);
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var uid, out var mind))
        {
            if (mind.OriginalOwnedEntity != netTarget)
                continue;

            mindId = uid;
            return true;
        }

        mindId = null;
        return false;
    }
}
