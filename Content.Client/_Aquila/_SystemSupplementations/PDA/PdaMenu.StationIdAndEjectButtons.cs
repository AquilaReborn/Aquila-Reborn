using System.Text.RegularExpressions;
using Content.Shared.PDA;
using Robust.Shared.Timing;

namespace Content.Client.PDA;

public sealed partial class PdaMenu
{
    private static readonly Regex StationIdentifierRegex = new(@"\b[A-Z]{2,}-\d+\b", RegexOptions.RightToLeft);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entityManager.TryGetComponent<PdaComponent>(PdaEntity, out var pda))
            return;

        if (EjectIdButton.IsActive != pda.IdSlot.HasItem)
            EjectIdButton.IsActive = pda.IdSlot.HasItem;

        if (EjectPenButton.IsActive != pda.PenSlot.HasItem)
            EjectPenButton.IsActive = pda.PenSlot.HasItem;

        if (EjectPaiButton.IsActive != pda.PaiSlot.HasItem)
            EjectPaiButton.IsActive = pda.PaiSlot.HasItem;
    }

    private static string GetStationIdentifier(string stationName)
    {
        var match = StationIdentifierRegex.Match(stationName);
        return match.Success ? match.Value : stationName;
    }
}
