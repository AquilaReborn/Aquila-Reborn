using Robust.Client.ResourceManagement;

namespace Content.Client.Paper.UI;

public sealed partial class PaperWindow
{
    private void ApplyReceiptStamp(PaperVisualsComponent visuals)
    {
        if (visuals.StampImagePath is { } stampImagePath)
        {
            PaperStampImage.Texture = _resCache.GetResource<TextureResource>(stampImagePath);
            PaperStampImage.Visible = true;
        }
        else
        {
            PaperStampImage.Texture = null;
            PaperStampImage.Visible = false;
        }

        PaperStampImage.ModulateSelfOverride = visuals.StampImageModulate;
    }
}
