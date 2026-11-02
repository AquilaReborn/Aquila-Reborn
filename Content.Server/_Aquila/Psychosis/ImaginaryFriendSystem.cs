using System.Linq;
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
/// Воображаемые друзья и энграммы: призрачные спутники, которых видит и слышит только их хозяин.
/// Спутники одного хозяина смотрят его глазами, поэтому видят и слышат друг друга.
/// </summary>
public sealed class ImaginaryFriendSystem : EntitySystem
{
    private static readonly EntProtoId FriendPrototype = "ImaginaryFriend";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimTime = TimeSpan.FromMinutes(5);
    private static readonly LocId CuredMessage = "imaginary-friend-cured";

    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PsychosisMedicineSystem _medicine = default!;
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
        SubscribeLocalEvent<ImaginaryFriendComponent, GetVisMaskEvent>(OnFriendGetVisMask);
        SubscribeLocalEvent<ImaginaryFriendComponent, ExpandPvsEvent>(OnFriendExpandPvs);
        SubscribeLocalEvent<ImaginaryFriendOwnerComponent, ComponentShutdown>(OnOwnerShutdown);
        SubscribeLocalEvent<ImaginaryFriendOwnerComponent, ExpandPvsEvent>(OnOwnerExpandPvs);
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

            _medicine.Check(uid, out var suppressed, out var curing);
            var session = GetSession(uid);
            var hasTreatable = false;

            foreach (var friend in owner.Friends.ToArray())
            {
                if (!TryComp<ImaginaryFriendComponent>(friend, out var friendComp))
                {
                    owner.Friends.Remove(friend);
                    continue;
                }

                var claimed = _mind.TryGetMind(friend, out _, out _);
                if (!claimed && now > friendComp.ClaimDeadline)
                {
                    RemoveFriend((uid, owner), friend);
                    continue;
                }

                hasTreatable |= friendComp.AffectedByMedicine;

                var friendSuppressed = friendComp.AffectedByMedicine && suppressed;
                friendComp.Suppressed = friendSuppressed;
                friendComp.ShownTo = claimed && !friendSuppressed ? session : null;
                KeepNearOwner(uid, (friend, friendComp));
                SyncVision(uid, friend);
            }

            if (owner.Friends.Count == 0)
            {
                RemCompDeferred<ImaginaryFriendOwnerComponent>(uid);
                continue;
            }

            if (!curing || !hasTreatable)
                continue;

            owner.CureProgress += (float) CheckInterval.TotalSeconds;
            if (owner.CureProgress < owner.CureTime)
                continue;

            owner.CureProgress = 0f;
            _popup.PopupEntity(Loc.GetString(CuredMessage), uid, uid, PopupType.Medium);
            RemoveTreatable((uid, owner));
        }
    }

    /// <summary>
    /// Даёт существу воображаемого друга, если его ещё нет. Друга займёт призрак.
    /// </summary>
    public bool TryAfflict(EntityUid owner)
    {
        if (HasImaginaryFriend(owner))
            return false;

        var friend = Spawn(FriendPrototype, Transform(owner).Coordinates);
        Attach(owner, friend, true);
        return true;
    }

    /// <summary>
    /// Есть ли у существа воображаемый друг, которого лечат лекарства. Энграммы не считаются.
    /// </summary>
    public bool HasImaginaryFriend(EntityUid owner)
    {
        if (!TryComp<ImaginaryFriendOwnerComponent>(owner, out var ownerComp))
            return false;

        foreach (var friend in ownerComp.Friends)
        {
            if (CompOrNull<ImaginaryFriendComponent>(friend)?.AffectedByMedicine == true)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Убирает у существа всех воображаемых друзей, которых лечат лекарства. Энграммы остаются.
    /// </summary>
    public void Cure(EntityUid owner)
    {
        if (TryComp<ImaginaryFriendOwnerComponent>(owner, out var ownerComp))
            RemoveTreatable((owner, ownerComp));
    }

    /// <summary>
    /// Делает сущность невидимой для всех, кроме того, кому её потом покажут.
    /// </summary>
    public void HideFromOthers(EntityUid friend)
    {
        var visibility = EnsureComp<VisibilityComponent>(friend);
        _visibility.SetLayer((friend, visibility), (ushort) VisibilityFlags.ImaginaryFriend);
    }

    /// <summary>
    /// Привязывает готовую сущность к хозяину как спутника: её видит и слышит только хозяин и другие его спутники,
    /// она держится рядом и смотрит его глазами. Если в сущности уже есть разум, она сразу появляется.
    /// </summary>
    public void Attach(EntityUid owner, EntityUid friend, bool affectedByMedicine, LocId? appearedMessage = null)
    {
        var friendComp = EnsureComp<ImaginaryFriendComponent>(friend);
        friendComp.Owner = owner;
        friendComp.AffectedByMedicine = affectedByMedicine;
        friendComp.ClaimDeadline = _timing.CurTime + ClaimTime;
        if (appearedMessage is { } message)
            friendComp.AppearedMessage = message;

        HideFromOthers(friend);
        _eye.SetDrawFov(friend, true);
        SyncVision(owner, friend);

        var ownerComp = EnsureComp<ImaginaryFriendOwnerComponent>(owner);
        ownerComp.Friends.Add(friend);
        ownerComp.NextCheck = _timing.CurTime + CheckInterval;

        if (_mind.TryGetMind(friend, out _, out _))
            Reveal((friend, friendComp));
    }

    /// <summary>
    /// Слышит ли слушатель речь говорящего. Спутника слышат только он сам, его хозяин и другие спутники того же хозяина.
    /// </summary>
    public bool CanHear(EntityUid speaker, EntityUid listener)
    {
        if (listener == speaker || !TryComp<ImaginaryFriendComponent>(speaker, out var friend))
            return true;

        if (friend.Suppressed || friend.Owner is not { } owner)
            return false;

        return listener == owner
            || TryComp<ImaginaryFriendComponent>(listener, out var listenerFriend)
            && listenerFriend.Owner == owner
            && !listenerFriend.Suppressed;
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (!HasComp<ImaginaryFriendComponent>(ev.Source))
            return;

        var remove = new List<ICommonSession>();
        foreach (var session in ev.Recipients.Keys)
        {
            if (session.AttachedEntity is not { } attached
                || !CanHear(ev.Source, attached))
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
        if (ent.Comp.Owner is not { } ownerUid
            || TerminatingOrDeleted(ownerUid)
            || !TryComp<ImaginaryFriendOwnerComponent>(ownerUid, out var owner)
            || !owner.Friends.Contains(ent))
            return;

        Reveal(ent);
    }

    private void Reveal(Entity<ImaginaryFriendComponent> ent)
    {
        if (ent.Comp.Owner is not { } owner)
            return;

        _transform.SetCoordinates(ent, Transform(owner).Coordinates);

        var suppressed = false;
        if (ent.Comp.AffectedByMedicine)
            _medicine.Check(owner, out suppressed, out _);

        ent.Comp.Suppressed = suppressed;
        ent.Comp.ShownTo = suppressed ? null : GetSession(owner);
        _popup.PopupEntity(Loc.GetString(ent.Comp.AppearedMessage), owner, owner, PopupType.Medium);
    }

    private void OnFriendShutdown(Entity<ImaginaryFriendComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.ShownTo = null;

        if (TryComp<ImaginaryFriendOwnerComponent>(ent.Comp.Owner, out var owner))
            owner.Friends.Remove(ent);
    }

    private void OnFriendGetVisMask(Entity<ImaginaryFriendComponent> ent, ref GetVisMaskEvent args)
    {
        if (TryComp<EyeComponent>(ent.Comp.Owner, out var ownerEye))
            args.VisibilityMask |= ownerEye.VisibilityMask;
    }

    private void OnOwnerShutdown(Entity<ImaginaryFriendOwnerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var friend in ent.Comp.Friends.ToArray())
        {
            DeleteFriend(friend);
        }

        ent.Comp.Friends.Clear();
    }

    private void RemoveTreatable(Entity<ImaginaryFriendOwnerComponent> owner)
    {
        foreach (var friend in owner.Comp.Friends.ToArray())
        {
            if (CompOrNull<ImaginaryFriendComponent>(friend)?.AffectedByMedicine == true)
                RemoveFriend(owner, friend);
        }
    }

    private void RemoveFriend(Entity<ImaginaryFriendOwnerComponent> owner, EntityUid friend)
    {
        owner.Comp.Friends.Remove(friend);
        DeleteFriend(friend);
    }

    private void DeleteFriend(EntityUid friend)
    {
        if (TerminatingOrDeleted(friend))
            return;

        if (_mind.TryGetMind(friend, out var mind, out var mindComp))
            _ghost.SpawnGhost((mind, mindComp), friend);

        QueueDel(friend);
    }

    private void OnOwnerExpandPvs(Entity<ImaginaryFriendOwnerComponent> ent, ref ExpandPvsEvent args)
    {
        foreach (var friend in ent.Comp.Friends)
        {
            if (CompOrNull<ImaginaryFriendComponent>(friend)?.ShownTo != args.Session)
                continue;

            args.RecursiveEntities ??= new List<EntityUid>();
            args.RecursiveEntities.Add(friend);
            args.VisMask |= (int) VisibilityFlags.ImaginaryFriend;
        }
    }

    private void OnFriendExpandPvs(Entity<ImaginaryFriendComponent> ent, ref ExpandPvsEvent args)
    {
        args.RecursiveEntities ??= new List<EntityUid>();
        args.RecursiveEntities.Add(ent);
        args.VisMask |= (int) VisibilityFlags.ImaginaryFriend;

        if (!TryComp<ImaginaryFriendOwnerComponent>(ent.Comp.Owner, out var owner))
            return;

        foreach (var other in owner.Friends)
        {
            if (other != ent.Owner && CompOrNull<ImaginaryFriendComponent>(other)?.ShownTo != null)
                args.RecursiveEntities.Add(other);
        }
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

    private void SyncVision(EntityUid owner, EntityUid friend)
    {
        if (!TryComp<EyeComponent>(friend, out var friendEye))
            return;

        if (friendEye.Target != owner)
            _eye.SetTarget(friend, owner, friendEye);

        var ownerMask = TryComp<EyeComponent>(owner, out var ownerEye)
            ? ownerEye.VisibilityMask
            : EyeComponent.DefaultVisibilityMask;

        if (friendEye.VisibilityMask != ownerMask)
            _eye.RefreshVisibilityMask((friend, friendEye));
    }

    private ICommonSession? GetSession(EntityUid uid)
    {
        return TryComp<ActorComponent>(uid, out var actor) ? actor.PlayerSession : null;
    }
}
