// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>sotbattleprepare [ID часов]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotBattlePrepareCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotbattleprepare";
    public override string Description => "Отправляет шаттл с указанными песочными часами (и всё на нём) в FTL-пространство и удерживает там до sotbattlestart.";
    public override string Help => "Использование: sotbattleprepare <ID часов>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_sot.TryPrepare(args[0], out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine($"Шаттл \"{args[0]}\" уходит в FTL-пространство и будет ждать там до sotbattlestart.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint("<ID часов>")
            : CompletionResult.Empty;
    }
}

/// <summary>sotbattlestart [ID1] [ID2] [ID карты] [X1] [Y1] [X2] [Y2]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotBattleStartCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotbattlestart";
    public override string Description => "Начинает битву: два подготовленных шаттла выходят из FTL на карте сражения в заданных координатах.";
    public override string Help =>
        "Использование: sotbattlestart <ID часов 1> <ID часов 2> <ID карты> <X1> <Y1> <X2> <Y2>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 7)
        {
            shell.WriteError(Help);
            return;
        }

        if (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapInt))
        {
            shell.WriteError($"\"{args[2]}\" - не число, ожидался ID карты.");
            return;
        }

        if (!TryParseFloat(args[3], out var x1) || !TryParseFloat(args[4], out var y1) ||
            !TryParseFloat(args[5], out var x2) || !TryParseFloat(args[6], out var y2))
        {
            shell.WriteError("Координаты X/Y должны быть числами.");
            return;
        }

        if (!_sot.TryStart(args[0], args[1], new MapId(mapInt), new Vector2(x1, y1), new Vector2(x2, y2),
                out var battleNumber, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine($"Битва №{battleNumber} началась: \"{args[0]}\" и \"{args[1]}\" выходят из FTL на карте {mapInt} (прибытие через несколько секунд).");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<ID часов 1>"),
            2 => CompletionResult.FromHint("<ID часов 2>"),
            3 => CompletionResult.FromHint("<ID карты>"),
            4 => CompletionResult.FromHint("<X первого корабля>"),
            5 => CompletionResult.FromHint("<Y первого корабля>"),
            6 => CompletionResult.FromHint("<X второго корабля>"),
            7 => CompletionResult.FromHint("<Y второго корабля>"),
            _ => CompletionResult.Empty
        };
    }

    /// <summary>Понимает и точку, и запятую как разделитель дробной части.</summary>
    private static bool TryParseFloat(string s, out float value)
    {
        return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}

/// <summary>sotbattleend [ID часов участника] ([ID победителя] | draw)</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotBattleEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotbattleend";
    public override string Description =>
        "Принудительно завершает битву и объявляет победителя на весь сервер. Без указания победителя побеждает тот, чьи часы целее.";
    public override string Help =>
        "Использование: sotbattleend <ID часов любого участника> [ID победителя | draw]";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError(Help);
            return;
        }

        var winnerArg = args.Length == 2 ? args[1] : null;

        if (!_sot.TryEndBattle(args[0], winnerArg, out var battle, out var error) || battle == null)
        {
            shell.WriteError(error);
            return;
        }

        var result = battle.WinnerId == null ? "ничья" : $"победитель \"{battle.WinnerId}\"";
        shell.WriteLine($"Битва №{battle.Number} завершена принудительно: {result}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<ID часов участника>"),
            2 => CompletionResult.FromHint("[ID победителя | draw]"),
            _ => CompletionResult.Empty
        };
    }
}

/// <summary>sotroundend [секунд до рестарта]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotRoundEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotroundend";
    public override string Description =>
        "Завершает раунд. В итогах раунда выводятся все битвы и их победители (Афина или Жнецы).";
    public override string Help => "Использование: sotroundend [секунд до рестарта]";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        TimeSpan? delay = null;
        if (args.Length == 1)
        {
            if (!float.TryParse(args[0].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                || seconds < 0f)
            {
                shell.WriteError($"\"{args[0]}\" - не подходит, ожидалось неотрицательное число секунд.");
                return;
            }

            delay = TimeSpan.FromSeconds(seconds);
        }

        if (!_sot.TryEndRound(delay, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine("Раунд завершается. Итоги битв будут показаны на экране итогов.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint("[секунд до рестарта]")
            : CompletionResult.Empty;
    }
}
