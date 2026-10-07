// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Kakila.Sot;

/// <summary>
///     Фракция игрока в SoT (Афина или Жнецы). Выдаётся ролям через AddComponentSpecial
///     (см. Roles/SoT/athena.yml и reapers.yml) или вручную админом: addcomp &lt;uid&gt; SotFaction.
///     Песочными часами могут пользоваться только игроки своей фракции.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SotFactionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SotSide Side;
}
