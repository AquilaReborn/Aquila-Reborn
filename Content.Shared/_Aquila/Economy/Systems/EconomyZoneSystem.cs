using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared.Popups;
using Robust.Shared.Configuration;

namespace Content.Shared._Aquila.Economy.Systems;

public sealed class EconomyZoneSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly LocId FreeZoneMessage = "holochip-free-zone";
    private static readonly LocId OfflineMessage = "bank-offline";

    /// <summary>
    /// Проверяет, включена ли экономика на сервере.
    /// </summary>
    public bool IsEconomyEnabled()
    {
        return _cfg.GetCVar(AquilaCVars.Economy);
    }

    /// <summary>
    /// Проверяет, действует ли экономика там, где находится сущность: она включена на сервере, а грид и карта не отмечены как зона без экономики.
    /// </summary>
    public bool IsEconomyActive(EntityUid uid)
    {
        if (!IsEconomyEnabled())
            return false;

        var xform = Transform(uid);
        return !HasComp<EconomyFreeZoneComponent>(xform.GridUid) && !HasComp<EconomyFreeZoneComponent>(xform.MapUid);
    }

    /// <summary>
    /// Проверяет, действует ли экономика там, где стоит устройство, и показывает игроку попап, если она не действует.
    /// </summary>
    /// <param name="user">Игрок, который выполняет действие.</param>
    /// <param name="source">Устройство, с которым работает игрок: зона берётся по нему, а если у него нет трансформа, по игроку.</param>
    /// <param name="freeZoneMessage">Сообщение для зоны без экономики. Null: общее сообщение.</param>
    /// <returns>True, если экономика действует.</returns>
    public bool TryCheckActive(EntityUid user, EntityUid source, LocId? freeZoneMessage = null)
    {
        if (IsEconomyActive(GetZoneSource(user, source)))
            return true;

        _popup.PopupEntity(Loc.GetString(IsEconomyEnabled() ? freeZoneMessage ?? FreeZoneMessage : OfflineMessage), source, user);
        return false;
    }

    /// <summary>
    /// Проверяет, отмечена ли карта или грид как зона без экономики.
    /// </summary>
    public bool IsFreeZone(EntityUid zone)
    {
        return HasComp<EconomyFreeZoneComponent>(zone);
    }

    /// <summary>
    /// Отмечает карту или грид как зону без экономики или снимает отметку.
    /// </summary>
    public void SetFreeZone(EntityUid zone, bool free)
    {
        if (free)
            EnsureComp<EconomyFreeZoneComponent>(zone);
        else
            RemComp<EconomyFreeZoneComponent>(zone);
    }

    private EntityUid GetZoneSource(EntityUid user, EntityUid source)
    {
        return TryComp(source, out TransformComponent? xform) && (xform.GridUid != null || xform.MapUid != null)
            ? source
            : user;
    }
}
