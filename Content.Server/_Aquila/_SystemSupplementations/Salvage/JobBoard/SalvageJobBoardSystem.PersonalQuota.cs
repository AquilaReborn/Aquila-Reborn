using Content.Server._Aquila.Contracts.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Salvage.JobBoard;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.JobBoard;

public sealed partial class SalvageJobBoardSystem
{
    [Dependency] private readonly PersonalContractSystem _contracts = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly LocId NeedClaimPopup = "job-board-need-claim";

    private void OnClaimMessage(Entity<SalvageJobBoardConsoleComponent> ent, ref JobBoardClaimMessage args)
    {
        if (_timing.CurTime < ent.Comp.NextClaimTime)
            return;

        ent.Comp.NextClaimTime = _timing.CurTime + ent.Comp.ClaimDelay;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<SalvageJobsDataComponent>(station, out var jobsData))
            return;

        if (!_prototypeManager.TryIndex<CargoBountyPrototype>(args.JobId, out var job))
            return;

        if (!GetAvailableJobs((station, jobsData)).Contains(args.JobId))
            return;

        if (!_contracts.TryClaim(station, args.Actor, job, job.Reward))
            return;

        UpdateUi(ent, (station, jobsData));
        SendMineUiAquila(ent, (station, jobsData), args.Actor);
    }

    private bool CanPrintLabelAquila(EntityUid station, EntityUid actor, CargoBountyPrototype job)
    {
        if (!_cfg.GetCVar(AquilaCVars.Economy) || _contracts.IsClaimedBy(station, actor, job))
            return true;

        _popup.PopupEntity(Loc.GetString(NeedClaimPopup), actor, actor);
        return false;
    }

    private void UpdateUiAquila(Entity<SalvageJobBoardConsoleComponent> ent, Entity<SalvageJobsDataComponent> stationEnt)
    {
        var jobs = GetAvailableJobs(stationEnt);

        var taken = new HashSet<string>();
        _contracts.WriteTakenJobs(stationEnt.Owner, jobs, taken);

        var state = new SalvageJobBoardConsoleState(
            GetRank(stationEnt).Title,
            GetRankProgression(stationEnt),
            jobs,
            taken);

        _ui.SetUiState(ent.Owner, SalvageJobBoardUiKey.Key, state);
    }

    private void SendMineUiAquila(Entity<SalvageJobBoardConsoleComponent> ent, Entity<SalvageJobsDataComponent> stationEnt, EntityUid actor)
    {
        var mine = new HashSet<string>();
        _contracts.WriteMineJobs(stationEnt.Owner, actor, GetAvailableJobs(stationEnt), mine);

        _ui.ServerSendUiMessage(ent.Owner, SalvageJobBoardUiKey.Key, new JobBoardMineJobsMessage(mine), actor);
    }
}
