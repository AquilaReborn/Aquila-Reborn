// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Kakila.AnnounceOnDestruction;

/// <summary>
///     При уничтожении сущности делает глобальное объявление на весь сервер
///     с заданным именем отправителя и текстом.
/// </summary>
/// <remarks>
///     Sender и Message могут быть как обычным текстом, так и ключом локализации (.ftl).
/// </remarks>
[RegisterComponent]
public sealed partial class AnnounceOnDestructionComponent : Component
{
    /// <summary>Имя отправителя объявления.</summary>
    [DataField(required: true)]
    public string Sender = string.Empty;

    /// <summary>Текст объявления.</summary>
    [DataField(required: true)]
    public string Message = string.Empty;

    /// <summary>Проигрывать ли звук объявления.</summary>
    [DataField]
    public bool PlaySound = true;

    /// <summary>Звук объявления. Если null — стандартный звук объявлений.</summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>Цвет текста объявления. Если null — стандартный.</summary>
    [DataField]
    public Color? Color;
}
