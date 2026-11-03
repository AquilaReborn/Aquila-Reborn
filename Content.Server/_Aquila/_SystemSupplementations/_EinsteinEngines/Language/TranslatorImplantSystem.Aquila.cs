using Content.Shared._Aquila.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Robust.Shared.Containers;

namespace Content.Server._EinsteinEngines.Language;

public sealed partial class TranslatorImplantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void AddChipLanguages(EntityUid implantee, EntityUid implant, ref DetermineEntityLanguagesEvent args)
    {
        if (!HasComp<TranslatorChipHolderComponent>(implant)
            || !_container.TryGetContainer(implant, TranslatorChipHolderComponent.ContainerId, out var chips)
            || !TryComp<LanguageKnowledgeComponent>(implantee, out var knowledge))
            return;

        foreach (var chip in chips.ContainedEntities)
        {
            if (!TryComp<TranslatorChipComponent>(chip, out var translator) || !translator.Enabled)
                continue;

            if (TranslatorSystem.CheckLanguagesMatch(translator.RequiredLanguages, knowledge.SpokenLanguages, translator.RequiresAllLanguages))
            {
                foreach (var language in translator.SpokenLanguages)
                    args.SpokenLanguages.Add(language);
            }

            if (TranslatorSystem.CheckLanguagesMatch(translator.RequiredLanguages, knowledge.UnderstoodLanguages, translator.RequiresAllLanguages))
            {
                foreach (var language in translator.UnderstoodLanguages)
                    args.UnderstoodLanguages.Add(language);
            }
        }
    }
}
