// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Kakila.Sot;

/// <summary>
///     Широковещательное событие: битва записана в журнал и вот-вот начнётся.
///     Участники определяются по ID привязки их песочных часов (ShuttleLink).
/// </summary>
public sealed class SotBattleStartedEvent : EntityEventArgs
{
    public readonly int Number;
    public readonly string IdA;
    public readonly string IdB;

    public SotBattleStartedEvent(int number, string idA, string idB)
    {
        Number = number;
        IdA = idA;
        IdB = idB;
    }
}
