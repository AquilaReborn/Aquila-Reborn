// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Aquila.EventContent.Atlas739;
using Robust.Client.GameObjects;

namespace Content.Client._Aquila.EventContent.Atlas739;

/// <summary>
///     Внешний вид проклятия SoT: красит спрайт персонажа цветом SotCurse.Color (оттенок + прозрачность).
///     Цвет задаётся на весь SpriteComponent, а слои одежды, предметов в руках, волос и т.д. лежат в том же
///     спрайте, поэтому проклятие действует и на них. При снятии проклятия возвращается исходный цвет.
/// </summary>
public sealed class SotCurseVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>Цвет спрайта до проклятия (чтобы вернуть как было, а не просто белый).</summary>
    private readonly Dictionary<EntityUid, Color> _originalColors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotCurseComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SotCurseComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<SotCurseComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SotCurseComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void OnState(Entity<SotCurseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Цвет могли поменять на лету (VV, повторное наложение): красим заново.
        Apply(ent);
    }

    private void OnShutdown(Entity<SotCurseComponent> ent, ref ComponentShutdown args)
    {
        if (!_originalColors.Remove(ent.Owner, out var original))
            return;

        if (TerminatingOrDeleted(ent) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetColor((ent.Owner, sprite), original);
    }

    private void Apply(Entity<SotCurseComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Запоминаем исходный цвет только один раз (первый вызов), дальше только перекрашиваем.
        _originalColors.TryAdd(ent.Owner, sprite.Color);

        _sprite.SetColor((ent.Owner, sprite), ent.Comp.Color);
    }
}
