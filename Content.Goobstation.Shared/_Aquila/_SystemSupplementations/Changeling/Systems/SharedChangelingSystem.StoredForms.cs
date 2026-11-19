using Content.Goobstation.Shared.Changeling.Components;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChangelingSystem
{
    /// <summary>
    ///     Restores stored form references using their persistent identities.
    /// </summary>
    public void RestoreStoredForms(Entity<ChangelingIdentityComponent?> ent,
        TransformData? original,
        TransformData? current,
        TransformData? selected)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.OriginalForm = original;
        ent.Comp.CurrentForm = FindStoredForm(ent.Comp, current) ?? current;
        ent.Comp.SelectedForm = FindStoredForm(ent.Comp, selected) ?? original;
    }

    private TransformData? FindStoredForm(ChangelingIdentityComponent comp, TransformData? data)
    {
        if (data == null)
            return null;

        if (comp.OriginalForm is { } original && original.FormId == data.FormId)
            return original;

        foreach (var form in comp.AbsorbedDNA)
        {
            if (form.FormId == data.FormId)
                return form;
        }

        return null;
    }
}
