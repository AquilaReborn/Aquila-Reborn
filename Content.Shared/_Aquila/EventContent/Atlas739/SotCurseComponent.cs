// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Проклятие SoT. Пока компонент есть на персонаже, он выглядит полупрозрачным с цветным оттенком
///     (клиентская часть: SotCurseVisualsSystem, постшейдер SotCurse). Спрайт собирается в один силуэт и
///     красится целиком, поэтому оттенок и прозрачность получают и тело, и одежда, и предметы в руках,
///     а тело не просвечивает сквозь одежду.
///     Накладывается действием верховного пирата (SotCurseCaster), снимается действием самого проклятого.
///     Вручную: addcomp &lt;uid&gt; SotCurse, цвет можно менять на лету через VV.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedSotCurseSystem))]
public sealed partial class SotCurseComponent : Component
{
    /// <summary>
    ///     ИЗМЕНИТЬ: цвет проклятия. Спрайт умножается на этот цвет: RGB даёт оттенок (зелёный по умолчанию),
    ///     альфа-канал (A) даёт прозрачность (1 - непрозрачный, 0 - невидимый).
    ///     При наложении действием цвет берётся из SotCurseCaster.CurseColor.
    ///     В YAML поле называется color.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = new(0.45f, 1f, 0.55f, 0.55f);

    /// <summary>Действие снятия проклятия, выдаётся проклятому.</summary>
    [DataField]
    public EntProtoId UncurseAction = "ActionSotUncurse";

    /// <summary>Выданное действие снятия (сервер). Не данные прототипа, поэтому не DataField.</summary>
    [ViewVariables]
    public EntityUid? UncurseActionEntity;
}
