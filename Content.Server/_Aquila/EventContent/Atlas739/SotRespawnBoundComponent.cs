// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Висит на игроке, привязанном к платформе воскрешения (SotRespawnPad). Добавляется и снимается системой
///     SotRespawnSystem при взаимодействии с платформой.
/// </summary>
[RegisterComponent]
public sealed partial class SotRespawnBoundComponent : Component
{
    /// <summary>Платформа, к которой привязан игрок.</summary>
    [ViewVariables]
    public EntityUid Pad;

    /// <summary>Когда сработает автоматическое воскрешение (только при AutoRespawn платформы). null - не запланировано.</summary>
    [ViewVariables]
    public TimeSpan? RespawnAt;

    /// <summary>Время смерти (CurTime сервера). null - игрок жив. От него считается задержка действия призрака.</summary>
    [ViewVariables]
    public TimeSpan? DiedAt;

    /// <summary>Призрак игрока, получивший действие воскрешения.</summary>
    [ViewVariables]
    public EntityUid? Ghost;

    /// <summary>Действие воскрешения, выданное призраку.</summary>
    [ViewVariables]
    public EntityUid? GhostAction;
}
