using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.AutoCook;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.AutoCook;

/// <summary>
/// Превращает данные автоповара от сервера в локализованный текст.
/// </summary>
public sealed class AutoCookText(IPrototypeManager proto)
{
    public string ResultName(AutoCookRecipeKind kind, string result)
    {
        return kind == AutoCookRecipeKind.Reagent ? ReagentName(result) : EntityName(result);
    }

    public string IngredientText(AutoCookIngredient ingredient)
    {
        var name = ingredient.Reagent ? ReagentName(ingredient.Id) : EntityName(ingredient.Id);

        if (ingredient.Have is not { } have)
            return $"{name} — {FormatAmount(ingredient.Need)}";

        return ingredient.Reagent
            ? $"{name} — {FormatAmount(have)}/{FormatAmount(ingredient.Need)}"
            : $"{name} — {have.Int()}/{ingredient.Need.Int()}";
    }

    public string StockText(AutoCookStockEntry entry)
    {
        return entry.Reagent
            ? $"{ReagentName(entry.Id)} — {FormatAmount(entry.Amount)}"
            : $"{EntityName(entry.Id)} — ×{entry.Amount.Int()}";
    }

    public string StepText(AutoCookStepData step)
    {
        return step.Kind switch
        {
            AutoCookStepKind.Synthesize => Loc.GetString("autocook-step-synthesize",
                ("name", ReagentName(step.Subject)),
                ("amount", FormatAmount(step.Value))),
            AutoCookStepKind.TakeBuffer => Loc.GetString("autocook-step-take-buffer",
                ("name", ReagentName(step.Subject)),
                ("amount", FormatAmount(step.Value))),
            AutoCookStepKind.React => Loc.GetString("autocook-step-react", ("name", ReagentName(step.Subject))),
            AutoCookStepKind.Mixing => Loc.GetString("autocook-step-mixing", ("action", MixingVerb(step.Subject))),
            AutoCookStepKind.Heat => Loc.GetString("autocook-step-heat", ("temp", (int) step.Value)),
            AutoCookStepKind.Cool => Loc.GetString("autocook-step-cool", ("temp", (int) step.Value)),
            AutoCookStepKind.PrepareIngredients => Loc.GetString("autocook-step-prepare"),
            AutoCookStepKind.Cook => Loc.GetString("autocook-step-cook", ("name", EntityName(step.Subject))),
            AutoCookStepKind.Mix => Loc.GetString("autocook-step-mix", ("name", EntityName(step.Subject))),
            AutoCookStepKind.Slice => Loc.GetString("autocook-step-slice", ("name", EntityName(step.Subject))),
            AutoCookStepKind.Process => Loc.GetString("autocook-step-process", ("name", EntityName(step.Subject))),
            _ => step.Subject,
        };
    }

    public string ReagentName(string id)
    {
        return proto.TryIndex<ReagentPrototype>(id, out var reagent) ? reagent.LocalizedName : id;
    }

    public string EntityName(string id)
    {
        return proto.TryIndex<EntityPrototype>(id, out var entity) ? entity.Name : id;
    }

    private string MixingVerb(string id)
    {
        return proto.TryIndex<MixingCategoryPrototype>(id, out var category) ? Loc.GetString(category.VerbText) : id;
    }

    private static string FormatAmount(FixedPoint2 amount)
    {
        return FormatAmount(amount.Float());
    }

    private static string FormatAmount(float amount)
    {
        return MathF.Round(amount, 1).ToString("0.#");
    }
}
