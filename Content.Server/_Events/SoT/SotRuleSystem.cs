/// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._Kakila.ShuttleLink;
using Content.Shared._Kakila.Sot;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Kakila.Sot;

/// <summary>
///     Учёт битв SoT: кто с кем воюет, автоматическое завершение битвы при уничтожении часов,
///     принудительное завершение, завершение раунда и итоговая статистика.
/// </summary>
public sealed class SotRuleSystem : GameRuleSystem<SotRuleComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotHourglassComponent, DestructionEventArgs>(OnHourglassDestroyed);
    }

    #region Поиск

    private bool TryGetActiveRule(out Entity<SotRuleComponent> rule)
    {
        var query = EntityQueryEnumerator<SotRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            rule = new Entity<SotRuleComponent>(uid, comp);
            return true;
        }

        rule = default;
        return false;
    }

    /// <summary>Ищет живые песочные часы по ID привязки (часы, которые уже уничтожаются, пропускаются).</summary>
    private bool TryFindHourglass(string id, out EntityUid uid, out SotSide side)
    {
        var query = EntityQueryEnumerator<ShuttleLinkComponent, SotHourglassComponent>();
        while (query.MoveNext(out var ent, out var link, out var hourglass))
        {
            if (link.Id != id || TerminatingOrDeleted(ent))
                continue;

            uid = ent;
            side = hourglass.Side;
            return true;
        }

        uid = default;
        side = default;
        return false;
    }

    private static SotBattle? FindActiveBattle(SotRuleComponent rule, string id)
    {
        foreach (var battle in rule.Battles)
        {
            if (battle.Status == SotBattleStatus.Active && battle.Involves(id))
                return battle;
        }

        return null;
    }

    /// <summary>Участвуют ли часы с этим ID в идущей битве.</summary>
    public bool IsInActiveBattle(string id)
    {
        return TryGetActiveRule(out var rule) && FindActiveBattle(rule.Comp, id) != null;
    }

    #endregion

    #region Начало битвы

    /// <summary>
    ///     Проверяет участников и записывает новую битву. Вызывать до перемещения шаттлов:
    ///     если вернулось false, ничего менять не нужно.
    /// </summary>
    public bool TryBeginBattle(string id1, string id2, out int number, out string error)
    {
        number = 0;
        error = string.Empty;

        if (!TryGetActiveRule(out var rule))
        {
            error = "Правило пресета SoT не активно. Запустите раунд с пресетом SoT.";
            return false;
        }

        if (GameTicker.RunLevel != GameRunLevel.InRound)
        {
            error = "Раунд не идёт.";
            return false;
        }

        if (!TryFindHourglass(id1, out _, out var side1))
        {
            error = $"Песочные часы с ID \"{id1}\" не найдены (или уже уничтожены).";
            return false;
        }

        if (!TryFindHourglass(id2, out _, out var side2))
        {
            error = $"Песочные часы с ID \"{id2}\" не найдены (или уже уничтожены).";
            return false;
        }

        if (rule.Comp.RequireDifferentSides && side1 == side2)
        {
            error = $"Часы \"{id1}\" и \"{id2}\" принадлежат одной стороне ({SideName(side1)}). " +
                    "Битва возможна только между Афиной и Жнецами.";
            return false;
        }

        foreach (var id in new[] { id1, id2 })
        {
            if (FindActiveBattle(rule.Comp, id) is { } active)
            {
                error = $"Часы \"{id}\" уже участвуют в битве №{active.Number}. Завершите её командой sotbattleend.";
                return false;
            }
        }

        var battle = new SotBattle
        {
            Number = rule.Comp.NextBattleNumber++,
            IdA = id1,
            IdB = id2,
            SideA = side1,
            SideB = side2,
            StartTime = GameTicker.RoundDuration()
        };

        rule.Comp.Battles.Add(battle);
        number = battle.Number;
        return true;
    }

    #endregion

    #region Завершение битвы

    private void OnHourglassDestroyed(EntityUid uid, SotHourglassComponent comp, DestructionEventArgs args)
    {
        if (GameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (!TryComp<ShuttleLinkComponent>(uid, out var link))
            return;

        if (!TryGetActiveRule(out var rule))
            return;

        // Часы уничтожены вне битвы - в статистику не попадает.
        if (FindActiveBattle(rule.Comp, link.Id) is not { } battle)
            return;

        // Если оба часа падают одним взрывом, выигрывает тот, чьи часы разрушились позже:
        // первое событие закрывает битву, второе уже не находит активной битвы.
        FinishBattle(rule, battle, battle.OpponentOf(link.Id), SotBattleEndReason.HourglassDestroyed);
    }

    /// <summary>
    ///     Принудительно завершает битву, в которой участвуют часы <paramref name="participantId"/>.
    ///     winnerArg: null - победитель по состоянию часов (больше целой прочности), "draw" - ничья,
    ///     либо ID победителя (один из двух участников).
    /// </summary>
    public bool TryEndBattle(string participantId, string? winnerArg, out SotBattle? battle, out string error)
    {
        battle = null;
        error = string.Empty;

        if (!TryGetActiveRule(out var rule))
        {
            error = "Правило пресета SoT не активно.";
            return false;
        }

        if (FindActiveBattle(rule.Comp, participantId) is not { } found)
        {
            error = $"Идущей битвы с участием \"{participantId}\" нет.";
            return false;
        }

        string? winner;
        if (winnerArg == null)
        {
            winner = DetermineWinnerByHealth(found);
        }
        else if (winnerArg.Equals("draw", StringComparison.OrdinalIgnoreCase))
        {
            winner = null;
        }
        else if (found.Involves(winnerArg))
        {
            winner = winnerArg;
        }
        else
        {
            error = $"Победителем может быть \"{found.IdA}\", \"{found.IdB}\" или draw (ничья).";
            return false;
        }

        FinishBattle(rule, found, winner, SotBattleEndReason.Forced);
        battle = found;
        return true;
    }

    private void FinishBattle(Entity<SotRuleComponent> rule, SotBattle battle, string? winnerId, SotBattleEndReason reason)
    {
        battle.Status = SotBattleStatus.Finished;
        battle.Reason = reason;
        battle.WinnerId = winnerId;
        battle.WinnerSide = winnerId == null ? null : (winnerId == battle.IdA ? battle.SideA : battle.SideB);
        battle.EndTime = GameTicker.RoundDuration();

        var sender = Loc.GetString("sot-announcement-sender");
        var reasonText = Loc.GetString(ReasonKey(reason));

        if (winnerId == null || battle.WinnerSide is not { } winnerSide)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("sot-battle-end-draw", ("number", battle.Number), ("reason", reasonText)),
                sender,
                colorOverride: Color.Gold);
            return;
        }

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("sot-battle-end-win",
                ("number", battle.Number),
                ("side", SideName(winnerSide)),
                ("id", winnerId),
                ("reason", reasonText)),
            sender,
            colorOverride: winnerSide == SotSide.Athena ? Color.Blue : Color.Red);
    }

    /// <summary>Побеждает тот, чьи часы целее. Почти равная прочность - ничья (null).</summary>
    private string? DetermineWinnerByHealth(SotBattle battle)
    {
        var hpA = GetHealthFraction(battle.IdA);
        var hpB = GetHealthFraction(battle.IdB);

        if (hpA == null && hpB == null)
            return null;
        if (hpA == null)
            return battle.IdB;
        if (hpB == null)
            return battle.IdA;

        if (Math.Abs(hpA.Value - hpB.Value) < 0.001f)
            return null;

        return hpA.Value > hpB.Value ? battle.IdA : battle.IdB;
    }

    /// <summary>Доля оставшейся прочности часов: 1 - целые, 0 - на грани разрушения. null, если часов нет.</summary>
    private float? GetHealthFraction(string id)
    {
        if (!TryFindHourglass(id, out var uid, out _))
            return null;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 1f;

        var threshold = _destructible.DestroyedAt(uid).Float();
        if (threshold <= 0f || threshold > 1_000_000f) // нет Destructible: DestroyedAt вернёт огромное число
            return 1f;

        return 1f - Math.Clamp(damageable.TotalDamage.Float() / threshold, 0f, 1f);
    }

    #endregion

    #region Завершение раунда

    /// <summary>
    ///     Завершает раунд. Идущие битвы помечаются как прерванные и попадают в итоги вместе с остальными.
    /// </summary>
    public bool TryEndRound(TimeSpan? delay, out string error)
    {
        error = string.Empty;

        if (!TryGetActiveRule(out var rule))
        {
            error = "Правило пресета SoT не активно.";
            return false;
        }

        if (GameTicker.RunLevel != GameRunLevel.InRound)
        {
            error = "Раунд не идёт.";
            return false;
        }

        foreach (var battle in rule.Comp.Battles)
        {
            if (battle.Status != SotBattleStatus.Active)
                continue;

            battle.Status = SotBattleStatus.Interrupted;
            battle.Reason = SotBattleEndReason.RoundEnded;
            battle.EndTime = GameTicker.RoundDuration();
        }

        _roundEnd.EndRound(delay ?? rule.Comp.RestartDelay);
        return true;
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        SotRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine(Loc.GetString("sot-round-end-header"));

        if (component.Battles.Count == 0)
        {
            args.AddLine(Loc.GetString("sot-round-end-no-battles"));
            args.AddLine("");
            return;
        }

        var athenaWins = 0;
        var reapersWins = 0;
        var draws = 0;
        var interrupted = 0;

        foreach (var battle in component.Battles)
        {
            // Раунд мог закончиться не через sotroundend: битва всё ещё числится идущей.
            var isActive = battle.Status == SotBattleStatus.Active;
            var status = isActive ? SotBattleStatus.Interrupted : battle.Status;
            var reason = isActive ? SotBattleEndReason.RoundEnded : battle.Reason ?? SotBattleEndReason.RoundEnded;
            var end = isActive ? GameTicker.RoundDuration() : battle.EndTime;

            string result;
            if (status == SotBattleStatus.Interrupted)
            {
                interrupted++;
                result = Loc.GetString("sot-round-end-result-interrupted", ("reason", Loc.GetString(ReasonKey(reason))));
            }
            else if (battle.WinnerId == null || battle.WinnerSide is not { } winnerSide)
            {
                draws++;
                result = Loc.GetString("sot-round-end-result-draw", ("reason", Loc.GetString(ReasonKey(reason))));
            }
            else
            {
                if (winnerSide == SotSide.Athena)
                    athenaWins++;
                else
                    reapersWins++;

                result = Loc.GetString("sot-round-end-result-win",
                    ("side", SideName(winnerSide)),
                    ("id", battle.WinnerId),
                    ("reason", Loc.GetString(ReasonKey(reason))));
            }

            args.AddLine(Loc.GetString("sot-round-end-battle",
                ("number", battle.Number),
                ("sideA", SideName(battle.SideA)),
                ("idA", battle.IdA),
                ("sideB", SideName(battle.SideB)),
                ("idB", battle.IdB),
                ("result", result),
                ("duration", (end - battle.StartTime).ToString(@"hh\:mm\:ss"))));
        }

        args.AddLine(Loc.GetString("sot-round-end-totals",
            ("athena", athenaWins),
            ("reapers", reapersWins),
            ("draws", draws),
            ("interrupted", interrupted)));

        if (athenaWins > reapersWins)
            args.AddLine(Loc.GetString("sot-round-end-winner", ("side", SideName(SotSide.Athena))));
        else if (reapersWins > athenaWins)
            args.AddLine(Loc.GetString("sot-round-end-winner", ("side", SideName(SotSide.Reapers))));
        else
            args.AddLine(Loc.GetString("sot-round-end-winner-none"));

        args.AddLine("");
    }

    #endregion

    #region Утилиты

    private string SideName(SotSide side)
    {
        return Loc.GetString(side switch
        {
            SotSide.Athena => "sot-side-athena",
            SotSide.Reapers => "sot-side-reapers",
            _ => "sot-side-unknown"
        });
    }

    private string ReasonKey(SotBattleEndReason reason)
    {
        return reason switch
        {
            SotBattleEndReason.HourglassDestroyed => "sot-battle-reason-destroyed",
            SotBattleEndReason.Forced => "sot-battle-reason-forced",
            _ => "sot-battle-reason-round-end"
        };
    }

    #endregion
}
