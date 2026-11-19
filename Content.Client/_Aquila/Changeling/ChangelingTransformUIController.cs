using Content.Client._Aquila.Changeling.Types;
using Content.Client.Humanoid;
using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Goobstation.Shared._Aquila.Changeling.Types;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Changeling;

/// <summary>
///     Builds changeling form menus and manages their client-only preview dolls.
/// </summary>
public sealed class ChangelingTransformUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    ///     Rebuilds one interface instance from the server's stored form list.
    /// </summary>
    public void UpdateMenu(ChangelingTransformMenuSession session, ChangelingTransformState state)
    {
        ClearMenu(session);

        var options = new List<RadialMenuOptionBase>(state.Forms.Count);
        foreach (var form in state.Forms)
        {
            Color? background = null;
            Color? hoverBackground = null;

            if (form.Current)
            {
                background = Palettes.Green.Element;
                hoverBackground = Palettes.Green.HoveredElement;
            }
            else if (form.Selected)
            {
                background = Palettes.Amber.Element;
                hoverBackground = Palettes.Amber.HoveredElement;
            }

            var preview = CreatePreview(form.Appearance);
            session.Previews.Add(preview);
            options.Add(new RadialMenuActionOption<int>(session.SelectForm, form.Index)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(preview),
                ToolTip = form.Name,
                BackgroundColor = background,
                HoverBackgroundColor = hoverBackground,
            });
        }

        session.Menu.SetButtons(options);
    }

    /// <summary>
    ///     Removes one menu's buttons and deletes only its local preview dolls.
    /// </summary>
    public void ClearMenu(ChangelingTransformMenuSession session)
    {
        session.Menu.SetButtons(Array.Empty<RadialMenuOptionBase>());
        foreach (var preview in session.Previews)
            EntityManager.DeleteEntity(preview);

        session.Previews.Clear();
    }

    private EntityUid CreatePreview(ChangelingTransformAppearance appearance)
    {
        var preview = EntityManager.SpawnEntity(_prototypes.Index(appearance.Species).DollPrototype, MapCoordinates.Nullspace);
        var humanoid = EntityManager.GetComponent<HumanoidAppearanceComponent>(preview);
        humanoid.Species = appearance.Species;
        humanoid.Sex = appearance.Sex;
        humanoid.Gender = appearance.Gender;
        humanoid.Age = appearance.Age;
        humanoid.Height = appearance.Height;
        humanoid.Width = appearance.Width;
        humanoid.SkinColor = appearance.SkinColor;
        humanoid.EyeColor = appearance.EyeColor;
        humanoid.MarkingSet = new MarkingSet(appearance.MarkingSet);
        humanoid.CustomBaseLayers = new(appearance.CustomBaseLayers);

        EntityManager.System<HumanoidAppearanceSystem>().UpdateSprite((preview, humanoid, EntityManager.GetComponent<SpriteComponent>(preview)));
        return preview;
    }
}
