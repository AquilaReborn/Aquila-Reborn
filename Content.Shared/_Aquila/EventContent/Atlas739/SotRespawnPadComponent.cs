// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Платформа воскрешения на базе. К ней привязываются игроки (взаимодействие: привязать/отвязать).
///     Когда привязанный игрок умирает, спустя Delay его тело телепортируется на платформу,
///     полностью лечится, а душа (если игрок ещё в роли призрака-посетителя) возвращается в тело.
/// </summary>
[RegisterComponent]
public sealed partial class SotRespawnPadComponent : Component
{
    /// <summary>Какой фракции принадлежит платформа. null - привязаться может кто угодно.</summary>
    [DataField]
    public SotSide? Side;

    /// <summary>Сколько игроков можно привязать к одной платформе.</summary>
    [DataField]
    public int MaxBound = 1;

    /// <summary>
    ///     Воскрешать ли автоматически через Delay после смерти. false (по умолчанию): игрок, ставший призраком,
    ///     получает действие "Воскреснуть на платформе" (доступно через ActionDelay после смерти).
    /// </summary>
    [DataField]
    public bool AutoRespawn;

    /// <summary>Через сколько после смерти воскрешение сработает само (только при AutoRespawn).</summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>Через сколько после смерти призрак сможет воскреснуть действием.</summary>
    [DataField]
    public TimeSpan ActionDelay = TimeSpan.FromSeconds(10);

    /// <summary>Действие, которое получает призрак привязанного игрока.</summary>
    [DataField]
    public EntProtoId ActionPrototype = "ActionSotRespawn";

    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [DataField]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    /// <summary>Привязанные игроки (рабочее состояние, заполняет сервер).</summary>
    [ViewVariables]
    public HashSet<EntityUid> Bound = new();
}

[Serializable, NetSerializable]
public enum SotRespawnPadVisuals : byte
{
    /// <summary>bool: к платформе привязан хотя бы один игрок.</summary>
    Bound
}

[Serializable, NetSerializable]
public enum SotRespawnPadVisualLayers : byte
{
    Base
}
