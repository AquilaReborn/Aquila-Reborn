using Content.Shared._Aquila.PDA.Ringer;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.PDA;

public abstract partial class SharedRingerSystem
{
    private static readonly LocId VibrationPopup = "comp-ringer-vibration-popup";

    private void InitializeAquila()
    {
        SubscribeLocalEvent<RingerComponent, RingerSetSilentMessage>(OnSetSilent);
    }

    /// <summary>
    /// Включает или выключает беззвучный режим КПК: в нём уведомления только вибрируют, а рингтон не играет.
    /// </summary>
    public void SetSilent(Entity<RingerComponent?> ent, bool silent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Silent == silent)
            return;

        ent.Comp.Silent = silent;
        DirtyField(ent, nameof(RingerComponent.Silent));
        UpdateRingerUi((ent, ent.Comp));
    }

    private void OnSetSilent(Entity<RingerComponent> ent, ref RingerSetSilentMessage args)
    {
        SetSilent(ent.AsNullable(), args.Silent);
    }

    private bool TryVibrateSilently(Entity<RingerComponent> ent)
    {
        if (!ent.Comp.Silent)
            return false;

        _popup.PopupPredicted(Loc.GetString(VibrationPopup),
            ent,
            ent.Owner,
            Filter.Pvs(ent, 0.05f),
            false,
            PopupType.Medium);

        return true;
    }
}
