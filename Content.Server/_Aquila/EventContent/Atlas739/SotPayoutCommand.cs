// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>sotpayout [коэффициент победителей] [коэффициент проигравших]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotPayoutCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;
    [Dependency] private readonly SotRuleSystem _rule = default!;

    public override string Command => "sotpayout";
    public override string Description =>
        "Показывает настройки выплат за битвы или меняет коэффициенты (выплата = базовая сумма * коэффициент).";
    public override string Help =>
        "Использование: sotpayout - показать; sotpayout <коэф. победителей> [коэф. проигравших] - задать. Например: sotpayout 1.5 0.5";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_sot.CheckPreset(out var presetError))
        {
            shell.WriteError(presetError);
            return;
        }

        if (args.Length > 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (args.Length > 0)
        {
            if (!TryParse(args[0], out var winner) || (args.Length == 2 && !TryParse(args[1], out _)))
            {
                shell.WriteError("Коэффициенты должны быть неотрицательными числами.");
                return;
            }

            float? loser = null;
            if (args.Length == 2)
            {
                TryParse(args[1], out var l);
                loser = l;
            }

            if (!_rule.TrySetPayoutCoefficients(winner, loser, out var error))
            {
                shell.WriteError(error);
                return;
            }
        }

        if (!_rule.TryGetPayoutSettings(out var s))
        {
            shell.WriteError("Правило пресета SoT не активно.");
            return;
        }

        shell.WriteLine($"Выплаты за битвы: {(s.Enabled ? "включены" : "выключены")}.");
        shell.WriteLine($"Победители: {s.WinnerPayout} x {s.WinnerCoefficient.ToString(CultureInfo.InvariantCulture)} = {Math.Round(s.WinnerPayout * s.WinnerCoefficient)} кредитов каждому.");
        shell.WriteLine($"Проигравшие: {s.LoserPayout} x {s.LoserCoefficient.ToString(CultureInfo.InvariantCulture)} = {Math.Round(s.LoserPayout * s.LoserCoefficient)} кредитов каждому.");
        shell.WriteLine($"Ничья: {s.DrawPayout} кредитов каждому.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("[коэффициент победителей]"),
            2 => CompletionResult.FromHint("[коэффициент проигравших]"),
            _ => CompletionResult.Empty
        };
    }

    private static bool TryParse(string s, out float value)
    {
        return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               && value >= 0f;
    }
}
