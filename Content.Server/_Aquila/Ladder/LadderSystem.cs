using System.Diagnostics.CodeAnalysis;
using Content.Shared._Aquila.Ladder;
using Content.Shared._Aquila.Ladder.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Aquila.Ladder;

/// <summary>
/// Переносит существо к связанной лестнице после ожидания и выбирает выход среди лестниц с тем же ключом.
/// </summary>
public sealed class LadderSystem : SharedLadderSystem
{
    private static readonly LocId NoExitMessage = "ladder-climb-no-exit";

    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderComponent, LadderClimbDoAfterEvent>(OnClimbDoAfter);
        SubscribeLocalEvent<LadderComponent, StepTriggeredOffEvent>(OnStepTriggeredOff);
    }

    private void OnClimbDoAfter(Entity<LadderComponent> ent, ref LadderClimbDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryClimb(ent.AsNullable(), args.User);
    }

    private void OnStepTriggeredOff(Entity<LadderComponent> ent, ref StepTriggeredOffEvent args)
    {
        ClearArrival(ent.Owner, args.Tripper);
    }

    /// <summary>
    /// Сразу переносит существо по лестнице, если оно всё ещё может и стоит рядом. Возвращает true при переносе.
    /// </summary>
    public bool TryClimb(Entity<LadderComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanClimb(ent, user))
            return false;

        if (!_transform.InRange(user, ent.Owner, ent.Comp.Range))
            return false;

        if (!TryGetExit((ent.Owner, ent.Comp), out var exit))
        {
            Popup.PopupEntity(Loc.GetString(NoExitMessage), ent.Owner, user);
            return false;
        }

        DoClimb((ent.Owner, ent.Comp), exit.Value, user);
        return true;
    }

    /// <summary>
    /// Ищет работающую лестницу с тем же ключом связи. Если таких несколько, выбирает случайную.
    /// </summary>
    public bool TryGetExit(Entity<LadderComponent?> ent, [NotNullWhen(true)] out Entity<LadderComponent>? exit)
    {
        exit = null;

        if (!Resolve(ent, ref ent.Comp, false) || string.IsNullOrEmpty(ent.Comp.LinkId))
            return false;

        var candidates = new List<Entity<LadderComponent>>();
        var query = EntityQueryEnumerator<LadderComponent>();
        while (query.MoveNext(out var uid, out var ladder))
        {
            if (uid == ent.Owner || !ladder.Enabled || ladder.LinkId != ent.Comp.LinkId)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            candidates.Add((uid, ladder));
        }

        if (candidates.Count == 0)
            return false;

        exit = _random.Pick(candidates);
        return true;
    }

    private void DoClimb(Entity<LadderComponent> ent, Entity<LadderComponent> exit, EntityUid user)
    {
        EntityUid? pulled = null;
        if (ent.Comp.CarryPulled
            && TryComp<PullerComponent>(user, out var puller)
            && puller.Pulling is { } pulling
            && TryComp<PullableComponent>(pulling, out var pullable))
        {
            Pulling.TryStopPull(pulling, pullable, ignoreGrab: true);
            pulled = pulling;
        }

        _audio.PlayPvs(ent.Comp.ClimbSound, ent.Owner);

        var destination = Transform(exit.Owner).Coordinates;
        MarkArrival(exit, user);
        _transform.SetCoordinates(user, destination);

        if (pulled is { } carried)
        {
            MarkArrival(exit, carried);
            _transform.SetCoordinates(carried, destination);
            Pulling.TryStartPull(user, carried);
        }

        _audio.PlayPvs(exit.Comp.ClimbSound, exit.Owner);

        _adminLog.Add(LogType.Teleport,
            LogImpact.Low,
            $"{ToPrettyString(user):user} climbed {ToPrettyString(ent.Owner):ladder} to {ToPrettyString(exit.Owner):exit}");
    }
}
