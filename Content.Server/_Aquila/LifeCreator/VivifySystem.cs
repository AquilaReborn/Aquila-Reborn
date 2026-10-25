using Content.Server.Popups;
using Content.Shared.Aquila.LifeCreator;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Content.Server.DoAfter;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Aquila.LifeCreator;

public sealed class VivifySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VivifyItemActionEvent>(OnAction);
        SubscribeLocalEvent<LifeCreatorComponent, VivifyItemDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<VivifiedCreatureComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<VivifiedCreatureComponent, VivifyRevertDoAfterEvent>(OnRevertDoAfter);
        SubscribeLocalEvent<VivifiedCreatureComponent, MobStateChangedEvent>(OnCreatureStateChanged);
    }

    private void OnAction(VivifyItemActionEvent args)
    {
        if (args.Handled) return;

        var performer = args.Performer;

        if (!HasComp<LifeCreatorComponent>(performer))
        {
            _popup.PopupEntity(Loc.GetString("vivify-no-life-creator"), performer, performer);
            return;
        }

        if (!_hands.TryGetActiveItem(performer, out var item) || item is not { } itemUid)
        {
            _popup.PopupEntity(Loc.GetString("vivify-no-item"), performer, performer);
            return;
        }

        if (!HasComp<ItemComponent>(itemUid))
        {
            _popup.PopupEntity(Loc.GetString("vivify-not-item"), performer, performer);
            return;
        }

        if (HasComp<VivifiedCreatureComponent>(itemUid))
        {
            _popup.PopupEntity(Loc.GetString("vivify-already-creature"), performer, performer);
            return;
        }

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            TimeSpan.FromSeconds(3),
            new VivifyItemDoAfterEvent(args.CreaturePrototype),
            performer,
            target: itemUid,
            used: itemUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid performer, LifeCreatorComponent component, VivifyItemDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        args.Handled = true;

        if (args.Used is not { } itemUid || !Exists(itemUid)) return;
        if (!HasComp<ItemComponent>(itemUid)) return;
        if (HasComp<VivifiedCreatureComponent>(itemUid)) return;

        _hands.TryDrop(performer, itemUid, doDropInteraction: false);

        var coords = Transform(performer).Coordinates;
        var creatureUid = Spawn(args.CreaturePrototype, coords);

        var creatureComponent = EnsureComp<VivifiedCreatureComponent>(creatureUid);
        creatureComponent.Caster = performer;
        creatureComponent.ParkedItem = itemUid;

        var container = _container.EnsureContainer<ContainerSlot>(creatureUid, VivifiedCreatureComponent.ParkedItemContainerId);
        _container.Insert(itemUid, container);

        _hands.TryPickupAnyHand(performer, creatureUid);
    }

    private void OnGetVerbs(EntityUid uid, VivifiedCreatureComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract) return;
        if (args.User != component.Caster) return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("vivify-revert-verb"),
            Act = () => StartRevertDoAfter(uid, user)
        });
    }

    private void StartRevertDoAfter(EntityUid creatureUid, EntityUid performer)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            TimeSpan.FromSeconds(2),
            new VivifyRevertDoAfterEvent(),
            creatureUid,
            target: creatureUid)
        {
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRevertDoAfter(EntityUid creatureUid, VivifiedCreatureComponent component, VivifyRevertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        args.Handled = true;

        DoRevert(creatureUid, component);
    }

    private void OnCreatureStateChanged(EntityUid uid, VivifiedCreatureComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead) return;

        DoRevert(uid, component);
    }

    private void DoRevert(EntityUid creatureUid, VivifiedCreatureComponent component)
    {
        if (!Exists(component.ParkedItem))
            return;

        var innerContainer = _container.EnsureContainer<ContainerSlot>(creatureUid, VivifiedCreatureComponent.ParkedItemContainerId);
        _container.Remove(component.ParkedItem, innerContainer);

        BaseContainer? handContainer = null;
        if (_container.TryGetContainingContainer(creatureUid, out var outerContainer) &&
            HasComp<HandsComponent>(outerContainer.Owner))
        {
            handContainer = outerContainer;
        }

        var spawnCoords = Transform(creatureUid).Coordinates;

        QueueDel(creatureUid);

        if (handContainer != null)
            _container.Insert(component.ParkedItem, handContainer);
        else
            _transform.SetCoordinates(component.ParkedItem, spawnCoords);
    }
}
