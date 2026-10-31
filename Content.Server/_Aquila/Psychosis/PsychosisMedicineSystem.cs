using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Psychosis;

/// <summary>
/// Проверяет лекарства от психоза в крови: галоперидол глушит симптомы, нейролептин ещё и лечит.
/// </summary>
public sealed class PsychosisMedicineSystem : EntitySystem
{
    private static readonly ProtoId<ReagentPrototype> Haloperidol = "Haloperidol";
    private static readonly ProtoId<ReagentPrototype> Neuroleptin = "Neuroleptin";

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Смотрит, заглушены ли симптомы и идёт ли лечение.
    /// </summary>
    public void Check(EntityUid uid, out bool suppressed, out bool curing)
    {
        suppressed = false;
        curing = false;

        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        Entity<SolutionComponent>? blood = null;
        if (!_solution.ResolveSolution(uid, bloodstream.BloodSolutionName, ref blood, out var solution))
            return;

        curing = solution.GetTotalPrototypeQuantity(Neuroleptin) > 0;
        suppressed = curing || solution.GetTotalPrototypeQuantity(Haloperidol) > 0;
    }
}
