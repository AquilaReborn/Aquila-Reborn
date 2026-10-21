using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Aquila.Settings;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Settings;

public sealed class AqSettingSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, AquilaCVars.Setting, OnSettingChanged);
    }

    /// <summary>
    /// Возвращает активный сеттинг или null, если в CVar указан несуществующий.
    /// </summary>
    public AqSettingPrototype? GetActiveSetting()
    {
        return _proto.TryIndex<AqSettingPrototype>(_cfg.GetCVar(AquilaCVars.Setting), out var setting)
            ? setting
            : null;
    }

    private void OnSettingChanged(string value)
    {
        if (!_proto.TryIndex<AqSettingPrototype>(value, out var setting))
        {
            Log.Error($"Unknown setting {value} was selected!");
            return;
        }

        if (_ticker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            var selected = _gameMapManager.GetSelectedMap();
            if (selected == null || _gameMapManager.AllVotableMaps().All(map => map.ID != selected.ID))
            {
                _gameMapManager.ClearSelectedMap();
                _gameMapManager.SelectMapByConfigRules();
            }

            _ticker.UpdateInfoText();
        }

        _chat.DispatchServerAnnouncement(Loc.GetString("aq-setting-changed", ("setting", Loc.GetString(setting.Name))));
    }
}
