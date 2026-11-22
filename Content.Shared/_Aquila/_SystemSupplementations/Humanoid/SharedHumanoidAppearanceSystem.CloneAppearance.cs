using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    /// <summary>
    ///     Применяет сохранённую копию компонента внешности к сущности без обращения к её владельцу.
    /// </summary>
    /// <param name="target">Сущность, которой применяют внешность.</param>
    /// <param name="appearance">Сохранённая копия компонента внешности, например снимок формы генокрада.</param>
    public void CloneAppearance(EntityUid target, HumanoidAppearanceComponent appearance)
    {
        ApplyAppearance(target, appearance);
    }

    private void ApplyAppearance(EntityUid target, HumanoidAppearanceComponent appearance)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid))
            return;

        targetHumanoid.Species = appearance.Species;
        targetHumanoid.SkinColor = appearance.SkinColor;
        targetHumanoid.EyeColor = appearance.EyeColor;
        targetHumanoid.Age = appearance.Age;
        targetHumanoid.Height = appearance.Height;
        targetHumanoid.Width = appearance.Width;
        SetSex(target, appearance.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers = new(appearance.CustomBaseLayers);
        targetHumanoid.MarkingSet = new(appearance.MarkingSet);
        targetHumanoid.Gender = appearance.Gender;

        if (TryComp<GrammarComponent>(target, out var grammar))
            _grammarSystem.SetGender((target, grammar), appearance.Gender);

        _identity.QueueIdentityUpdate(target);
        Dirty(target, targetHumanoid);
        RaiseMarkingsChanged(target);
    }
}
