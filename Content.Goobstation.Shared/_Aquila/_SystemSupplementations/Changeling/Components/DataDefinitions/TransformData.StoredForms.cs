using Content.Goobstation.Shared.Serialization;

namespace Content.Goobstation.Shared.Changeling.Components;

public sealed partial class TransformData
{
    /// <summary>
    ///     Stable identity shared by serialized copies of the stored form.
    /// </summary>
    [DataField(customTypeSerializer: typeof(GuidSerializer))]
    public Guid FormId = Guid.NewGuid();
}
