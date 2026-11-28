using Content.Server._Aquila.Heretic;
using Content.Server._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

public sealed partial class MoonAscensionSystem
{
    [Dependency] private readonly MansusRealmSystem _mansusRealm = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private static readonly EntProtoId BoundAmulet = "ClothingNeckMoonAmuletBound";

    /// <summary>
    /// Снимает с цели лунатизм библией: возвращает фракции, убирает роль, статус и привязанный амулет,
    /// роняет жертву и отправляет ей сообщение о возвращении из Мансуса.
    /// </summary>
    /// <param name="target">Кого освобождают от безумия.</param>
    /// <param name="user">Кто проводит очищение библией.</param>
    /// <returns><see langword="true"/>, если цель была лунатиком и очищение прошло.</returns>
    public bool TryCleanseLunatic(EntityUid target, EntityUid user)
    {
        if (!TryComp<MoonLunaticComponent>(target, out var lunatic))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (_mind.TryGetMind(target, out var mindId, out _))
            _role.MindRemoveRole<MoonLunaticRoleComponent>(mindId);

        _status.TryRemoveStatusEffect(target, LunaticInsanityStatus);

        if (FindBoundAmulet(target) is { } amulet)
            QueueDel(amulet);

        RemComp<MoonLunaticComponent>(target);
        RemComp<HereticMinionComponent>(target);

        _audio.PlayPvs(lunatic.CleanseSound, target);
        _stun.KnockdownOrStun(target, TimeSpan.FromSeconds(2), refresh: true);
        _mansusRealm.SendReturnMessage(target);
        return true;
    }

    private EntityUid? FindBoundAmulet(EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, NeckSlot, out var neck) && Prototype(neck.Value)?.ID == BoundAmulet.Id)
            return neck.Value;

        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (Prototype(held)?.ID == BoundAmulet.Id)
                return held;
        }

        return null;
    }
}
