// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Kakila.Sot;

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

    /// <summary>Журнал всех битв раунда (по порядку).</summary>
    [ViewVariables]
    public List<SotBattle> Battles = new();

    [ViewVariables]
    public int NextBattleNumber = 1;
}
