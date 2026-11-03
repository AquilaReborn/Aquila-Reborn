using System.Linq;
using Content.Server._Aquila.Psychosis;
using Content.Server._Aquila.Psychosis.Components;
using Content.Server.Administration.Logs;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.ActionBlocker;
using Content.Shared._Aquila.Engram;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Engram;

/// <summary>
/// Душегуб вытягивает личность из мозга на чип, а чип, вставленный себе, выпускает энграмму:
/// голограмму жертвы с её внешностью, голосом и последней одеждой, которую видит и слышит только носитель.
/// </summary>
public sealed class EngramSystem : EntitySystem
{
    private static readonly LocId NoSoulMessage = "soulkiller-no-soul";
    private static readonly LocId AliveMessage = "soulkiller-target-alive";
    private static readonly LocId ExtractStartMessage = "soulkiller-extract-start";
    private static readonly LocId ExtractedMessage = "soulkiller-extracted";
    private static readonly LocId VictimMessage = "soulkiller-victim";
    private static readonly LocId ChipEmptyMessage = "engram-chip-empty";
    private static readonly LocId ChipName = "engram-chip-name";
    private static readonly LocId EngramAppearedMessage = "engram-appeared";

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ImaginaryFriendSystem _friend = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoulkillerComponent, AfterInteractEvent>(OnSoulkillerInteract);
        SubscribeLocalEvent<SoulkillerComponent, SoulkillerExtractDoAfterEvent>(OnExtractDoAfter);
        SubscribeLocalEvent<EngramChipComponent, UseInHandEvent>(OnChipUse);
        SubscribeLocalEvent<EngramChipComponent, EngramChipInsertDoAfterEvent>(OnChipInsertDoAfter);
    }

    private void OnSoulkillerInteract(Entity<SoulkillerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target
            || target == args.User)
            return;

        args.Handled = true;

        if (!_mind.TryGetMind(target, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString(NoSoulMessage), target, args.User);
            return;
        }

        if (!HasComp<BrainComponent>(target) && _mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString(AliveMessage), target, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.ExtractTime, new SoulkillerExtractDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString(ExtractStartMessage), target, args.User, PopupType.MediumCaution);
    }

    private void OnExtractDoAfter(Entity<SoulkillerComponent> ent, ref SoulkillerExtractDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target
            || !_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        args.Handled = true;

        var coords = Transform(args.User).Coordinates;
        var chip = Spawn(ent.Comp.ChipPrototype, coords);
        var chipComp = EnsureComp<EngramChipComponent>(chip);
        var container = _container.EnsureContainer<ContainerSlot>(chip, chipComp.ContainerId);

        var engram = CreateEngram(ent.Comp, target, coords);
        _container.Insert(engram, container);

        var victimName = Name(engram);
        _meta.SetEntityName(chip, Loc.GetString(ChipName, ("name", victimName)));

        _popup.PopupEntity(Loc.GetString(VictimMessage), target, target, PopupType.LargeCaution);
        _mind.TransferTo(mindId, engram, ghostCheckOverride: true, mind: mind);

        _popup.PopupEntity(Loc.GetString(ExtractedMessage, ("name", victimName)), args.User, args.User, PopupType.Medium);
        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.User):player} extracted the soul of {ToPrettyString(target):target} into an engram chip");
    }

    private EntityUid CreateEngram(SoulkillerComponent soulkiller, EntityUid victim, EntityCoordinates coords)
    {
        var victimHumanoid = CompOrNull<HumanoidAppearanceComponent>(victim);
        var species = _proto.Index(victimHumanoid?.Species ?? soulkiller.DefaultSpecies);
        var engram = Spawn(species.DollPrototype, coords);
        EnsureComp<ImaginaryFriendComponent>(engram);
        _friend.HideFromOthers(engram);

        RemComp<HandsComponent>(engram);
        RemComp<ComplexInteractionComponent>(engram);
        EntityManager.AddComponents(engram, _proto.Index(soulkiller.EngramTemplate));
        _actionBlocker.UpdateCanMove(engram);

        if (victimHumanoid != null)
            _humanoid.CloneAppearance(victim, engram, victimHumanoid);

        _meta.SetEntityName(engram, Name(victim));
        CopyClothing(soulkiller, victim, engram);
        CopyLanguages(victim, engram, species);
        return engram;
    }

    private void CopyLanguages(EntityUid victim, EntityUid engram, SpeciesPrototype species)
    {
        var knowledge = CompOrNull<LanguageKnowledgeComponent>(victim);
        if (knowledge == null)
            _proto.Index(species.Prototype).TryGetComponent(out knowledge, Factory);

        if (knowledge == null)
        {
            EnsureComp<UniversalLanguageSpeakerComponent>(engram);
            return;
        }

        var engramKnowledge = EnsureComp<LanguageKnowledgeComponent>(engram);
        engramKnowledge.SpokenLanguages = new(knowledge.SpokenLanguages);
        engramKnowledge.UnderstoodLanguages = new(knowledge.UnderstoodLanguages);
        _language.UpdateEntityLanguages(engram);

        if (TryComp<LanguageSpeakerComponent>(victim, out var speaker))
            _language.SetLanguage(engram, speaker.CurrentLanguage);
    }

    private void CopyClothing(SoulkillerComponent soulkiller, EntityUid victim, EntityUid engram)
    {
        foreach (var slot in soulkiller.CopiedSlots)
        {
            if (!_inventory.TryGetSlotEntity(victim, slot, out var worn)
                || MetaData(worn.Value).EntityPrototype is not { } proto)
                continue;

            var copy = Spawn(proto.ID, Transform(engram).Coordinates);
            ClearStorage(copy);
            EnsureComp<UnremoveableComponent>(copy);

            if (!_inventory.TryEquip(engram, copy, slot, silent: true, force: true))
                QueueDel(copy);
        }
    }

    private void ClearStorage(EntityUid item)
    {
        if (!TryComp<StorageComponent>(item, out var storage))
            return;

        foreach (var stored in storage.Container.ContainedEntities.ToArray())
        {
            QueueDel(stored);
        }
    }

    private void OnChipUse(Entity<EngramChipComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (GetEngram(ent) == null)
        {
            _popup.PopupEntity(Loc.GetString(ChipEmptyMessage), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InsertTime, new EngramChipInsertDoAfterEvent(), ent, used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnChipInsertDoAfter(Entity<EngramChipComponent> ent, ref EngramChipInsertDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || GetEngram(ent) is not { } engram)
            return;

        args.Handled = true;

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.Remove(engram, container, force: true, destination: Transform(args.User).Coordinates);

        _physics.SetCanCollide(engram, true);
        _actionBlocker.UpdateCanMove(engram);
        _friend.Attach(args.User, engram, false, EngramAppearedMessage);

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted the engram of {ToPrettyString(engram):target}");
        QueueDel(ent);
    }

    private EntityUid? GetEngram(Entity<EngramChipComponent> chip)
    {
        if (!_container.TryGetContainer(chip, chip.Comp.ContainerId, out var container)
            || container.ContainedEntities.Count == 0)
            return null;

        return container.ContainedEntities[0];
    }
}
