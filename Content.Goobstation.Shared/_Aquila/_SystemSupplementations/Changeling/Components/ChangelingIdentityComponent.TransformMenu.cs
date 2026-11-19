using Content.Goobstation.Shared.Changeling.Systems;

namespace Content.Goobstation.Shared.Changeling.Components;

public sealed partial class ChangelingIdentityComponent
{
    /// <summary>
    ///     The original form of the changeling. It is not stored in <see cref="AbsorbedDNA"/>,
    ///     so it does not count towards <see cref="MaxAbsorbedDNA"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), Access(typeof(SharedChangelingSystem))]
    public TransformData? OriginalForm = null;
}
