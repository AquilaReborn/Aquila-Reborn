using System.Linq;
using Content.Goobstation.Common.Barks;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Множитель роста персонажа.
    /// </summary>
    [DataField]
    public float Height { get; set; } = 1f;

    /// <summary>
    /// Множитель ширины персонажа.
    /// </summary>
    [DataField]
    public float Width { get; set; } = 1f;

    /// <summary>
    /// Возвращает копию профиля с новыми ООС-заметками.
    /// </summary>
    public HumanoidCharacterProfile WithOocNotes(string oocNotes)
    {
        return new(this) { OocNotes = oocNotes };
    }

    /// <summary>
    /// Возвращает копию профиля с новым ростом.
    /// </summary>
    public HumanoidCharacterProfile WithHeight(float height)
    {
        return new(this) { Height = height };
    }

    /// <summary>
    /// Возвращает копию профиля с новой шириной.
    /// </summary>
    public HumanoidCharacterProfile WithWidth(float width)
    {
        return new(this) { Width = width };
    }

    private string GetValidOocNotes(IConfigurationManager configManager)
    {
        var maxOocNotesLength = configManager.GetCVar(CCVars.MaxFlavorTextLength);
        var oocNotes = FormattedMessage.RemoveMarkupOrThrow(OocNotes);
        if (oocNotes.Length > maxOocNotesLength)
            oocNotes = oocNotes[..maxOocNotesLength];

        return oocNotes;
    }

    private ProtoId<BarkPrototype> GetValidBarkVoice(IPrototypeManager prototypeManager)
    {
        if (prototypeManager.TryIndex(BarkVoice, out var bark)
            && bark.RoundStart
            && (bark.SpeciesWhitelist == null || bark.SpeciesWhitelist.Contains(Species)))
        {
            return BarkVoice;
        }

        return prototypeManager.EnumeratePrototypes<BarkPrototype>()
            .Where(o => o.RoundStart && (o.SpeciesWhitelist is null || o.SpeciesWhitelist.Contains(Species)))
            .Select(o => (ProtoId<BarkPrototype>) o.ID)
            .FirstOrDefault(SharedHumanoidAppearanceSystem.DefaultBarkVoice);
    }
}
