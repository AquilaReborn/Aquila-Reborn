namespace Content.Client._Aquila.Heretic.Helpers;

/// <summary>
/// Общая плавность эффектов Мансуса: плавное появление, удержание и плавное исчезновение по времени.
/// </summary>
public static class MansusFadeHelper
{
    /// <summary>
    /// Прозрачность эффекта от 0 до 1: растёт от <paramref name="start"/> за <paramref name="fadeIn"/>,
    /// а если задано <paramref name="end"/>, то плавно гаснет за <paramref name="fadeOut"/> после него.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    /// <param name="start">Момент, с которого эффект начинает появляться.</param>
    /// <param name="fadeIn">Сколько длится плавное появление.</param>
    /// <param name="end">Момент, после которого эффект начинает гаснуть, или null, если гаснуть не нужно.</param>
    /// <param name="fadeOut">Сколько длится плавное исчезновение.</param>
    /// <returns>Прозрачность эффекта от 0 до 1.</returns>
    public static float GetAlpha(TimeSpan now, TimeSpan start, TimeSpan fadeIn, TimeSpan? end = null, TimeSpan fadeOut = default)
    {
        var alpha = Ease(Progress(now, start, fadeIn));
        if (end is not { } endTime || fadeOut <= TimeSpan.Zero)
            return alpha;

        return MathF.Min(alpha, 1f - Ease(Progress(now, endTime, fadeOut)));
    }

    /// <summary>
    /// Доля времени, прошедшая с указанного момента, от 0 до 1.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    /// <param name="start">Момент, с которого считается доля.</param>
    /// <param name="duration">Промежуток, за который доля проходит от 0 до 1.</param>
    /// <returns>Доля времени от 0 до 1.</returns>
    public static float Progress(TimeSpan now, TimeSpan start, TimeSpan duration)
    {
        return duration <= TimeSpan.Zero ? 1f : Math.Clamp((float) ((now - start) / duration), 0f, 1f);
    }

    /// <summary>
    /// Плавная кривая от 0 до 1 без рывков на краях.
    /// </summary>
    /// <param name="progress">Доля времени от 0 до 1.</param>
    /// <returns>Сглаженное значение от 0 до 1.</returns>
    public static float Ease(float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        return progress * progress * (3f - 2f * progress);
    }
}
