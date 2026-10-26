using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._Aquila.Voting;

public sealed class RoundEndPresetVoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IVoteManager _vote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.PostRound)
            return;

        if (!_cfg.GetCVar(CCVars.VotePresetEnabled) || _player.PlayerCount == 0)
            return;

        _vote.CreateStandardVote(null, StandardVoteType.Preset);
    }
}
