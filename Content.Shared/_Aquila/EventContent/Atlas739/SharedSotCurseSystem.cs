// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Общая основа системы проклятия SoT. Компоненты SotCurse и SotCurseCaster закрыты атрибутом Access на эту
///     систему: менять их поля может только она и её наследники (серверная SotCurseSystem), остальные только читают
///     (например, клиентская SotCurseVisualsSystem читает цвет).
/// </summary>
public abstract class SharedSotCurseSystem : EntitySystem;
