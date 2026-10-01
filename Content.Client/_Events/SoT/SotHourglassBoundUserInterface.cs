// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Kakila.Sot;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Kakila.Sot;

[UsedImplicitly]
public sealed class SotHourglassBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SotHourglassWindow? _window;

    public SotHourglassBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<SotHourglassWindow>();
        _window.OnVote += () => SendMessage(new SotHourglassVoteMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SotHourglassBuiState bState)
            return;

        _window?.UpdateState(bState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
        _window = null;
    }
}
