// Работа выполнена Claude (Anthropic).
// SPDX-FileCopyrightText: 2026 GromPlay739
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Колонка SoT: проигрывает заданную музыку по взаимодействию (вкл/выкл) и сама включается
///     (с перезапуском трека, если уже играла) в начале битвы корабля, на котором стоит.
/// </summary>
[RegisterComponent]
public sealed partial class SotSpeakerComponent : Component
{
    /// <summary>Трек. Путь пишется от Resources, например /Audio/_Aquila/EventContent/Atlas739/athena_theme.ogg.</summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>Зацикливать трек. Если false, он сыграет один раз.</summary>
    [DataField]
    public bool Loop = true;

    /// <summary>Громкость в децибелах (0 - громкость файла, отрицательное - тише).</summary>
    [DataField]
    public float Volume = -4f;

    /// <summary>Дальность, на которой слышно музыку.</summary>
    [DataField]
    public float MaxDistance = 20f;

    /// <summary>Включаться самой, когда начинается битва корабля, на котором стоит колонка.</summary>
    [DataField]
    public bool PlayOnBattleStart = true;

    /// <summary>Сейчас играющий трек (рабочее состояние, заполняет сервер).</summary>
    [ViewVariables]
    public EntityUid? AudioStream;
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
