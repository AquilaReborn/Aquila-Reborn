// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Aquila.EventContent.Atlas739;

namespace Content.Server._Aquila.EventContent.Atlas739;

public enum SotBattleStatus : byte
{
    /// <summary>Битва идёт.</summary>
    Active,

    /// <summary>Битва завершена (победитель или ничья).</summary>
    Finished,

    /// <summary>Раунд закончился раньше битвы.</summary>
    Interrupted
}

public enum SotBattleEndReason : byte
{
    /// <summary>Часы одного из участников уничтожены.</summary>
    HourglassDestroyed,

    /// <summary>Битву закончила команда sotbattleend.</summary>
    Forced,

    /// <summary>Раунд закончился, пока битва шла.</summary>
    RoundEnded
}

/// <summary>
///     Запись об одной битве: кто с кем воевал, чем закончилось и сколько длилось.
///     Участники определяются по ID привязки их песочных часов (ShuttleLink).
/// </summary>
public sealed class SotBattle
{
    public int Number;

    public string IdA = string.Empty;
    public string IdB = string.Empty;
    public SotSide SideA;
    public SotSide SideB;

    public SotBattleStatus Status = SotBattleStatus.Active;
    public SotBattleEndReason? Reason;

    /// <summary>ID часов победителя. null - ничья или битва не завершена.</summary>
    public string? WinnerId;

    /// <summary>Сторона победителя. null - ничья или битва не завершена.</summary>
    public SotSide? WinnerSide;

    /// <summary>Время раунда, когда битва началась.</summary>
    public TimeSpan StartTime;

    /// <summary>Время раунда, когда битва закончилась.</summary>
    public TimeSpan EndTime;

    public bool Involves(string id) => IdA == id || IdB == id;

    public string OpponentOf(string id) => IdA == id ? IdB : IdA;
}
