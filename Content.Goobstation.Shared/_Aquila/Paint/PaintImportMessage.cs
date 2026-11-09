using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Paint;

[Serializable, NetSerializable]
public sealed class PaintImportMessage(byte[] data) : BoundUserInterfaceMessage
{
    public const int MaxImportBytes = 2 * 1024 * 1024;

    public readonly byte[] Data = data;
}
