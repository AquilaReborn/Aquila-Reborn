using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;

namespace Content.Goobstation.Shared.Xenobiology.Systems;

public sealed partial class SlimeScannerSystem
{
    private void AppendReactionReagents(ReactiveComponent reactive)
    {
        if (reactive.Reactions == null)
            return;

        var names = new List<string>();
        foreach (var reaction in reactive.Reactions)
        {
            if (reaction.Reagents == null)
                continue;

            foreach (var reagent in reaction.Reagents)
            {
                if (_proto.TryIndex<ReagentPrototype>(reagent, out var rid))
                    names.Add($"[color={rid.SubstanceColor.ToHex()}]{rid.LocalizedName}[/color]");
            }
        }

        _sb.Append(string.Join(", ", names));
        _sb.AppendLine(".");
    }
}
