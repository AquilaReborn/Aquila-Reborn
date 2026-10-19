using Content.Goobstation.Shared._Aquila.Xenobiology; // Aquila Change
using Content.Goobstation.Shared._Aquila.Xenobiology.Systems; // Aquila Change
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Components.Equipment;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using System.Text;

namespace Content.Goobstation.Shared.Xenobiology.Systems;

public sealed class SlimeScannerSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SlimeGeneticsSystem _genetics = default!; // Aquila Change

    private StringBuilder _sb = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeComponent, AfterInteractUsingEvent>(OnSlimeAfterInteractUsing);
        SubscribeLocalEvent<SlimeExtractComponent, AfterInteractUsingEvent>(OnExtractAfterInteractUsing);
    }

    private void OnSlimeAfterInteractUsing(Entity<SlimeComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!CanSendTooltip(args))
            return;

        TrySendTooltip(args.User, ent, GenerateSlimeMarkup(ent));
        args.Handled = true;

        var ev = new SlimeScannedEvent(args.User, args.Used); // Aquila Change
        RaiseLocalEvent(ent, ref ev); // Aquila Change
    }

    private void OnExtractAfterInteractUsing(Entity<SlimeExtractComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!CanSendTooltip(args))
            return;

        var loc = Loc.GetString("slime-scanner-examine-extract", ("reagents", GenerateExtractMarkup(ent)));
        TrySendTooltip(args.User, ent, loc);
        args.Handled = true;
    }

    private bool CanSendTooltip(AfterInteractUsingEvent args)
        => !args.Handled && args.Target != null && args.CanReach && HasComp<SlimeScannerComponent>(args.Used);

    private void TrySendTooltip(EntityUid player, EntityUid target, string message)
    {
        var markup = FormattedMessage.FromMarkupOrThrow(message);
        _examine.SendExamineTooltip(player, target, markup, false, true);
    }

    private string GenerateSlimeMarkup(Entity<SlimeComponent> ent)
    {
        var mutationChancePercent = MathF.Floor(_genetics.GetMutationChance(ent.AsNullable()) * 100f); // Aquila Change

        var sb = new StringBuilder();

        sb.AppendLine(Loc.GetString("slime-scanner-examine-slime-description", ("color", ent.Comp.SlimeColor.ToHex()), ("name", Loc.GetString(_proto.Index(ent.Comp.Breed).BreedName)))); // Aquila Change

        // all this shit for a good looking examine text. imagine.
        sb.Append($"{Loc.GetString("slime-scanner-examine-slime-mutations", ("chance", mutationChancePercent))} ");
        var mutations = ent.Comp.PotentialMutations.ToList();
        for (int i = 0; i < mutations.Count; i++)
        {
            var info = _proto.Index(mutations[i]);

            var color = "white";
            // todo make the colors work
            if (info.Components.TryGetComponent(Factory.GetComponentName<SlimeComponent>(), out var sc)) // Aquila Change
                color = ((SlimeComponent) sc).SlimeColor.ToHex();

            sb.Append($"[color={color}]{Loc.GetString(info.BreedName)}[/color]"); // Aquila Change

            if (i == mutations.Count - 1)
                sb.AppendLine(".");
            else
                sb.Append(", ");
        }

        sb.AppendLine(Loc.GetString("slime-scanner-examine-slime-extracts", ("num", _genetics.GetExtractYield(ent.AsNullable())))); // Aquila Change

        var ev = new SlimeScannerInfoEvent(sb); // Aquila Change
        RaiseLocalEvent(ent, ref ev); // Aquila Change

        return sb.ToString();
    }

    private string GenerateExtractMarkup(Entity<SlimeExtractComponent> ent)
    {
        _sb.Clear();

        if (!TryComp<ReactiveComponent>(ent, out var reactive) || reactive.Reactions == null)
        {
            _sb.AppendLine(Loc.GetString("slime-scanner-examine-extract-unreactive"));
            return _sb.ToString();
        }

        // Aquila Change start
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
        // Aquila Change end

        return _sb.ToString();
    }
}
