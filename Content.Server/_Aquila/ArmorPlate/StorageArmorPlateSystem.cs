using System.Linq;
using Content.Shared._Aquila.ArmorPlate;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Armor;
using Content.Shared.Body.Systems;
using Content.Server.Destructible;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Aquila.ArmorPlate;

public sealed class StorageArmorPlateSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static VerbCategory? _defaultRemovePlateCategory;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageArmorPlateComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StorageArmorPlateComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<StorageArmorPlateComponent, InsertArmorPlateDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<StorageArmorPlateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StorageArmorPlateComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<StorageArmorPlateComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnRelayDamageModify);

    }

    #region Event handlers

    private void OnComponentInit(Entity<StorageArmorPlateComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnInteractUsing(Entity<StorageArmorPlateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!CanInsert(ent, args.Used))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.InsertDelay,
            new InsertArmorPlateDoAfterEvent(),
            eventTarget: ent,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = true;
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnInsertDoAfter(Entity<StorageArmorPlateComponent> ent, ref InsertArmorPlateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } plate)
            return;

        if (!CanInsert(ent, plate))
            return;

        if (!_container.Insert(plate, ent.Comp.Storage))
            return;

        _audio.PlayPvs(ent.Comp.PlateSound, ent);
        args.Handled = true;
    }

    private void OnGetInteractionVerbs(Entity<StorageArmorPlateComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!ent.Comp.CanRemovePlates || ent.Comp.Storage.ContainedEntities.Count == 0)
            return;

        var user = args.User;
        var category = ent.Comp.RemovePlateCategory ?? GetDefaultCategory();

        var plates = ent.Comp.Storage.ContainedEntities.ToArray();

        foreach (var plateUid in plates)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("storage-armor-plate-verb-entry",
                    ("name", MetaData(plateUid).EntityName),
                    ("integrity", GetPlateIntegrity(plateUid))),
                IconEntity = GetNetEntity(plateUid),
                Category = category,
                Act = () => RemovePlate(ent, user, plateUid),
            });
        }
    }

    private void OnExamined(Entity<StorageArmorPlateComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Storage.ContainedEntities.Count == 0)
            return;

        using (args.PushGroup(nameof(StorageArmorPlateComponent)))
        {
            args.PushMarkup(Loc.GetString("storage-armor-plate-examine-count",
                ("count", ent.Comp.Storage.ContainedEntities.Count),
                ("max", ent.Comp.MaxPlates)));

            foreach (var plateUid in ent.Comp.Storage.ContainedEntities)
            {
                var integrity = GetPlateIntegrity(plateUid);

                args.PushMarkup(Loc.GetString("storage-armor-plate-examine-entry",
                    ("name", MetaData(plateUid).EntityName),
                    ("integrity", integrity),
                    ("color", GetIntegrityColor(integrity))));
            }
        }
    }

    private void OnRelayDamageModify(Entity<StorageArmorPlateComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (args.Args.TargetPart == null)
            return;

        var (partType, _) = _body.ConvertTargetBodyPart(args.Args.TargetPart);

        foreach (var plateUid in ent.Comp.Storage.ContainedEntities)
        {
            if (!TryComp<ArmorComponent>(plateUid, out var plateArmor))
                continue;

            if (!plateArmor.ArmorCoverage.Contains(partType))
                continue;

            var damageBeforePlate = args.Args.Damage;
            var damageAfterPlate = DamageSpecifier.ApplyModifierSet(
                damageBeforePlate,
                DamageSpecifier.PenetrateArmor(plateArmor.Modifiers, damageBeforePlate.ArmorPenetration));

            DamagePlate(plateUid, damageBeforePlate, damageAfterPlate);

            args.Args.Damage = damageAfterPlate;
        }
    }

    #endregion

    #region Helpers

    private int GetPlateIntegrity(EntityUid plateUid)
    {
        if (!TryComp<DamageableComponent>(plateUid, out var damageable))
            return 100;

        var destroyedAt = _destructible.DestroyedAt(plateUid);
        if (destroyedAt <= 0 || destroyedAt == FixedPoint2.MaxValue)
            return 100;

        var fraction = 1f - (damageable.TotalDamage / destroyedAt).Float();
        return (int) MathF.Round(Math.Clamp(fraction, 0f, 1f) * 100f);
    }

    private static string GetIntegrityColor(int integrity)
    {
        return integrity switch
        {
            > 66 => "lime",
            > 33 => "yellow",
            _ => "red",
        };
    }

    private bool CanInsert(Entity<StorageArmorPlateComponent> ent, EntityUid plate)
    {
        return ent.Comp.Storage.ContainedEntities.Count < ent.Comp.MaxPlates
               && _tag.HasTag(plate, ent.Comp.PlateTag);
    }

    private void DamagePlate(EntityUid plateUid, DamageSpecifier before, DamageSpecifier after)
    {
        var absorbed = new DamageSpecifier();

        foreach (var (type, beforeValue) in before.DamageDict)
        {
            var afterValue = after.DamageDict.GetValueOrDefault(type);
            var diff = beforeValue - afterValue;

            if (diff > 0)
                absorbed.DamageDict[type] = diff;
        }

        if (absorbed.DamageDict.Count == 0)
            return;

        _damageable.TryChangeDamage(plateUid, absorbed, ignoreResistances: true);
    }

    private void RemovePlate(Entity<StorageArmorPlateComponent> ent, EntityUid user, EntityUid plateUid)
    {
        if (!_container.Remove(plateUid, ent.Comp.Storage))
            return;

        _transform.SetCoordinates(plateUid, Transform(user).Coordinates);
        _hands.PickupOrDrop(user, plateUid);
        _audio.PlayPvs(ent.Comp.PlateSound, ent);
    }

    private static VerbCategory GetDefaultCategory()
    {
        return _defaultRemovePlateCategory ??= new VerbCategory(
            "verb-categories-remove-plate",
            "/Textures/Interface/VerbIcons/eject.svg.192dpi.png");
    }

    #endregion
}
