namespace Content.Shared.Heretic;

public sealed partial class HereticComponent
{
    /// <summary>
    /// Сколько очков знаний еретик набрал за раунд суммарно. Значение только растёт.
    /// </summary>
    [ViewVariables]
    public float TotalKnowledgeGained;

    /// <summary>
    /// Порог суммарно набранных очков знаний, после которого у еретика появляется видимая аура.
    /// </summary>
    [DataField]
    public float PointsToAura = 8f;

    /// <summary>
    /// Принимал ли еретик сов: после этого аура не появляется больше никогда.
    /// </summary>
    [ViewVariables]
    public bool FeastOfOwlsUsed;
}
