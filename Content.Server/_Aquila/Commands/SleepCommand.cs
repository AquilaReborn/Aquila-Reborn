using Content.Shared.Administration;
using Content.Shared.Bed.Sleep;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Aquila.Commands;

[AnyCommand]
public sealed class SleepCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    private static readonly LocId DescriptionLoc = "sleep-command-description";
    private static readonly LocId HelpLoc = "sleep-command-help";
    private static readonly LocId FailMessage = "sleep-command-failed";
    private static readonly LocId CannotRunMessage = "shell-cannot-run-command-from-server";

    public string Command => "sleep";
    public string Description => Loc.GetString(DescriptionLoc);
    public string Help => Loc.GetString(HelpLoc);

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString(CannotRunMessage));
            return;
        }

        if (player.AttachedEntity is not { Valid: true } entity || player.Status != SessionStatus.InGame)
            return;

        if (!_e.System<SleepingSystem>().TrySleeping(entity))
            shell.WriteError(Loc.GetString(FailMessage));
    }
}
