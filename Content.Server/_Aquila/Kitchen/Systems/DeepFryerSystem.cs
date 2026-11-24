using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._Aquila.Kitchen;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Kitchen;

/// <summary>
/// Фритюрница. Еду кладут в корзину, включают взаимодействием, через <see cref="DeepFryerComponent.FryTime"/>
/// партия готова: сырые прототипы из <see cref="DeepFryerComponent.Recipes"/> меняются на готовые, а остальная
/// еда остаётся как есть.
/// </summary>
public sealed class DeepFryerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly LocId PopupNoPower = "deep-fryer-popup-no-power";
    private static readonly LocId PopupBusy = "deep-fryer-popup-busy";
    private static readonly LocId PopupEmpty = "deep-fryer-popup-empty";
    private static readonly LocId PopupFull = "deep-fryer-popup-full";
    private static readonly LocId PopupNotItem = "deep-fryer-popup-not-item";
    private static readonly LocId PopupTooBig = "deep-fryer-popup-too-big";
    private static readonly LocId PopupFried = "deep-fryer-popup-fried";
    private static readonly LocId PopupNothingToFry = "deep-fryer-popup-nothing-to-fry";
    private static readonly LocId PopupUnloaded = "deep-fryer-popup-unloaded";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DeepFryerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DeepFryerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DeepFryerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DeepFryerComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DeepFryerComponent>();

        while (query.MoveNext(out var uid, out var fryer))
        {
            if (fryer.FryEnd is { } end && now >= end)
                FinishFrying((uid, fryer));
        }
    }

    private void OnInit(Entity<DeepFryerComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnActivate(Entity<DeepFryerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.BatchDone && ent.Comp.FryEnd == null)
        {
            TryUnload(ent, args.User);
            return;
        }

        TryStartFrying(ent, args.User);
    }

    private void OnInteractUsing(Entity<DeepFryerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.FryEnd != null)
        {
            _popup.PopupEntity(Loc.GetString(PopupBusy), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!TryComp<ItemComponent>(args.Used, out var item))
        {
            _popup.PopupEntity(Loc.GetString(PopupNotItem), ent, args.User);
            args.Handled = true;
            return;
        }

        if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
        {
            _popup.PopupEntity(Loc.GetString(PopupTooBig, ("item", args.Used)), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!TryGetStorage(ent, out var storage))
            return;

        if (storage.Count >= ent.Comp.Capacity)
        {
            _popup.PopupEntity(Loc.GetString(PopupFull), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!_hands.TryDropIntoContainer(args.User, args.Used, storage))
            return;

        ent.Comp.BatchDone = false;
        args.Handled = true;
    }

    private void OnPowerChanged(Entity<DeepFryerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        ent.Comp.FryEnd = null;
        StopFryingEffects(ent);
    }

    private void OnShutdown(Entity<DeepFryerComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.FryingStream);
    }

    private bool TryStartFrying(Entity<DeepFryerComponent> ent, EntityUid user)
    {
        if (!CanStartFrying(ent, user))
            return false;

        DoStartFrying(ent, user);
        return true;
    }

    private bool CanStartFrying(Entity<DeepFryerComponent> ent, EntityUid user, bool quiet = false)
    {
        if (!TryComp<ApcPowerReceiverComponent>(ent, out var power) || !power.Powered)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupNoPower), ent, user);

            return false;
        }

        if (ent.Comp.FryEnd != null)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupBusy), ent, user);

            return false;
        }

        if (!TryGetStorage(ent, out var storage) || storage.Count == 0)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(PopupEmpty), ent, user);

            return false;
        }

        return true;
    }

    private void DoStartFrying(Entity<DeepFryerComponent> ent, EntityUid user)
    {
        ent.Comp.FryEnd = _timing.CurTime + ent.Comp.FryTime;
        ent.Comp.BatchDone = false;

        _appearance.SetData(ent, DeepFryerVisuals.IsFrying, true);
        _audio.PlayPvs(ent.Comp.StartSound, ent);
        ent.Comp.FryingStream = _audio.PlayPvs(ent.Comp.FryingSound, ent,
            AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;

        NotifyFrying(ent, user);
    }

    private void NotifyFrying(Entity<DeepFryerComponent> ent, EntityUid user)
    {
        if (!TryGetStorage(ent, out var storage))
            return;

        foreach (var item in storage.ContainedEntities)
        {
            if (GetFryResult(ent.Comp, item) == null)
                continue;

            _popup.PopupEntity(Loc.GetString(PopupFried, ("item", item)), ent, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString(PopupNothingToFry), ent, user);
    }

    private void FinishFrying(Entity<DeepFryerComponent> ent)
    {
        ent.Comp.FryEnd = null;

        if (TryGetStorage(ent, out var storage))
        {
            foreach (var item in storage.ContainedEntities.ToArray())
            {
                FryItem(ent, item);
            }
        }

        ent.Comp.BatchDone = true;

        _audio.PlayPvs(ent.Comp.DoneSound, ent);
        StopFryingEffects(ent);
    }

    private void StopFryingEffects(Entity<DeepFryerComponent> ent)
    {
        _appearance.SetData(ent, DeepFryerVisuals.IsFrying, false);
        ent.Comp.FryingStream = _audio.Stop(ent.Comp.FryingStream);
    }

    private void FryItem(Entity<DeepFryerComponent> ent, EntityUid item)
    {
        if (GetFryResult(ent.Comp, item) is { } fried)
            ReplaceItem(ent, item, fried);
    }

    private EntProtoId? GetFryResult(DeepFryerComponent comp, EntityUid item)
    {
        if (MetaData(item).EntityPrototype?.ID is not { } source)
            return null;

        foreach (var recipeId in comp.Recipes)
        {
            if (!_prototype.TryIndex(recipeId, out var recipe) || recipe.Source.Id != source)
                continue;

            return _prototype.HasIndex(recipe.Result) ? recipe.Result : (EntProtoId?) null;
        }

        return null;
    }

    private void ReplaceItem(Entity<DeepFryerComponent> ent, EntityUid item, EntProtoId fried)
    {
        if (!TryGetStorage(ent, out var storage))
            return;

        var result = SpawnAttachedTo(fried, Transform(ent).Coordinates);

        if (!_container.Insert(result, storage))
        {
            Del(result);
            return;
        }

        Del(item);
    }

    private bool TryUnload(Entity<DeepFryerComponent> ent, EntityUid user)
    {
        if (!CanUnload(ent, user))
            return false;

        DoUnload(ent, user);
        return true;
    }

    private bool CanUnload(Entity<DeepFryerComponent> ent, EntityUid user, bool quiet = false)
    {
        if (TryGetStorage(ent, out var storage) && storage.Count > 0)
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(PopupEmpty), ent, user);

        return false;
    }

    private void DoUnload(Entity<DeepFryerComponent> ent, EntityUid user)
    {
        if (!TryGetStorage(ent, out var storage))
            return;

        _container.EmptyContainer(storage, destination: Transform(ent).Coordinates);
        ent.Comp.BatchDone = false;

        _popup.PopupEntity(Loc.GetString(PopupUnloaded), ent, user);
    }

    private bool TryGetStorage(Entity<DeepFryerComponent> ent, [NotNullWhen(true)] out BaseContainer? storage)
    {
        return _container.TryGetContainer(ent, ent.Comp.ContainerId, out storage);
    }
}
