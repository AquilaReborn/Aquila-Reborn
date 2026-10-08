// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Доступ к песочным часам только для своей фракции. Работает и на клиенте, и на сервере,
///     чтобы окно часов не открывалось у чужих игроков.
/// </summary>
public sealed class SotHourglassAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotHourglassComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    /// <summary>true, если у игрока есть SotFaction и она совпадает со стороной часов.</summary>
    public bool IsOwnFaction(EntityUid user, SotSide hourglassSide)
    {
        return TryComp<SotFactionComponent>(user, out var faction) && faction.Side == hourglassSide;
    }

    private void OnOpenAttempt(EntityUid uid, SotHourglassComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || IsOwnFaction(args.User, comp.Side))
            return;

        args.Cancel();

        if (args.Silent)
            return;

        _popup.PopupClient(Loc.GetString("sot-hourglass-popup-wrong-faction"), uid, args.User);
    }
}
