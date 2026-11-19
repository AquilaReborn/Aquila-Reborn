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

    /// <summary>
    /// Громкость музыки, которая играет из станционного радио.
    /// </summary>
    public static readonly CVarDef<float> RadioMusicVolume =
        CVarDef.Create("aquila.radio_music_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Автоматически сохранять локальный реплей со старта раунда.
    /// </summary>
    public static readonly CVarDef<bool> AutoRecordReplay =
        CVarDef.Create("aquila.auto_record_replay", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Перемешивать возможные мутации слаймов: при спавне слайм получает набор из графа пород, перемешанного для его станции.
    /// </summary>
    public static readonly CVarDef<bool> XenobiologyShuffle =
        CVarDef.Create("aquila.xenobiology_shuffle", true, CVar.SERVERONLY);

    /// <summary>
    /// Включает экономику: банковские счета, рынок карго и личные заказы.
    /// </summary>
    public static readonly CVarDef<bool> Economy =
        CVarDef.Create("aquila.economy", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// ID активного сеттинга из прототипов AqSettingPrototype, который задаёт пул карт для раунда.
    /// </summary>
    public static readonly CVarDef<string> Setting =
        CVarDef.Create("aquila.setting", "SS14", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Включает пространственные звуковые эффекты: реверберацию помещений и приглушение звука в вакууме.
    /// </summary>
    public static readonly CVarDef<bool> AudioEnvironment =
        CVarDef.Create("aquila.audio_environment", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Громкость музыки, которую включают администраторы.
    /// </summary>
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

    /// <summary>
    /// Выводить в лог отладку предкритического состояния.
    /// </summary>
    public static readonly CVarDef<bool> PreCriticalDebug =
        CVarDef.Create("aquila.precrit_debug", false, CVar.SERVERONLY);

    /// <summary>
    ///     Whether changeling abilities require a number of absorbed entities before they can be used.
    /// </summary>
    public static readonly CVarDef<bool> ChangelingRequireAbsorbed =
        CVarDef.Create("aquila.changeling_require_absorbed", false, CVar.SERVER | CVar.REPLICATED);
}
