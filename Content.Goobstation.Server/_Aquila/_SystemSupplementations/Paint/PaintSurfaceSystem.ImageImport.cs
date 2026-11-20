using Content.Goobstation.Shared.Paint;
using Content.Shared._Aquila.Images.Types;
using SixLabors.ImageSharp;

namespace Content.Goobstation.Server.Paint;

public sealed partial class PaintSurfaceSystem
{
    private void InitializeAquila()
    {
        Subs.BuiEvents<PaintSurfaceComponent>(PaintUiKey.Key,
            subs => subs.Event<PaintImportMessage>(OnAquilaImport));
    }

    private void OnAquilaImport(Entity<PaintSurfaceComponent> ent, ref PaintImportMessage args)
    {
        if (ent.Comp.Finished
            || args.Data.Length == 0
            || !ImageLimits.Paint.AllowsLength(args.Data.Length))
            return;

        PaintImage image;
        try
        {
            image = PaintImageImporter.Import(args.Data, ent.Comp.Size, ent.Comp.Background);
        }
        catch (Exception e) when (e is ImageFormatException or NotSupportedException)
        {
            return;
        }

        SetImage(ent, image);
    }
}
