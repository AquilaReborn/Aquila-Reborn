// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared._Kakila.ShuttleLink;
using Content.Shared._Kakila.Sot;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Kakila.Sot;

/// <summary>
///     Голосование экипажа за готовность к битве через песочные часы.
///
///     Фазы: Idle (голосуют за готовность) -> Countdown (решающий голос получен, 15 секунд до вылета,
///     голосуют за отмену) -> Waiting (корабль в FTL и ждёт sotbattlestart, голосуют за отмену, при
///     достаточном числе голосов корабль возвращается на прежнюю позицию).
///     Голосовать могут только живые игроки, стоящие на том же гриде, что и часы.
/// </summary>
public sealed class SotHourglassSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SotRoundSystem _round = default!;
    [Dependency] private readonly SotRuleSystem _rule = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);
    private TimeSpan _nextUpdate;

    /// <summary>Позиция и поворот шаттла в момент вылета - чтобы вернуть его при отмене.</summary>
    private readonly Dictionary<EntityUid, (Vector2 Position, Angle Rotation)> _departPositions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotHourglassComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<SotHourglassComponent, SotHourglassVoteMessage>(OnVote);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        var query = EntityQueryEnumerator<SotHourglassComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var phase = GetPhase(uid, comp, out _, out var id);

            // Время вылета.
            if (phase == SotHourglassPhase.Countdown && comp.DepartAt is { } at && now >= at && id != null)
            {
                Depart(uid, comp, id);
                continue;
            }

            // Пересчёт голосов: игрок мог погибнуть, уйти с корабля или отключиться.
            Evaluate(uid, comp);
        }
    }

    #region UI

    private void OnUiOpened(EntityUid uid, SotHourglassComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, comp, force: true);
    }

    private void OnVote(EntityUid uid, SotHourglassComponent comp, SotHourglassVoteMessage args)
    {
        var actor = args.Actor;
        var phase = GetPhase(uid, comp, out var grid, out _);

        if (grid == null || phase is not (SotHourglassPhase.Idle or SotHourglassPhase.Countdown or SotHourglassPhase.Waiting))
        {
            _popup.PopupEntity(Loc.GetString("sot-hourglass-popup-unavailable"), uid, actor);
            UpdateUi(uid, comp, force: true);
            return;
        }

        if (!IsValidVoter(actor, grid.Value))
        {
            _popup.PopupEntity(Loc.GetString("sot-hourglass-popup-cannot-vote"), uid, actor);
            return;
        }

        // Если фаза уже сменилась, старые голоса не считаются.
        SyncPhase(comp, phase);

        // Повторное нажатие снимает голос.
        if (!comp.Voters.Add(actor))
            comp.Voters.Remove(actor);

        Evaluate(uid, comp);
    }

    private void UpdateUi(EntityUid uid, SotHourglassComponent comp, bool force = false)
    {
        var phase = GetPhase(uid, comp, out var grid, out var id);

        if (grid != null)
            comp.Voters.RemoveWhere(v => !IsValidVoter(v, grid.Value));

        var votersSum = 0;
        foreach (var voter in comp.Voters)
            votersSum += voter.GetHashCode();

        var hash = HashCode.Combine(phase, Required(comp), comp.DepartAt, comp.Voters.Count, votersSum, id);
        if (!force && hash == comp.LastUiHash)
            return;

        comp.LastUiHash = hash;

        var voters = new List<NetEntity>(comp.Voters.Count);
        foreach (var voter in comp.Voters)
            voters.Add(GetNetEntity(voter));

        _ui.SetUiState(uid, SotHourglassUiKey.Key, new SotHourglassBuiState
        {
            Side = comp.Side,
            ShipId = id ?? string.Empty,
            Phase = phase,
            Votes = comp.Voters.Count,
            Required = Required(comp),
            Voters = voters,
            DepartAt = comp.DepartAt,
            DepartSeconds = (int) comp.DepartDelay.TotalSeconds
        });
    }

    #endregion

    #region Логика голосования

    private static int Required(SotHourglassComponent comp) => Math.Max(1, comp.RequiredVotes);

    /// <summary>Определяет фазу по состоянию корабля. Фаза не хранится, а считается каждый раз.</summary>
    private SotHourglassPhase GetPhase(EntityUid uid, SotHourglassComponent comp, out EntityUid? grid, out string? id)
    {
        grid = Transform(uid).GridUid;
        id = TryComp<ShuttleLinkComponent>(uid, out var link) ? link.Id : null;

        if (grid == null || id == null)
            return SotHourglassPhase.Unavailable;

        if (!_round.CheckPreset(out _))
            return SotHourglassPhase.Unavailable;

        if (_rule.IsInActiveBattle(id))
            return SotHourglassPhase.Battle;

        // Шаттл уже отправлен и ждёт старта битвы: локальная проверка вместо _round.IsHeld.
        if (IsHeld(grid.Value, comp))
            return SotHourglassPhase.Waiting;

        if (HasComp<FTLComponent>(grid.Value))
            return SotHourglassPhase.FtlBusy;

        return comp.DepartAt != null ? SotHourglassPhase.Countdown : SotHourglassPhase.Idle;
    }

    /// <summary>
    ///     Шаттл считается "удержанным" (отправлен и ждёт sotbattlestart), если у его грида есть FTLComponent
    ///     и часы уже переведены в фазу Waiting. Это заменяет отсутствующий SotRoundSystem.IsHeld.
    /// </summary>
    private bool IsHeld(EntityUid grid, SotHourglassComponent comp)
    {
        return comp.LastPhase == SotHourglassPhase.Waiting && HasComp<FTLComponent>(grid);
    }

    private bool IsValidVoter(EntityUid voter, EntityUid grid)
    {
        return Exists(voter)
               && !TerminatingOrDeleted(voter)
               && HasComp<ActorComponent>(voter) // подключённый игрок
               && _mobState.IsAlive(voter)
               && Transform(voter).GridUid == grid;
    }

    /// <summary>При смене фазы голоса обнуляются: голоса за готовность не годятся для отмены.</summary>
    private static void SyncPhase(SotHourglassComponent comp, SotHourglassPhase phase)
    {
        if (comp.LastPhase == phase)
            return;

        comp.LastPhase = phase;
        comp.Voters.Clear();

        // Таймер вылета имеет смысл только в фазе Countdown.
        if (phase != SotHourglassPhase.Countdown)
            comp.DepartAt = null;
    }

    /// <summary>Пересчитывает голоса, при достаточном числе запускает действие фазы и обновляет UI.</summary>
    private void Evaluate(EntityUid uid, SotHourglassComponent comp)
    {
        var phase = GetPhase(uid, comp, out var grid, out _);
        SyncPhase(comp, phase);

        if (grid != null)
        {
            comp.Voters.RemoveWhere(v => !IsValidVoter(v, grid.Value));

            if (comp.Voters.Count >= Required(comp))
            {
                switch (phase)
                {
                    case SotHourglassPhase.Idle:
                        StartCountdown(uid, comp);
                        break;
                    case SotHourglassPhase.Countdown:
                        CancelCountdown(uid, comp);
                        break;
                    case SotHourglassPhase.Waiting:
                        CancelReady(uid, comp, grid.Value);
                        break;
                }
            }
        }

        UpdateUi(uid, comp);
    }

    private void StartCountdown(EntityUid uid, SotHourglassComponent comp)
    {
        comp.DepartAt = _timing.CurTime + comp.DepartDelay;
        comp.Voters.Clear();
        comp.LastPhase = SotHourglassPhase.Countdown;

        Say(uid, Loc.GetString("sot-hourglass-say-countdown", ("seconds", (int) comp.DepartDelay.TotalSeconds)));
    }

    private void CancelCountdown(EntityUid uid, SotHourglassComponent comp)
    {
        comp.DepartAt = null;
        comp.Voters.Clear();
        comp.LastPhase = SotHourglassPhase.Idle;

        Say(uid, Loc.GetString("sot-hourglass-say-countdown-cancelled"));
    }

    /// <summary>Время вышло: отправляет шаттл в FTL (так же, как команда sotbattleprepare).</summary>
    private void Depart(EntityUid uid, SotHourglassComponent comp, string id)
    {
        comp.DepartAt = null;
        comp.Voters.Clear();

        if (_round.TryPrepare(id, out var error))
        {
            // Запоминаем позицию до FTL, чтобы вернуть шаттл при отмене.
            var grid = Transform(uid).GridUid;
            if (grid != null)
            {
                _departPositions[grid.Value] = (
                    _transform.GetWorldPosition(grid.Value),
                    _transform.GetWorldRotation(grid.Value));
            }

            comp.LastPhase = SotHourglassPhase.Waiting;
            Say(uid, Loc.GetString("sot-hourglass-say-departing"));
        }
        else
        {
            comp.LastPhase = SotHourglassPhase.Idle;
            Say(uid, Loc.GetString("sot-hourglass-say-depart-failed", ("error", error)));
        }

        UpdateUi(uid, comp, force: true);
    }

    /// <summary>Отмена после вылета: шаттл возвращается на ту позицию, откуда улетел.</summary>
    private void CancelReady(EntityUid uid, SotHourglassComponent comp, EntityUid grid)
    {
        comp.Voters.Clear();
        comp.LastPhase = SotHourglassPhase.FtlBusy;

        // Прямой возврат шаттла вместо отсутствующего _round.RequestReturn.
        if (_departPositions.TryGetValue(grid, out var depart))
        {
            _transform.SetWorldPosition(grid, depart.Position);
            _transform.SetWorldRotation(grid, depart.Rotation);
            _departPositions.Remove(grid);
        }

        if (HasComp<FTLComponent>(grid))
            RemComp<FTLComponent>(grid);

        Say(uid, Loc.GetString("sot-hourglass-say-return"));
    }

    /// <summary>Часы говорят в местный чат.</summary>
    private void Say(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(
            uid,
            message,
            InGameICChatType.Speak,
            hideChat: false,
            hideLog: true,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    #endregion
}
