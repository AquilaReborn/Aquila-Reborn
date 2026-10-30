using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Aquila.HolyCorpse;

/// <summary>
/// Attaches to a creature that has at least one limb implanted in it.
/// </summary>
[RegisterComponent]
public sealed partial class HolyCorpseHostComponent : Component
{
    [ViewVariables]
    public Container LimbContainer = default!;

    /// <summary>
    /// All implanted limbs (entity + prototype).
    /// </summary>
    [ViewVariables]
    public List<(EntityUid Entity, string ProtoId)> Limbs = new();

    /// <summary>
    /// A set of components used during creation (for rollback upon deletion).
    /// </summary>
    [ViewVariables]
    public ComponentRegistry? AppliedComponents;

    [ViewVariables]
    public bool SetApplied;

    // The base stats of a guardian (excluding any buffs from appendages) are recorded immediately upon its creation.
    [ViewVariables] public float? BaseAttackRate;
    [ViewVariables] public DamageSpecifier? BaseMeleeDamage;
    [ViewVariables] public float? BaseStaminaDamage;
    [ViewVariables] public float? BaseDistanceAllowed;
    [ViewVariables] public float? BaseFireRate;
    [ViewVariables] public float? BaseRechargeCooldown;
}
