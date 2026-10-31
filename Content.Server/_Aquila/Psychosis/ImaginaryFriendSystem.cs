using Content.Server._Aquila.Psychosis.Components;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Shared.Emoting;
using Content.Shared.Eye;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Psychosis;

/// <summary>
/// Воображаемый друг: призрачный спутник, которого видит и слышит только его хозяин.
/// </summary>
public sealed class ImaginaryFriendSystem : EntitySystem
{
    private static readonly EntProtoId FriendPrototype = "ImaginaryFriend";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimTime = TimeSpan.FromMinutes(5);

    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PsychosisMedicineSystem _medicine = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
        SubscribeLocalEvent<ImaginaryFriendComponent, SpeakAttemptEvent>(OnFriendSpeakAttempt);
        SubscribeLocalEvent<ImaginaryFriendComponent, EmoteAttemptEvent>(OnFriendEmoteAttempt);
        SubscribeLocalEvent<ImaginaryFriendComponent, MindAddedMessage>(OnFriendMindAdded);
        SubscribeLocalEvent<ImaginaryFriendComponent, ComponentShutdown>(OnFriendShutdown);
        SubscribeLocalEvent<ImaginaryFriendOwnerComponent, ComponentShutdown>(OnOwnerShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ImaginaryFriendOwnerComponent>();
        while (query.MoveNext(out var uid, out var owner))
        {
            if (now < owner.NextCheck)
                continue;

            owner.NextCheck = now + CheckInterval;

            if (owner.Friend is not { } friend || !TryComp<ImaginaryFriendComponent>(friend, out var friendComp))
            {
                RemCompDeferred<ImaginaryFriendOwnerComponent>(uid);
                continue;
            }

            var claimed = _mind.TryGetMind(friend, out _, out _);
            if (!claimed && now > owner.ClaimDeadline)
            {
                RemCompDeferred<ImaginaryFriendOwnerComponent>(uid);
                continue;
            }

            _medicine.Check(uid, out var suppressed, out var curing);
            friendComp.Suppressed = suppressed;
            UpdateShownTo((friend, friendComp), claimed && !suppressed ? GetSession(uid) : null);
            KeepNearOwner(uid, (friend, friendComp));

            if (!curing)
                continue;

            owner.CureProgress += (float) CheckInterval.TotalSeconds;
            if (owner.CureProgress < owner.CureTime)
                continue;

            _popup.PopupEntity(Loc.GetString("imaginary-friend-cured"), uid, uid, PopupType.Medium);
            RemCompDeferred<ImaginaryFriendOwnerComponent>(uid);
        }
    }

    /// <summary>
    /// Даёт существу воображаемого друга. Друга займёт призрак.
    /// </summary>
    public bool TryAfflict(EntityUid owner)
    {
        if (HasComp<ImaginaryFriendOwnerComponent>(owner))
            return false;

        var friend = Spawn(FriendPrototype, Transform(owner).Coordinates);
        var friendComp = EnsureComp<ImaginaryFriendComponent>(friend);
        friendComp.Owner = owner;

        var visibility = EnsureComp<VisibilityComponent>(friend);
        _visibility.SetLayer((friend, visibility), (ushort) VisibilityFlags.ImaginaryFriend);

        var ownerComp = AddComp<ImaginaryFriendOwnerComponent>(owner);
        ownerComp.Friend = friend;
        ownerComp.ClaimDeadline = _timing.CurTime + ClaimTime;
        ownerComp.NextCheck = _timing.CurTime + CheckInterval;
        return true;
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (!TryComp<ImaginaryFriendComponent>(ev.Source, out var friend))
            return;

        var remove = new List<ICommonSession>();
        foreach (var session in ev.Recipients.Keys)
        {
            if (session.AttachedEntity is not { } attached
                || attached != ev.Source && (attached != friend.Owner || friend.Suppressed))
                remove.Add(session);
        }

        foreach (var session in remove)
        {
            ev.Recipients.Remove(session);
        }
    }

    private void OnFriendSpeakAttempt(Entity<ImaginaryFriendComponent> ent, ref SpeakAttemptEvent args)
    {
        if (ent.Comp.Suppressed)
            args.Cancel();
    }

    private void OnFriendEmoteAttempt(Entity<ImaginaryFriendComponent> ent, ref EmoteAttemptEvent args)
    {
        if (ent.Comp.Suppressed)
            args.Cancel();
    }

    private void OnFriendMindAdded(Entity<ImaginaryFriendComponent> ent, ref MindAddedMessage args)
    {
        if (TerminatingOrDeleted(ent.Comp.Owner))
            return;

        _transform.SetCoordinates(ent, Transform(ent.Comp.Owner).Coordinates);
        _medicine.Check(ent.Comp.Owner, out var suppressed, out _);
        ent.Comp.Suppressed = suppressed;
        UpdateShownTo(ent, suppressed ? null : GetSession(ent.Comp.Owner));
        _popup.PopupEntity(Loc.GetString("imaginary-friend-appeared"), ent.Comp.Owner, ent.Comp.Owner, PopupType.Medium);
    }

    private void OnFriendShutdown(Entity<ImaginaryFriendComponent> ent, ref ComponentShutdown args)
    {
        UpdateShownTo(ent, null);
    }

    private void OnOwnerShutdown(Entity<ImaginaryFriendOwnerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Friend is not { } friend || TerminatingOrDeleted(friend))
            return;

        if (_mind.TryGetMind(friend, out var mind, out var mindComp))
            _ghost.SpawnGhost((mind, mindComp), friend);

        QueueDel(friend);
    }

    /// <summary>
    /// Отправляет друга только сессии хозяина: остальным игрокам он не приходит вовсе.
    /// </summary>
    private void UpdateShownTo(Entity<ImaginaryFriendComponent> friend, ICommonSession? session)
    {
        if (friend.Comp.ShownTo == session)
            return;

        if (friend.Comp.ShownTo != null)
            _pvs.RemoveForceSend(friend, friend.Comp.ShownTo);

        friend.Comp.ShownTo = session;

        if (session != null)
            _pvs.AddForceSend(friend, session);
    }

    private void KeepNearOwner(EntityUid owner, Entity<ImaginaryFriendComponent> friend)
    {
        var ownerXform = Transform(owner);
        var friendXform = Transform(friend);

        if (ownerXform.MapID == friendXform.MapID
            && ownerXform.Coordinates.TryDistance(EntityManager, _transform, friendXform.Coordinates, out var distance)
            && distance <= friend.Comp.MaxDistance)
            return;

        _transform.SetCoordinates(friend, ownerXform.Coordinates);
    }

    private ICommonSession? GetSession(EntityUid uid)
    {
        return TryComp<ActorComponent>(uid, out var actor) ? actor.PlayerSession : null;
    }
}
