using System.Text.RegularExpressions;
using Content.Server._Aquila.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server._Aquila.Speech.EntitySystems;

/// <summary>
/// Vox accent: replaces the Russian letter "к" with "кик" and the Russian letter "ч" with "чич",
/// the uppercase "К" and "Ч" become "Кик" and "Чич". Latin letters are not touched.
/// </summary>
public sealed class VoxAccentSystem : EntitySystem
{
    private static readonly Regex RegexK = new("[кК]", RegexOptions.Compiled);
    private static readonly Regex RegexCh = new("[чЧ]", RegexOptions.Compiled);

    /// <summary>
    /// Subscribes the system to accent collection during speech.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoxAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private void OnAccentGet(EntityUid uid, VoxAccentComponent component, AccentGetEvent args)
    {
        args.Message = RegexK.Replace(args.Message, match =>
            match.Value == "К" ? component.UppercaseReplacement : component.LowercaseReplacement);

        args.Message = RegexCh.Replace(args.Message, match =>
            match.Value == "Ч" ? component.UppercaseChReplacement : component.LowercaseChReplacement);
    }
}
