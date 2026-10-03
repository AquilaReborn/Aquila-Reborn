using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Destructible;
using Content.Shared._Aquila.ArmorPlate;
using Content.Shared.Armor;
using Content.Shared.Body.Systems;
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
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly VerbCategory RemovePlateCategory = new(
        "verb-categories-remove-plate",
        "/Textures/Interface/VerbIcons/eject.svg.192dpi.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageArmorPlateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StorageArmorPlateComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<StorageArmorPlateComponent, InsertArmorPlateDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<StorageArmorPlateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StorageArmorPlateComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<StorageArmorPlateComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnRelayDamageModify);
    }

    private void OnInit(Entity<StorageArmorPlateComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Storage = _container.EnsureContainer<Container>(ent, StorageArmorPlateComponent.ContainerId);
    }

    private void OnInteractUsing(Entity<StorageArmorPlateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !CanInsert(ent, args.Used))
            return;

        args.Handled = true;

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

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnInsertDoAfter(Entity<StorageArmorPlateComponent> ent, ref InsertArmorPlateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } plate)
            return;

        if (!CanInsert(ent, plate) || !_container.Insert(plate, ent.Comp.Storage))
            return;

        args.Handled = true;
        _audio.PlayPvs(ent.Comp.PlateSound, ent);
    }

    private void OnGetVerbs(Entity<StorageArmorPlateComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.CanRemovePlates)
            return;

        var user = args.User;

        foreach (var plate in ent.Comp.Storage.ContainedEntities)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("storage-armor-plate-verb-entry",
                    ("name", Name(plate)),
                    ("integrity", GetPlateIntegrity(plate))),
                IconEntity = GetNetEntity(plate),
                Category = RemovePlateCategory,
                Act = () => RemovePlate(ent, user, plate),
            });
        }
    }

    private void OnExamined(Entity<StorageArmorPlateComponent> ent, ref ExaminedEvent args)
    {
        var plates = ent.Comp.Storage.ContainedEntities;
        if (!args.IsInDetailsRange || plates.Count == 0)
            return;

        using (args.PushGroup(nameof(StorageArmorPlateComponent)))
        {
            args.PushMarkup(Loc.GetString("storage-armor-plate-examine-count",
                ("count", plates.Count),
                ("max", ent.Comp.MaxPlates)));

            foreach (var plate in plates)
            {
                var integrity = GetPlateIntegrity(plate);

                args.PushMarkup(Loc.GetString("storage-armor-plate-examine-entry",
                    ("name", Name(plate)),
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

        foreach (var plate in ent.Comp.Storage.ContainedEntities)
        {
            if (!TryComp<ArmorComponent>(plate, out var armor) || !armor.ArmorCoverage.Contains(partType))
                continue;

            var before = args.Args.Damage;
            var after = DamageSpecifier.ApplyModifierSet(
                before,
                DamageSpecifier.PenetrateArmor(armor.Modifiers, before.ArmorPenetration));

            AbsorbDamage(plate, before, after);
            args.Args.Damage = after;
        }
    }

    private bool CanInsert(Entity<StorageArmorPlateComponent> ent, EntityUid plate)
    {
        return ent.Comp.Storage.ContainedEntities.Count < ent.Comp.MaxPlates
               && _tag.HasTag(plate, ent.Comp.PlateTag);
    }

    /// <returns>Целостность пластины в процентах.</returns>
    private int GetPlateIntegrity(EntityUid plate)
    {
        if (!TryComp<DamageableComponent>(plate, out var damageable))
            return 100;

        var destroyedAt = _destructible.DestroyedAt(plate);
        if (destroyedAt <= 0 || destroyedAt == FixedPoint2.MaxValue)
            return 100;

        var remaining = 1f - (damageable.TotalDamage / destroyedAt).Float();
        return (int) MathF.Round(Math.Clamp(remaining, 0f, 1f) * 100f);
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

    /// <summary>
    /// Переносит на пластину ту часть урона, которую она погасила.
    /// </summary>
    private void AbsorbDamage(EntityUid plate, DamageSpecifier before, DamageSpecifier after)
    {
        var absorbed = new DamageSpecifier();

        foreach (var (type, beforeValue) in before.DamageDict)
        {
            var diff = beforeValue - after.DamageDict.GetValueOrDefault(type);
            if (diff > 0)
                absorbed.DamageDict[type] = diff;
        }

        if (absorbed.DamageDict.Count > 0)
            _damageable.TryChangeDamage(plate, absorbed, ignoreResistances: true);
    }

    private void RemovePlate(Entity<StorageArmorPlateComponent> ent, EntityUid user, EntityUid plate)
    {
        if (!_container.Remove(plate, ent.Comp.Storage))
            return;

        _transform.SetCoordinates(plate, Transform(user).Coordinates);
        _hands.PickupOrDrop(user, plate);
        _audio.PlayPvs(ent.Comp.PlateSound, ent);
    }
}
