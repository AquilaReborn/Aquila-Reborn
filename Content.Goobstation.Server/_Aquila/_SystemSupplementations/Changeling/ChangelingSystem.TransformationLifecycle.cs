using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared._Aquila.Actions.Types;
using Content.Shared.Humanoid;

namespace Content.Goobstation.Server.Changeling;

public sealed partial class ChangelingSystem
{
    private void RestoreTransformationForms(EntityUid uid, ChangelingIdentityComponent comp, bool sting)
    {
        if (!sting)
            RestoreStoredForms((uid, comp), comp.OriginalForm, comp.CurrentForm, comp.SelectedForm);
    }

    private bool IsCurrentTransformation(ChangelingIdentityComponent comp, TransformData data, bool sting)
    {
        return !sting && data == comp.CurrentForm;
    }

    private HumanoidAppearanceComponent SnapshotTransformAppearance(HumanoidAppearanceComponent appearance)
    {
        return _serialization.CreateCopy(appearance, notNullableOverride: true);
    }

    private void ApplyTransformAppearance(EntityUid uid, HumanoidAppearanceComponent appearance)
    {
        _humanoid.CloneAppearance(uid, appearance);
    }

    private void CloseTransformationInterfaces(EntityUid uid)
    {
        _ui.CloseUserUis(uid);
    }

    private void CompleteTransformation(EntityUid uid, EntityUid target)
    {
        RaiseNetworkEvent(new TransformActionLayoutEvent(GetNetEntity(uid), GetNetEntity(target)), target);
        QueueDel(uid);
    }

    private void StoreAbsorbedForm(ChangelingIdentityComponent comp, TransformData data)
    {
        if (comp.AbsorbedDNA.Count >= comp.MaxAbsorbedDNA && comp.AbsorbedDNA.Count > 0)
        {
            var dropped = comp.AbsorbedDNA[0];
            comp.AbsorbedDNA.RemoveAt(0);

            if (comp.SelectedForm == dropped)
                comp.SelectedForm = comp.OriginalForm;
        }

        comp.AbsorbedHistory.Add(data); // so we can't just come back and sting them again
        comp.AbsorbedDNA.Add(data);
        comp.TotalStolenDNA++;
    }
}
