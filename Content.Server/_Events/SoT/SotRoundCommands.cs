// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Kakila.Sot;

/// <summary>sotroundprepare [ID часов]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotRoundPrepareCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotroundprepare";
    public override string Description => "Отправляет шаттл с указанными песочными часами (и всё на нём) в FTL-пространство и удерживает там до sotroundstart.";
    public override string Help => "Использование: sotroundprepare <ID часов>";

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

        shell.WriteLine($"Шаттл \"{args[0]}\" уходит в FTL-пространство и будет ждать там до sotroundstart.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint("<ID часов>")
            : CompletionResult.Empty;
    }
}

/// <summary>sotroundstart [ID1] [ID2] [ID карты] [X1] [Y1] [X2] [Y2]</summary>
[AdminCommand(AdminFlags.Round)]
public sealed class SotRoundStartCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SotRoundSystem _sot = default!;

    public override string Command => "sotroundstart";
    public override string Description => "Начинает битву: два подготовленных шаттла выходят из FTL на карте сражения в заданных координатах.";
    public override string Help =>
        "Использование: sotroundstart <ID часов 1> <ID часов 2> <ID карты> <X1> <Y1> <X2> <Y2>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 7)
        {
            shell.WriteError(Help);
            return;
        }

        if (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapInt))
        {
            shell.WriteError($"\"{args[2]}\" — не число, ожидался ID карты.");
            return;
        }

        if (!TryParseFloat(args[3], out var x1) || !TryParseFloat(args[4], out var y1) ||
            !TryParseFloat(args[5], out var x2) || !TryParseFloat(args[6], out var y2))
        {
            shell.WriteError("Координаты X/Y должны быть числами.");
            return;
        }

        if (!_sot.TryStart(args[0], args[1], new MapId(mapInt), new Vector2(x1, y1), new Vector2(x2, y2), out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine($"Битва началась: \"{args[0]}\" и \"{args[1]}\" выходят из FTL на карте {mapInt} (прибытие через несколько секунд).");
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
