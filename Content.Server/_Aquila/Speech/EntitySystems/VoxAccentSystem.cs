using System.Text.RegularExpressions;
using Content.Server._Aquila.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server._Aquila.Speech.EntitySystems;

/// <summary>
/// Акцент воксов: заменяет русскую "к" на "кик", а заглавную "К" на "Кик".
/// Латинскую k не трогает.
/// </summary>
public sealed class VoxAccentSystem : EntitySystem
{
    private static readonly Regex RegexK = new("[кК]", RegexOptions.Compiled);

    /// <summary>
    /// Подписывает систему на сбор акцентов при разговоре.
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
    }
}
