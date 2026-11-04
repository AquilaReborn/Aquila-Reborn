// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Игровое правило пресета SoT. Раунд состоит из нескольких битв; здесь хранится их журнал.
///     Окончание битвы раунд НЕ завершает: раунд завершает только команда sotroundend
///     (или любое другое штатное завершение раунда).
/// </summary>
[RegisterComponent, Access(typeof(SotRuleSystem))]
public sealed partial class SotRuleComponent : Component
{
    /// <summary>Через сколько раунд перезапустится после sotroundend (если не указано иное).</summary>
    [DataField]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(30);

    /// <summary>Запрещать битвы между часами одной стороны (Афина против Афины).</summary>
    [DataField]
    public bool RequireDifferentSides = true;

    // ---- Выплаты на личные счета (экономика Aquila) ----

    /// <summary>Платить ли участникам по итогам битвы.</summary>
    [DataField]
    public bool PayoutEnabled = true;

    /// <summary>Сколько получает каждый игрок фракции-победителя, кредитов.</summary>
    [DataField]
    public int WinnerPayout = 10000;

    /// <summary>Сколько получает каждый игрок проигравшей фракции, кредитов.</summary>
    [DataField]
    public int LoserPayout = 1000;

    /// <summary>Сколько получает каждый игрок обеих фракций при ничьей, кредитов (0 - без выплат).</summary>
    [DataField]
    public int DrawPayout = 0;

    /// <summary>Коэффициент к выплате победителям (итог = WinnerPayout * коэффициент). Меняется командой sotpayout.</summary>
    [DataField]
    public float WinnerCoefficient = 1f;

    /// <summary>Коэффициент к выплате проигравшим. Меняется командой sotpayout.</summary>
    [DataField]
    public float LoserCoefficient = 1f;

    /// <summary>Журнал всех битв раунда (по порядку).</summary>
    [ViewVariables]
    public List<SotBattle> Battles = new();

    [ViewVariables]
    public int NextBattleNumber = 1;
}
