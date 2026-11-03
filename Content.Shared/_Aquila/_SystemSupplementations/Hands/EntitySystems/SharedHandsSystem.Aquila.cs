namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private const float FullHandsCrawlModifier = 0.5f;

    private static float GetCrawlHandsModifier(int freeHands, int totalHands)
    {
        var freeFraction = (float) freeHands / totalHands;
        return FullHandsCrawlModifier + (1f - FullHandsCrawlModifier) * freeFraction;
    }
}
