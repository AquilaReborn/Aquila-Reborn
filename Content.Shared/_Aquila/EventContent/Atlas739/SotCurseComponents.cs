// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Проклятие SoT. Пока компонент есть на персонаже, он выглядит полупрозрачным с цветным оттенком
///     (клиентская часть: SotCurseVisualsSystem). Красится весь спрайт целиком, поэтому оттенок и
///     прозрачность получают и тело, и надетая одежда, и предметы в руках.
///     Накладывается действием верховного пирата (SotCurseCaster), снимается действием самого проклятого.
///     Вручную: addcomp &lt;uid&gt; SotCurse, цвет можно менять на лету через VV.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SotCurseComponent : Component
{
    /// <summary>
    ///     ИЗМЕНИТЬ: цвет проклятия. Спрайт умножается на этот цвет: RGB даёт оттенок (зелёный по умолчанию),
    ///     альфа-канал (A) даёт прозрачность (1 - непрозрачный, 0 - невидимый).
    ///     При наложении действием цвет берётся из SotCurseCaster.CurseColor.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = new(0.45f, 1f, 0.55f, 0.55f);

    /// <summary>Действие снятия проклятия, выдаётся проклятому (сервер).</summary>
    [DataField]
    public EntProtoId UncurseAction = "ActionSotUncurse";

    /// <summary>Выданное действие снятия (сервер).</summary>
    [DataField]
    public EntityUid? UncurseActionEntity;
}

/// <summary>
///     Тот, кто может проклинать: выдаёт действие "Проклясть" (ActionSotCurse). Ставится роли верховного
///     пирата через AddComponentSpecial (см. Roles/athena.yml) или вручную: addcomp &lt;uid&gt; SotCurseCaster.
/// </summary>
[RegisterComponent]
public sealed partial class SotCurseCasterComponent : Component
{
    /// <summary>ИЗМЕНИТЬ: действие наложения проклятия.</summary>
    [DataField]
    public EntProtoId Action = "ActionSotCurse";

    /// <summary>
    ///     ИЗМЕНИТЬ: цвет, которым проклятие ложится на цель (RGB - оттенок, A - прозрачность).
    ///     Меняется прямо в роли: type: SotCurseCaster / curseColor: "#73ff8c8c".
    /// </summary>
    [DataField]
    public Color CurseColor = new(0.45f, 1f, 0.55f, 0.55f);

    /// <summary>Фракция, члены которой могут быть прокляты (по SotFaction.Side у цели).</summary>
    [DataField]
    public SotSide TargetSide = SotSide.Athena;

    /// <summary>Выданное действие (сервер).</summary>
    [DataField]
    public EntityUid? ActionEntity;
}

/// <summary>Действие "Проклясть": цель, на которую нажал верховный пират.</summary>
public sealed partial class SotCurseActionEvent : EntityTargetActionEvent;

/// <summary>Действие "Снять проклятие": выполняет сам проклятый.</summary>
public sealed partial class SotUncurseActionEvent : InstantActionEvent;
