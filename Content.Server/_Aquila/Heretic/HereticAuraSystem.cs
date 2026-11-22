using Content.Shared._Aquila.Heretic;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Clothing;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic;

public sealed partial class HereticAuraSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly LocId AuraExamineMessage = "heretic-aura-examine";
    private static readonly EntProtoId[] AuraHidingClothes =
    [
        "ClothingOuterArmorHeretic",
        "ClothingOuterArmorCloakVoid",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<MindComponent, MindGotRemovedEvent>(OnMindGotRemoved);
        SubscribeLocalEvent<HereticAuraComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FeastOfOwlsComponent, ComponentStartup>(OnFeastOfOwls);
        SubscribeLocalEvent<InventoryComponent, ClothingDidEquippedEvent>(OnClothingChanged);
        SubscribeLocalEvent<InventoryComponent, ClothingDidUnequippedEvent>(OnClothingChanged);
    }

    /// <summary>
    /// Учитывает полученные очки знаний и обновляет ауру на текущем теле еретика.
    /// </summary>
    public void OnKnowledgeGained(Entity<HereticComponent> mind, float amount)
    {
        mind.Comp.TotalKnowledgeGained += MathF.Max(0f, amount);
        UpdateAura(mind, CompOrNull<MindComponent>(mind)?.OwnedEntity);
    }

    private void OnExamined(Entity<HereticAuraComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(AuraExamineMessage));
    }

    private void OnMindGotAdded(Entity<MindComponent> ent, ref MindGotAddedEvent args)
    {
        if (!TryComp<HereticComponent>(ent, out var heretic))
            return;

        UpdateAura((ent.Owner, heretic), args.Container.Owner);
    }

    private void OnMindGotRemoved(Entity<MindComponent> ent, ref MindGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Container))
            return;

        RemCompDeferred<HereticAuraComponent>(args.Container);
    }

    private void OnFeastOfOwls(Entity<FeastOfOwlsComponent> ent, ref ComponentStartup args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _)
            || !TryComp<HereticComponent>(mindId, out var heretic))
            return;

        heretic.FeastOfOwlsUsed = true;
        UpdateAura((mindId, heretic), ent.Owner);
    }

    private void OnClothingChanged(Entity<InventoryComponent> ent, ref ClothingDidEquippedEvent args)
        => UpdateHereticAura(ent.Owner);

    private void OnClothingChanged(Entity<InventoryComponent> ent, ref ClothingDidUnequippedEvent args)
        => UpdateHereticAura(ent.Owner);

    private void UpdateHereticAura(EntityUid body)
    {
        if (!_mind.TryGetMind(body, out var mindId, out _) || !TryComp<HereticComponent>(mindId, out var heretic))
            return;

        UpdateAura((mindId, heretic), body);
    }

    private void UpdateAura(Entity<HereticComponent> mind, EntityUid? body)
    {
        if (body is not { } target || TerminatingOrDeleted(target))
            return;

        if (mind.Comp.FeastOfOwlsUsed)
        {
            RemCompDeferred<HereticAuraComponent>(target);
            return;
        }

        if (WearsAuraHidingClothing(target))
        {
            RemCompDeferred<HereticAuraComponent>(target);
            return;
        }

        if (mind.Comp.TotalKnowledgeGained > mind.Comp.PointsToAura)
            EnsureComp<HereticAuraComponent>(target);
        else
            RemCompDeferred<HereticAuraComponent>(target);
    }

    private bool WearsAuraHidingClothing(EntityUid body)
    {
        if (!_inventory.TryGetSlotEntity(body, "outerClothing", out var outer))
            return false;

        if (MetaData(outer.Value).EntityPrototype is not { } proto)
            return false;

        foreach (var id in AuraHidingClothes)
        {
            if (id.Id == proto.ID)
                return true;

            foreach (var parent in proto.Parents ?? [])
            {
                if (parent == id.Id)
                    return true;
            }
        }

        return false;
    }
}
