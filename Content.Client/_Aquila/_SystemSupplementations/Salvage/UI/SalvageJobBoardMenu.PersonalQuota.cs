using Content.Shared.Cargo.Prototypes;
using Content.Shared.Salvage.JobBoard;
using Robust.Shared.Prototypes;

namespace Content.Client.Salvage.UI;

public sealed partial class SalvageJobBoardMenu
{
    public Action<string>? OnClaimButtonPressed;

    private void AquilaBindClaim(JobEntry entry, ProtoId<CargoBountyPrototype> job, SalvageJobBoardConsoleState state, HashSet<string> mine)
    {
        entry.AquilaSetupClaim(state.TakenJobs.Contains(job.Id), mine.Contains(job.Id));
        entry.OnClaimButtonPressed += () => OnClaimButtonPressed?.Invoke(job);
    }
}
