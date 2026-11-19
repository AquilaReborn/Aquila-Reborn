using Content.Goobstation.Shared._Aquila.Changeling.Types;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server.Polymorph.Components;
using Content.Shared._CorvaxGoob.TTS;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;
using Robust.Server.GameObjects;

namespace Content.Goobstation.Server.Changeling;

public sealed partial class ChangelingSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    private const string TransformClientType = "ChangelingTransformBoundUserInterface";
    private static readonly LocId TransformSelectPopup = "changeling-transform-select";
    private static readonly LocId UnknownDna = "forensics-dna-unknown";

    private void InitializeAquila()
    {
        SubscribeLocalEvent<ChangelingIdentityComponent, BoundUserInterfaceMessageAttempt>(OnBoundUserInterfaceMessageAttempt);

        Subs.BuiEvents<ChangelingIdentityComponent>(ChangelingTransformUiKey.Key,
            subs => subs.Event<ChangelingTransformIdentitySelectMessage>(OnChangelingTransformIdentitySelectMessage));
    }

    private void SaveOriginalForm(Entity<ChangelingIdentityComponent> ent)
    {
        RestoreStoredForms(ent.AsNullable(), ent.Comp.OriginalForm, ent.Comp.CurrentForm, ent.Comp.SelectedForm);

        if (ent.Comp.OriginalForm != null
            || ent.Comp.CurrentForm != null
            || HasComp<PolymorphedEntityComponent>(ent)
            || !TryComp<HumanoidAppearanceComponent>(ent, out var appearance))
            return;

        var data = new TransformData
        {
            Name = MetaData(ent.Owner).EntityName,
            DNA = TryComp<DnaComponent>(ent, out var dna) && dna.DNA != null
                ? dna.DNA
                : Loc.GetString(UnknownDna),
            Appearance = SnapshotTransformAppearance(appearance),
            Voice = TryComp<TTSComponent>(ent, out var tts) ? tts.VoicePrototypeId : null,
        };

        if (TryComp<FingerprintComponent>(ent, out var fingerprint))
            data.Fingerprint = fingerprint.Fingerprint;

        ent.Comp.OriginalForm = data;
        ent.Comp.CurrentForm = data;
        ent.Comp.SelectedForm = data;
    }

    private void CopyChangelingIdentity(Entity<ChangelingIdentityComponent> ent, PolymorphedEvent args)
    {
        _polymorph.CopyPolymorphComponent<ChangelingIdentityComponent>(ent, args.NewEntity);

        if (ent.Owner == args.NewEntity
            || !TryComp<ChangelingIdentityComponent>(args.NewEntity, out var newComp))
            return;

        newComp.AbsorbedDNA = ent.Comp.AbsorbedDNA;
        newComp.AbsorbedHistory = ent.Comp.AbsorbedHistory;
        RestoreStoredForms((args.NewEntity, newComp),
            ent.Comp.OriginalForm,
            ent.Comp.CurrentForm,
            ent.Comp.SelectedForm);
    }

    private void UpdateTransformedForm(EntityUid newEnt, TransformData? data, ChangelingIdentityComponent? comp)
    {
        if (comp == null || !TryComp<ChangelingIdentityComponent>(newEnt, out var newComp))
            return;

        newComp.CurrentForm = data;
        UpdateTransformState(newEnt, newComp);
    }

    private void OpenTransformMenu(EntityUid uid, ChangelingIdentityComponent comp)
    {
        _ui.SetUi(uid, ChangelingTransformUiKey.Key,
            new InterfaceData(TransformClientType));

        UpdateTransformState(uid, comp);

        _ui.OpenUi(uid, ChangelingTransformUiKey.Key, uid);
    }

    private void UpdateTransformState(EntityUid uid, ChangelingIdentityComponent comp)
    {
        var forms = new List<ChangelingTransformEntry>();

        if (comp.OriginalForm is { } original)
        {
            forms.Add(new ChangelingTransformEntry(0,
                original.Name,
                comp.CurrentForm == original,
                comp.SelectedForm == original,
                new ChangelingTransformAppearance(original.Appearance)));
        }

        for (var i = 0; i < comp.AbsorbedDNA.Count; i++)
        {
            var data = comp.AbsorbedDNA[i];
            forms.Add(new ChangelingTransformEntry(i + 1,
                data.Name,
                comp.CurrentForm == data,
                comp.SelectedForm == data,
                new ChangelingTransformAppearance(data.Appearance)));
        }

        _ui.SetUiState(uid, ChangelingTransformUiKey.Key, new ChangelingTransformState(forms));
    }

    private void OnBoundUserInterfaceMessageAttempt(Entity<ChangelingIdentityComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey is ChangelingTransformUiKey.Key && !CanUseTransformMenu(ent.Owner, args.Actor))
            args.Cancel();
    }

    private bool CanUseTransformMenu(EntityUid uid, EntityUid user, bool quiet = false)
    {
        return user == uid;
    }

    private void OnChangelingTransformIdentitySelectMessage(Entity<ChangelingIdentityComponent> ent,
        ref ChangelingTransformIdentitySelectMessage args)
    {
        TrySelectTransformIdentity(ent.AsNullable(), args.Actor, args.Index);
    }

    /// <summary>
    /// Selects a stored transformation identity after validating the owner and menu session.
    /// </summary>
    public bool TrySelectTransformIdentity(Entity<ChangelingIdentityComponent?> ent, EntityUid user, int index,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanSelectTransformIdentity((ent.Owner, ent.Comp), user, index, quiet))
            return false;

        DoSelectTransformIdentity((ent.Owner, ent.Comp), GetFormByIndex(ent.Comp, index)!, quiet);
        return true;
    }

    private bool CanSelectTransformIdentity(Entity<ChangelingIdentityComponent> ent, EntityUid user, int index,
        bool quiet = false)
    {
        return CanUseTransformMenu(ent.Owner, user, quiet)
            && _ui.IsUiOpen(ent.Owner, ChangelingTransformUiKey.Key, user)
            && GetFormByIndex(ent.Comp, index) != null;
    }

    private void DoSelectTransformIdentity(Entity<ChangelingIdentityComponent> ent, TransformData data, bool quiet)
    {
        ent.Comp.SelectedForm = data;
        if (!quiet)
        {
            _popup.PopupEntity(Loc.GetString(TransformSelectPopup, ("target", data.Name)),
                ent.Owner,
                ent.Owner);
        }
        UpdateTransformState(ent.Owner, ent.Comp);
    }

    private TransformData? GetFormByIndex(ChangelingIdentityComponent comp, int index)
    {
        if (index == 0)
            return comp.OriginalForm;

        var absorbedIndex = index - 1;
        if (absorbedIndex < 0 || absorbedIndex >= comp.AbsorbedDNA.Count)
            return null;

        return comp.AbsorbedDNA[absorbedIndex];
    }
}
