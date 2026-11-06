using Content.Shared.Store.Components;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    private void EnsureListingsCatalog(StoreComponent component)
    {
        if (component.FullListingsCatalog.Count == 0)
            RefreshAllListings(component);
    }
}
