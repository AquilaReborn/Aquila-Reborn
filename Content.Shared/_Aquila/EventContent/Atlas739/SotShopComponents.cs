// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Маркер консоли закупок SoT. Система SotShopLinkSystem сама связывает такие консоли
///     с телепадами закупок (SotShopTelepad) на том же гриде, чтобы мапперу не приходилось
///     вручную соединять их сетевым конфигуратором.
/// </summary>
[RegisterComponent]
public sealed partial class SotShopConsoleComponent : Component;

/// <summary>Маркер телепада закупок SoT: сюда приходят заказы с консоли своего корабля.</summary>
[RegisterComponent]
public sealed partial class SotShopTelepadComponent : Component;
