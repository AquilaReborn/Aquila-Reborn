// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Колонка SoT: музыка по взаимодействию (вкл/выкл) и автозапуск (с перезапуском трека)
///     в начале битвы корабля, на котором стоит колонка.
///
///     Музыку проигрывает КЛИЕНТ: она звучит равномерно (не затухает с расстоянием) только у тех игроков,
///     которые стоят на том же шаттле (гриде), что и колонка. Сервер лишь хранит состояние
///     (играет ли колонка и когда запущен трек), клиенты по нему синхронизируют позицию в треке.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SotSpeakerComponent : Component
{
    /// <summary>
    ///     Трек. Только путь (SoundPathSpecifier), например /Audio/_Aquila/SoT/athena_theme.ogg.
    ///     Набор звуков (collection) не поддерживается: у каждого клиента выбрался бы свой трек.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>Зацикливать трек. Если false, он сыграет один раз и колонка выключится.</summary>
    [DataField]
    public bool Loop = true;

    /// <summary>Громкость в децибелах (0 - громкость файла, отрицательное - тише).</summary>
    [DataField]
    public float Volume = -4f;

    /// <summary>Включаться самой, когда начинается битва корабля, на котором стоит колонка.</summary>
    [DataField]
    public bool PlayOnBattleStart = true;

    /// <summary>Играет ли колонка сейчас.</summary>
    [DataField, AutoNetworkedField]
    public bool Playing;

    /// <summary>Время сервера (CurTime), когда трек запущен. Клиенты считают по нему позицию в треке.</summary>
    [AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>Когда сыграет и выключится трек без зацикливания (только сервер).</summary>
    [ViewVariables]
    public TimeSpan? EndsAt;
}

[Serializable, NetSerializable]
public enum SotSpeakerVisuals : byte
{
    /// <summary>bool: колонка играет.</summary>
    Playing
}

[Serializable, NetSerializable]
public enum SotSpeakerVisualLayers : byte
{
    Base
}
