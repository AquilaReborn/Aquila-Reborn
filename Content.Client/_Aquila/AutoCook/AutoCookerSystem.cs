using Content.Shared._Aquila.AutoCook;

namespace Content.Client._Aquila.AutoCook;

public sealed class AutoCookerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<NetEntity, List<AutoCookRecipeEntry>> _recipes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AutoCookerRecipesEvent>(OnRecipes);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _recipes.Clear();
    }

    private void OnRecipes(AutoCookerRecipesEvent ev)
    {
        _recipes[ev.Cooker] = ev.Recipes;

        if (TryGetEntity(ev.Cooker, out var uid)
            && _ui.TryGetOpenUi<AutoCookerBoundUserInterface>(uid.Value, AutoCookerUiKey.Key, out var bui))
            bui.UpdateRecipes(ev.Recipes);
    }

    public List<AutoCookRecipeEntry>? GetRecipes(NetEntity cooker)
    {
        return _recipes.GetValueOrDefault(cooker);
    }

    public void ForgetRecipes(NetEntity cooker)
    {
        _recipes.Remove(cooker);
    }
}
