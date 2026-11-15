namespace Content.Goobstation.Shared.Xenobiology.Components;

public sealed partial class CorpseEaterComponent
{
    /// <summary>
    /// Пауза, после которой NPC снова ищет труп и пробует его есть, если прошлая попытка сорвалась.
    /// </summary>
    [DataField]
    public TimeSpan NpcRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Время, раньше которого NPC не ищет трупы и не начинает новую попытку.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextNpcAttempt;
}
