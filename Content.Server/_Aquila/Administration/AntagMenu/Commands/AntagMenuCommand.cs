using Content.Server._Aquila.Administration.AntagMenu.Eui;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Aquila.Administration.AntagMenu.Commands;

/// <summary>
/// Открывает администратору меню антагонистов из вкладки F7.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class AntagMenuCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _euis = default!;

    /// <inheritdoc/>
    public override string Command => "antagmenu";

    /// <inheritdoc/>
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _euis.OpenEui(new AdminAntagMenuEui(), player);
    }
}
