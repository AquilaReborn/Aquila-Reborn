// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Kakila.Sot;

/// <summary>
///     Песочные часы SoT: сторона (Афина или Жнецы), голосование экипажа за готовность к битве.
///     Вместе с ShuttleLinkComponent (ID привязки) определяет участника битвы.
/// </summary>
[RegisterComponent]
public sealed partial class SotHourglassComponent : Component
{
    [DataField(required: true)]
    public SotSide Side;

    /// <summary>Сколько голосов живых членов экипажа нужно для готовности (и для её отмены).</summary>
    [DataField]
    public int RequiredVotes = 1;

    /// <summary>Сколько проходит от решающего голоса до вылета.</summary>
    [DataField]
    public TimeSpan DepartDelay = TimeSpan.FromSeconds(15);

    // ---- Рабочее состояние (заполняется сервером, на клиент не передаётся) ----

    /// <summary>Проголосовавшие в текущей фазе (за готовность или за отмену, смотря какая фаза).</summary>
    [ViewVariables]
    public HashSet<EntityUid> Voters = new();

    /// <summary>Когда вылетит корабль. null - обратный отсчёт не идёт.</summary>
    [ViewVariables]
    public TimeSpan? DepartAt;

    [ViewVariables]
    public SotHourglassPhase LastPhase = SotHourglassPhase.Idle;

    /// <summary>Хэш последнего отправленного состояния UI, чтобы не слать одно и то же.</summary>
    [ViewVariables]
    public int LastUiHash;
}

/// <summary>Стороны противостояния.</summary>
[Serializable, NetSerializable]
public enum SotSide : byte
{
    Athena,
    Reapers
}
