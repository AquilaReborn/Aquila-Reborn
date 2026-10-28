using Content.Server._Goobstation.Wizard.Components;
using Robust.Shared.Map;

namespace Content.Server._Goobstation.Wizard.Systems;

public sealed partial class GravPulseOnMapInitSystem
{
    private readonly HashSet<EntityUid> _pending = new();

    /// <summary>
    /// Запускает отложенные импульсы для сущностей, которые уже попали на карту.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count == 0)
            return;

        foreach (var uid in _pending)
        {
            if (TryComp<GravPulseOnMapInitComponent>(uid, out var comp)
                && Transform(uid).MapID != MapId.Nullspace)
                Pulse((uid, comp));
        }

        _pending.Clear();
    }

    private bool TryDeferPulse(EntityUid uid)
    {
        if (Transform(uid).MapID != MapId.Nullspace)
            return false;

        _pending.Add(uid);
        return true;
    }
}
