using Content.Goobstation.Shared.Changeling.Components;

namespace Content.Goobstation.Server.Changeling;

public sealed partial class ChangelingEggSystem
{
    private void RestoreChangelingIdentity(EntityUid uid, ChangelingEggComponent egg)
    {
        if (egg.lingComp == null)
        {
            EnsureComp<ChangelingIdentityComponent>(uid);
            return;
        }

        var saved = egg.lingComp;
        var identity = new ChangelingIdentityComponent
        {
            SoundPool = saved.SoundPool,
            ShriekSound = saved.ShriekSound,
            ShriekPower = saved.ShriekPower,
            ArmourSound = saved.ArmourSound,
            ArmourStripSound = saved.ArmourStripSound,
            StatusIcon = saved.StatusIcon,
            IsInLesserForm = true,
            IsInLastResort = false,
            UpdateCooldown = saved.UpdateCooldown,
            AbsorbedHistory = saved.AbsorbedHistory,
            AbsorbedDNA = saved.AbsorbedDNA,
            MaxAbsorbedDNA = saved.MaxAbsorbedDNA,
            TotalAbsorbedEntities = saved.TotalAbsorbedEntities,
            TotalChangelingsAbsorbed = saved.TotalChangelingsAbsorbed,
            TotalStolenDNA = saved.TotalStolenDNA,
            CurrentForm = saved.OriginalForm ?? saved.CurrentForm,
        };

        AddComp(uid, identity);
        _changeling.RestoreStoredForms((uid, identity), saved.OriginalForm,
            saved.OriginalForm ?? saved.CurrentForm, saved.SelectedForm ?? saved.OriginalForm);
        identity.TotalEvolutionPoints = saved.TotalEvolutionPoints;
        Dirty(uid, identity);
    }
}
