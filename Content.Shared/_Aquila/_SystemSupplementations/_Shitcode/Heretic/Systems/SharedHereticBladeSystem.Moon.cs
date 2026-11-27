using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Heretic.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract partial class SharedHereticBladeSystem
{
    [Dependency] protected readonly MoonSanitySystem _moonSanity = default!; // Aquila Change
    [Dependency] protected readonly StatusEffectsSystem _moonStatus = default!; // Aquila Change

    private void InitializeMoonBladeChannel()
    {
        SubscribeLocalEvent<HereticBladeComponent, GotEquippedHandEvent>(OnMoonBladeEquippedHand);
        SubscribeLocalEvent<HereticBladeComponent, GotUnequippedHandEvent>(OnMoonBladeUnequippedHand);
    }

    /// <summary>
    /// Точка расширения лунного клинка: бьёт по мозгу и накладывает галлюцинации. По безумным и лежачим целям бьёт сильнее.
    /// Реализация живёт на сервере, в классе HereticBladeSystem.
    /// </summary>
    /// <param name="target">Цель удара.</param>
    /// <param name="blade">Лунный клинок.</param>
    protected virtual void ApplyMoonBladeEffect(EntityUid target, HereticBladeComponent? blade) { }

    /// <summary>
    /// Переводит лунный клинок в режим канала через амулет: убирает физический урон и запоминает его для возврата.
    /// </summary>
    /// <param name="blade">Лунный клинок.</param>
    /// <param name="wearer">Носитель лунного амулета.</param>
    /// <param name="component">Компонент клинка, если уже получен вызывающим кодом.</param>
    public void ChannelMoonBlade(EntityUid blade, MoonAmuletWearerComponent wearer, HereticBladeComponent? component = null)
    {
        if (!Resolve(blade, ref component))
            return;

        if (!_net.IsServer
            || component.Path != "Moon"
            || !TryComp<MeleeWeaponComponent>(blade, out var melee)
            || HasComp<MoonBladeChannelComponent>(blade))
            return;

        var channel = EnsureComp<MoonBladeChannelComponent>(blade);
        channel.Damage = melee.Damage;
        channel.BrainMultiplier = wearer.ChannelBrainMultiplier;
        channel.InsanityDuration = wearer.ChannelInsanityDuration;
        channel.Effect = wearer.ChannelEffect;

        melee.Damage = new DamageSpecifier();
        DirtyField(blade, melee, nameof(MeleeWeaponComponent.Damage));
    }

    /// <summary>
    /// Возвращает лунному клинку исходный физический урон и снимает режим канала.
    /// </summary>
    /// <param name="blade">Лунный клинок.</param>
    /// <param name="component">Компонент клинка, если уже получен вызывающим кодом.</param>
    public void RestoreMoonChannel(EntityUid blade, HereticBladeComponent? component = null)
    {
        if (!Resolve(blade, ref component))
            return;

        if (!_net.IsServer || !TryComp<MoonBladeChannelComponent>(blade, out var channel))
            return;

        if (channel.Damage != null && TryComp<MeleeWeaponComponent>(blade, out var melee))
        {
            melee.Damage = channel.Damage;
            DirtyField(blade, melee, nameof(MeleeWeaponComponent.Damage));
        }

        RemComp<MoonBladeChannelComponent>(blade);
    }

    private void OnMoonBladeEquippedHand(Entity<HereticBladeComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<MoonAmuletWearerComponent>(args.User, out var wearer))
        {
            RestoreMoonChannel(ent.Owner, ent.Comp);
            return;
        }

        RestoreMoonChannel(ent.Owner, ent.Comp);
        ChannelMoonBlade(ent.Owner, wearer, ent.Comp);
    }

    private void OnMoonBladeUnequippedHand(Entity<HereticBladeComponent> ent, ref GotUnequippedHandEvent args)
    {
        RestoreMoonChannel(ent.Owner, ent.Comp);
    }

    private void ApplyMoonBladeChannel(EntityUid performer, IReadOnlyList<EntityUid> targets, Entity<HereticBladeComponent> blade)
    {
        if (!_net.IsServer
            || !HasComp<MoonAmuletWearerComponent>(performer)
            || !TryComp<MoonBladeChannelComponent>(blade, out var channel))
            return;

        foreach (var target in targets)
        {
            if (target == performer || !HasComp<MobStateComponent>(target))
                continue;

            if (channel.Damage != null)
            {
                var total = FixedPoint2.New(channel.Damage.GetTotal().Float() * channel.BrainMultiplier);
                _moonSanity.ApplyBrainDamage(target, total);
            }

            if (HasComp<StatusEffectsComponent>(target))
                _moonStatus.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, channel.InsanityDuration);

            if (channel.Effect is { } effect)
                SpawnAtPosition(effect, Transform(target).Coordinates);
        }
    }
}
