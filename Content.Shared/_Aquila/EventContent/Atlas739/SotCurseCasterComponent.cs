// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Тот, кто может проклинать: выдаёт действие "Проклясть" (ActionSotCurse). Ставится роли верховного
///     пирата через AddComponentSpecial (см. Roles/athena.yml) или вручную: addcomp &lt;uid&gt; SotCurseCaster.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedSotCurseSystem))]
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

    /// <summary>
    ///     ИЗМЕНИТЬ: фракция, члены которой могут быть прокляты (сверяется с SotFaction.Side у цели).
    ///     Если не указано, проклинать можно членов СВОЕЙ фракции (по SotFaction.Side самого проклинающего):
    ///     у лидера Афины это Афина, у лидера Жнецов это Жнецы. Указывать нужно только для особых случаев.
    /// </summary>
    [DataField]
    public SotSide? TargetSide;

    /// <summary>Выданное действие (сервер). Не данные прототипа, поэтому не DataField.</summary>
    [ViewVariables]
    public EntityUid? ActionEntity;
}
