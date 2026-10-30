// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Aquila.CCVars;

[CVarDefs]
public sealed class AquilaCVars
{
    /// <summary>
    ///     Список ID пресетов через запятую, при которых работают команды sotroundprepare и sotroundstart.
    ///     Пустая строка - команды работают при любом пресете.
    ///     Меняется на лету: cvar aquila.sot_allowed_presets "SoT,Extended"
    /// </summary>
    public static readonly CVarDef<string> SotAllowedPresets =
        CVarDef.Create("aquila.sot_allowed_presets", "SoT", CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> RadioMusicVolume =
        CVarDef.Create("aquila.radio_music_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> XenobiologyShuffle =
        CVarDef.Create("aquila.xenobiology_shuffle", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> Economy =
        CVarDef.Create("aquila.economy", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> Setting =
        CVarDef.Create("aquila.setting", "SS14", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> AudioEnvironment =
        CVarDef.Create("aquila.audio_environment", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> AdminMusicVolume =
        CVarDef.Create("aquila.admin_music_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Рисовать ли декоративное свечение светильников.
    /// </summary>
    public static readonly CVarDef<bool> LightGlow =
        CVarDef.Create("aquila.light_glow", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Яркость декоративного свечения светильников, от 0 до 1.
    /// </summary>
    public static readonly CVarDef<float> LightGlowIntensity =
        CVarDef.Create("aquila.light_glow_intensity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
