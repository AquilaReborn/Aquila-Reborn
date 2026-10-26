using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Changeling;

/// <summary>
/// Отмечает истинную форму генокрада. Пока она активна, остальные способности генокрада недоступны.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingTrueFormComponent : Component
{
    /// <summary>
    /// Сколько генокрад будет оглушён после возвращения в своё тело.
    /// </summary>
    [DataField]
    public TimeSpan RevertStunDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Рёв при обращении в истинную форму.
    /// </summary>
    [DataField]
    public SoundSpecifier RoarSound = new SoundCollectionSpecifier("ChangelingTrueFormRoar");

    /// <summary>
    /// Эффект волны искажения, который появляется при рёве.
    /// </summary>
    [DataField]
    public EntProtoId RoarEffect = "EffectChangelingRoarWave";

    /// <summary>
    /// Радиус волны, которая сбивает с ног и отбрасывает существ при обращении.
    /// </summary>
    [DataField]
    public float WaveRange = 2.5f;

    /// <summary>
    /// Сколько лежат сбитые волной существа.
    /// </summary>
    [DataField]
    public TimeSpan WaveKnockdownDuration = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// На сколько тайлов волна отбрасывает существ.
    /// </summary>
    [DataField]
    public float WaveThrowDistance = 1.5f;

    /// <summary>
    /// Скорость отбрасывания волной.
    /// </summary>
    [DataField]
    public float WaveThrowSpeed = 6f;

    /// <summary>
    /// Радиус, в котором у игроков трясётся экран от рёва.
    /// </summary>
    [DataField]
    public float ShakeRange = 7f;
}
