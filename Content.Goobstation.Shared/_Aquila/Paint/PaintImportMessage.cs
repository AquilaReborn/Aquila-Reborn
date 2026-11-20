using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Paint;

/// <summary>
/// Requests the server to import PNG data into the paint canvas.
/// </summary>
[Serializable, NetSerializable]
public sealed class PaintImportMessage(byte[] data) : BoundUserInterfaceMessage
{
    /// <summary>
    /// PNG data selected by the client.
    /// </summary>
    public readonly byte[] Data = data;
}
