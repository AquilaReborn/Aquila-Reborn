using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Goobstation.Shared._Aquila.Xenobiology;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Guidebook;

/// <summary>
/// Список карточек экстрактов слаймов в руководстве, при необходимости только одного уровня.
/// </summary>
[UsedImplicitly]
public sealed class GuideXenobioExtractGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    public GuideXenobioExtractGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;
    }

    /// <summary>
    /// Создаёт список по тегу руководства с необязательным аргументом Tier.
    /// </summary>
    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        int? tier = null;

        if (args.TryGetValue("Tier", out var tierArg))
        {
            if (!int.TryParse(tierArg, out var parsedTier))
            {
                _logManager.GetSawmill("guide_xenobio_extract").Error($"Invalid xenobio extract tier \"{tierArg}\"");
                return false;
            }

            tier = parsedTier;
        }

        CreateEntries(tier);

        control = this;
        return true;
    }

    private void CreateEntries(int? tier)
    {
        var guides = _prototype.EnumeratePrototypes<XenobioExtractGuidePrototype>()
            .Where(guide => tier == null || guide.Tier == tier)
            .OrderBy(guide => guide.Tier)
            .ThenBy(guide => _prototype.Index(guide.Extract).Name);

        foreach (var guide in guides)
        {
            AddChild(new GuideXenobioExtractEmbed(guide));
        }
    }
}
