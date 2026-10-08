// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.EventContent.Atlas739;

[Serializable, NetSerializable]
public enum SotHourglassUiKey : byte
{
    Key
}

/// <summary>Что сейчас происходит с кораблём часов. От фазы зависит, за что голосуют игроки.</summary>
[Serializable, NetSerializable]
public enum SotHourglassPhase : byte
{
    /// <summary>Корабль на месте. Голосуют за готовность.</summary>
    Idle,

    /// <summary>Решающий голос получен, идёт обратный отсчёт до вылета. Голосуют за отмену.</summary>
    Countdown,

    /// <summary>Корабль в FTL и ждёт начала битвы. Голосуют за отмену (возврат на прежнюю позицию).</summary>
    Waiting,

    /// <summary>Идёт битва, голосовать нельзя.</summary>
    Battle,

    /// <summary>Корабль в FTL-прыжке или на перезарядке по другой причине, голосовать нельзя.</summary>
    FtlBusy,

    /// <summary>Недоступно (пресет не разрешён, часы не на шаттле или без ID).</summary>
    Unavailable
}

[Serializable, NetSerializable]
public sealed class SotHourglassBuiState : BoundUserInterfaceState
{
    public SotSide Side;
    public string ShipId = string.Empty;
    public SotHourglassPhase Phase;

    /// <summary>Сколько действующих голосов в текущей фазе.</summary>
    public int Votes;

    /// <summary>Сколько голосов нужно.</summary>
    public int Required;

    /// <summary>Кто проголосовал (чтобы клиент понял, голосовал ли он сам).</summary>
    public List<NetEntity> Voters = new();

    /// <summary>Время вылета (в CurTime сервера). null - отсчёта нет.</summary>
    public TimeSpan? DepartAt;

    /// <summary>Длина обратного отсчёта в секундах (для подсказки).</summary>
    public int DepartSeconds;
}

/// <summary>Игрок нажал кнопку голосования. Повторное нажатие снимает голос.</summary>
[Serializable, NetSerializable]
public sealed class SotHourglassVoteMessage : BoundUserInterfaceMessage
{
}
