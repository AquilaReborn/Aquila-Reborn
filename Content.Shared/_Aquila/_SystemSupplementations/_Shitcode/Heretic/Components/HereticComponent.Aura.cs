namespace Content.Shared.Heretic;

public sealed partial class HereticComponent
{
    /// <summary>
    /// How many knowledge points the heretic has gained this round in total. The value only grows.
    /// </summary>
    [ViewVariables]
    public float TotalKnowledgeGained;

    /// <summary>
    /// Threshold of total knowledge points after which the heretic gets a visible aura.
    /// </summary>
    [DataField]
    public float PointsToAura = 5f;

    /// <summary>
    /// Whether the heretic has taken the feast of owls; after that the aura never appears again.
    /// </summary>
    [ViewVariables]
    public bool FeastOfOwlsUsed;
}
