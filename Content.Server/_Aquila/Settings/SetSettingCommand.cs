using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Administration;
using Content.Shared.Aquila.Settings;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Settings;

[AdminCommand(AdminFlags.Round)]
public sealed class SetSettingCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override string Command => "setsetting";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            var current = _cfg.GetCVar(AquilaCVars.Setting);
            shell.WriteLine(Loc.GetString("cmd-setsetting-current", ("setting", current)));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_proto.TryIndex<AqSettingPrototype>(args[0], out var setting))
        {
            shell.WriteError(Loc.GetString("cmd-setsetting-unknown", ("setting", args[0])));
            return;
        }

        _cfg.SetCVar(AquilaCVars.Setting, setting.ID);
        shell.WriteLine(Loc.GetString("cmd-setsetting-success", ("setting", Loc.GetString(setting.Name))));
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{shell.Player?.Name ?? "Server"} changed setting to {setting.ID}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _proto.EnumeratePrototypes<AqSettingPrototype>()
            .OrderBy(setting => setting, SettingUIComparer.Instance)
            .Select(setting => new CompletionOption(setting.ID, Loc.GetString(setting.Name)));

        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-setsetting-arg-setting"));
    }
}
