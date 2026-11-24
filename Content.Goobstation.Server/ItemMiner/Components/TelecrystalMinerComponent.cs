// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <57039557+Ilya246@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.ItemMiner;

/// <summary>
/// Майнер телекристаллов, который со временем объявляет станции о незаконной добыче.
/// </summary>
[RegisterComponent]
public sealed partial class TelecrystalMinerComponent : Component
{
    /// <summary>
    /// На какой стадии оповещения станции находится майнер.
    /// </summary>
    [DataField]
    public TCMinerStage NotifiedStage = TCMinerStage.Initial;

    /// <summary>
    /// После скольких добытых телекристаллов объявить станции о майнере.
    /// </summary>
    [DataField]
    public int AnnounceAt = 40;

    /// <summary>
    /// Ключ локализации первого объявления.
    /// </summary>
    [DataField]
    public LocId Announcement = "telecrystal-miner-announcement";

    /// <summary>
    /// После скольких добытых телекристаллов объявить станции о местоположении майнера.
    /// </summary>
    [DataField]
    public int LocationAt = 100;

    /// <summary>
    /// Ключ локализации второго объявления, принимает {$location}.
    /// </summary>
    [DataField]
    public LocId LocationAnnouncement = "telecrystal-miner-announcement2";

    /// <summary>
    /// Сколько телекристаллов уже добыто.
    /// </summary>
    [DataField]
    public int Accumulated = 0;
}

/// <summary>
/// Стадии оповещения станции о работе майнера телекристаллов.
/// </summary>
public enum TCMinerStage
{
    Initial,
    FirstAnnounced,
    LocationAnnounced
}
