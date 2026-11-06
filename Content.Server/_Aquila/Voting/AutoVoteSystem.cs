using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._Aquila.Voting;

public sealed class AutoVoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameMapManager _gameMap = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IVoteManager _vote = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private const int MinMapVoteOptions = 2;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (_player.PlayerCount == 0)
            return;

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                StartPresetVote();
                break;
            case GameRunLevel.PreRoundLobby when ev.Old == GameRunLevel.PostRound:
                StartMapVote();
                break;
        }
    }

    private void StartPresetVote()
    {
        if (!_cfg.GetCVar(CCVars.VotePresetEnabled))
            return;

        _vote.CreateStandardVote(null, StandardVoteType.Preset);
    }

    private void StartMapVote()
    {
        if (!_cfg.GetCVar(CCVars.VoteMapEnabled) ||
            !_ticker.LobbyEnabled ||
            _cfg.GetCVar(CCVars.GameMap) != string.Empty)
            return;

        if (_gameMap.CurrentlyEligibleMaps().Count() < MinMapVoteOptions)
            return;

        _vote.CreateStandardVote(null, StandardVoteType.Map);
    }
}
