// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Kakila.CCVars;

[CVarDefs]
public sealed class KakilaCVars
{
    /// <summary>
    ///     Список ID пресетов через запятую, при которых работают команды sotroundprepare и sotroundstart.
    ///     Пустая строка - команды работают при любом пресете.
    ///     Меняется на лету: cvar kakila.sot_allowed_presets "SoT,Extended"
    /// </summary>
    public static readonly CVarDef<string> SotAllowedPresets =
        CVarDef.Create("kakila.sot_allowed_presets", "SoT", CVar.SERVERONLY | CVar.ARCHIVE);
}
