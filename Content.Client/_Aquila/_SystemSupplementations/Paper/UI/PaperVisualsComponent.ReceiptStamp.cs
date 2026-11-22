using Robust.Shared.Utility;

namespace Content.Client.Paper.UI;

public sealed partial class PaperVisualsComponent
{
    /// <summary>
    /// Путь к картинке печати, которая рисуется в правом нижнем углу листа бумаги.
    /// </summary>
    [DataField]
    public ResPath? StampImagePath;

    /// <summary>
    /// Цвет, которым тонируется картинка печати.
    /// </summary>
    [DataField]
    public Color StampImageModulate = Color.White;
}
