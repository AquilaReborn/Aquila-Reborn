using Content.Goobstation.Shared._Aquila.Changeling;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client._Aquila.Changeling;

public sealed class ChangelingRoarWaveSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private ChangelingRoarWaveOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ChangelingRoarWaveOverlay();
        _overlayManager.AddOverlay(_overlay);

        SubscribeLocalEvent<ChangelingRoarWaveComponent, ComponentStartup>(OnStartup);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnStartup(Entity<ChangelingRoarWaveComponent> ent, ref ComponentStartup args)
    {
        _overlay.AddWave(ent, ent.Comp.WaveSpeed, ent.Comp.WaveStrength, ent.Comp.DownScale, ent.Comp.FadeTime);
    }
}
