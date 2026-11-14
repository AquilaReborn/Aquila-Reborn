using Content.Goobstation.Shared.ChronoLegionnaire.Components;

namespace Content.Goobstation.Client.ChronoLegionnaire;

public sealed partial class StasisOverlaySystem
{
    private void InitializeAquila()
    {
        SubscribeLocalEvent<InsideStasisComponent, ComponentRemove>(OnStasisRemoved);
        SubscribeLocalEvent<InsideStasisComponent, EntityTerminatingEvent>(OnStasisTerminating);
    }

    private void OnStasisRemoved(Entity<InsideStasisComponent> stasised, ref ComponentRemove args)
    {
        RemoveStasisOverlay(stasised);
    }

    private void OnStasisTerminating(Entity<InsideStasisComponent> stasised, ref EntityTerminatingEvent args)
    {
        RemoveStasisOverlay(stasised);
    }

    private void RemoveStasisOverlay(EntityUid stasised)
    {
        if (_player.LocalEntity == stasised)
            _overlayManager.RemoveOverlay(_overlay);
    }
}
