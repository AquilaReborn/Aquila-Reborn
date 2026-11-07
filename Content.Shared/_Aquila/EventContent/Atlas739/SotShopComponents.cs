// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Маркер консоли закупок SoT. Система SotShopLinkSystem сама связывает такие консоли
///     с телепадами закупок (SotShopTelepad) на том же гриде И ТОЙ ЖЕ СТОРОНЫ, чтобы мапперу
///     не приходилось вручную соединять их сетевым конфигуратором. Сторона важна, если консоли
///     и телепады обеих фракций стоят на одном гриде (например, общая тестовая станция):
///     без проверки стороны телепад мог бы достаться чужой консоли, и заказ ушёл бы не туда.
/// </summary>
[RegisterComponent]
public sealed partial class SotShopConsoleComponent : Component
{
    [DataField(required: true)]
    public SotSide Side;
}

/// <summary>Маркер телепада закупок SoT: сюда приходят заказы с консоли СВОЕЙ стороны на этом же гриде.</summary>
[RegisterComponent]
public sealed partial class SotShopTelepadComponent : Component
{
    [DataField(required: true)]
    public SotSide Side;
}
