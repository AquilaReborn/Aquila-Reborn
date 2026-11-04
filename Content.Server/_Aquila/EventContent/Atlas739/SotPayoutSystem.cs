// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Aquila.Economy.Systems;
using Content.Server.Chat.Managers;
using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.EventContent.Atlas739;
using Content.Shared.Access.Systems;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>Текущие настройки выплат правила SoT (для команды sotpayout).</summary>
public readonly record struct SotPayoutSettings(
    bool Enabled,
    int WinnerPayout,
    int LoserPayout,
    int DrawPayout,
    float WinnerCoefficient,
    float LoserCoefficient);

/// <summary>
///     Выплаты за битву на личные счета игроков (экономика Aquila). Деньги эмитируются: просто зачисляются.
///     Платят всем игрокам обеих фракций, участвовавших в битве (по компоненту SotFaction): победителям - одну сумму,
///     проигравшим - другую, при ничьей - третью. Игрок получает деньги на счёт ID-карты, которая у него есть;
///     без карты или счёта выплата пропускается (запись в лог сервера).
/// </summary>
public sealed class SotPayoutSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <summary>Выплачивает участникам завершённой битвы. Суммы уже с учётом коэффициентов.</summary>
    public void PayBattle(SotBattle battle, int winnerAmount, int loserAmount, int drawAmount)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy))
        {
            Log.Info($"Битва №{battle.Number}: экономика выключена (aquila.economy), выплаты пропущены.");
            return;
        }

        if (battle.SideA == battle.SideB)
        {
            Log.Warning($"Битва №{battle.Number}: обе стороны одной фракции, непонятно кому платить, выплаты пропущены.");
            return;
        }

        var party = Loc.GetString("sot-announcement-sender");
        var comment = Loc.GetString("sot-payout-comment", ("number", battle.Number));

        var paid = 0;
        var withoutAccount = 0;

        var query = EntityQueryEnumerator<SotFactionComponent>();
        while (query.MoveNext(out var uid, out var faction))
        {
            if (faction.Side != battle.SideA && faction.Side != battle.SideB)
                continue;

            // Платим только игрокам (у сущности есть разум), а не бессознательным NPC.
            if (!_mind.TryGetMind(uid, out _, out var mind))
                continue;

            int amount;
            LocId reason;
            string notification;

            if (battle.WinnerSide is not { } winnerSide)
            {
                amount = drawAmount;
                reason = "sot-bank-transaction-draw";
                notification = "sot-payout-notification-draw";
            }
            else if (faction.Side == winnerSide)
            {
                amount = winnerAmount;
                reason = "sot-bank-transaction-win";
                notification = "sot-payout-notification-win";
            }
            else
            {
                amount = loserAmount;
                reason = "sot-bank-transaction-lose";
                notification = "sot-payout-notification-lose";
            }

            if (amount <= 0)
                continue;

            if (!_idCard.TryFindIdCard(uid, out var idCard)
                || !_bank.TryGetAccount(idCard.Owner, out var account))
            {
                withoutAccount++;
                Log.Info($"Битва №{battle.Number}: у {ToPrettyString(uid)} нет ID-карты или личного счёта, выплата {amount} пропущена.");
                continue;
            }

            _bank.Deposit(account.Value.AsNullable(), amount, reason, party, comment);
            paid++;

            if (mind.UserId is { } userId && _player.TryGetSessionById(userId, out var session))
            {
                _chat.DispatchServerMessage(session,
                    Loc.GetString(notification, ("number", battle.Number), ("amount", amount)));
            }
        }

        Log.Info($"Битва №{battle.Number}: выплачено игрокам: {paid}, без счёта: {withoutAccount}.");
    }
}
