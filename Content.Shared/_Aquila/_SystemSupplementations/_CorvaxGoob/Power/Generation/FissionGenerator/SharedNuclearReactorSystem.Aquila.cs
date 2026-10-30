using Content.Shared.ActionBlocker;

namespace Content.Shared._CorvaxGoob.Power.Generation.FissionGenerator;

public abstract partial class SharedNuclearReactorSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    /// <summary>
    /// Проверяет, может ли сущность управлять реактором через интерфейс. Призраки и прочие наблюдатели не могут.
    /// </summary>
    protected bool CanOperateReactor(EntityUid actor)
    {
        return _actionBlocker.CanComplexInteract(actor);
    }
}
