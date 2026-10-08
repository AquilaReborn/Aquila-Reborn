// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Shared._Aquila.AnnounceOnDestruction;
using Content.Shared.Destructible; // если не компилируется: Content.Server.Destructible

namespace Content.Server._Aquila.AnnounceOnDestruction;

public sealed class AnnounceOnDestructionSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnDestructionComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDestruction(Entity<AnnounceOnDestructionComponent> ent, ref DestructionEventArgs args)
    {
        var comp = ent.Comp;

        // Поддержка и ключей локализации, и обычного текста.
        var sender = Loc.TryGetString(comp.Sender, out var s) ? s : comp.Sender;
        var message = Loc.TryGetString(comp.Message, out var m) ? m : comp.Message;

        _chat.DispatchGlobalAnnouncement(
            message,
            sender,
            comp.PlaySound,
            comp.Sound,
            comp.Color);
    }
}
