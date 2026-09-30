// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Kakila.ShuttleLink;
using Content.Shared.Shuttles.Components; // FTLComponent; если не находится: Content.Server.Shuttles.Components
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Kakila.ShuttleLink;

public sealed class ShuttleLinkSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <summary>
    ///     Находит шаттл (грид) по ID. Ищет среди всех сущностей с ShuttleLinkComponent:
    ///     если компонент на гриде — берёт его, иначе берёт грид, на котором стоит сущность.
    /// </summary>
    public bool TryGetShuttle(string id, out EntityUid grid)
    {
        var query = EntityQueryEnumerator<ShuttleLinkComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var link, out var xform))
        {
            if (link.Id != id)
                continue;

            if (HasComp<MapGridComponent>(uid))
            {
                grid = uid;
                return true;
            }

            if (xform.GridUid is { } gridUid)
            {
                grid = gridUid;
                return true;
            }
        }

        grid = default;
        return false;
    }

    /// <summary>
    ///     Мгновенно телепортирует шаттл с данным ID в указанные координаты.
    /// </summary>
    /// <returns>false, если шаттл не найден, находится в FTL или целевая карта не существует.</returns>
    public bool TryTeleportShuttle(string id, MapCoordinates target, Angle? rotation = null)
    {
        if (!TryGetShuttle(id, out var grid))
            return false;

        // Не трогаем шаттл во время гиперпрыжка.
        if (HasComp<FTLComponent>(grid))
            return false;

        if (!_map.TryGetMap(target.MapId, out var mapUid))
            return false;

        _transform.SetCoordinates(grid, Transform(grid), new EntityCoordinates(mapUid.Value, target.Position), rotation: rotation);
        return true;
    }
}
