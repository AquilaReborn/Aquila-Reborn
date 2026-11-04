// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._Aquila.EventContent.Atlas739;

namespace Content.Server._Aquila.EventContent.Atlas739;

/// <summary>
///     Запрет гиба для игроков фракций (флаг SotFaction.PreventGib).
///     Человек разрывается по порогу разрушения (Destructible) с поведением GibBehavior. При старте компонента
///     SotFaction мы убираем эти поведения из ЭКЗЕМПЛЯРА компонента Destructible владельца: прототип и остальные
///     существа не затрагиваются. Урон по-прежнему убивает, но тело остаётся целым.
///     Другие источники гиба (GibOnRoundEnd, экспериментальный телепортёр, прямые вызовы GibBody) не затронуты.
/// </summary>
public sealed class SotFactionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotFactionComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SotFactionComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.PreventGib || !TryComp<DestructibleComponent>(ent, out var destructible))
            return;

        foreach (var threshold in destructible.Thresholds)
        {
            threshold.Behaviors.RemoveAll(behavior => behavior is GibBehavior);
        }
    }
}
