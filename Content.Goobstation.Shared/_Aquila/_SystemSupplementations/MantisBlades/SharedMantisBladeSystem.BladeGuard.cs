namespace Content.Goobstation.Shared.MantisBlades;

public sealed partial class SharedMantisBladeSystem
{
    private bool TryGetValidBlade(EntityUid? blade, out EntityUid validBlade)
    {
        validBlade = EntityUid.Invalid;
        if (blade is not { } value || value == EntityUid.Invalid || Deleted(value))
            return false;
        validBlade = value;
        return true;
    }
}
