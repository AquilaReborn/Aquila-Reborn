using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Audio;

/// <summary>
/// Настройки эха в помещениях: как оценивается помещение вокруг слушателя и какие пресеты реверберации смешиваются.
/// </summary>
[Prototype]
public sealed partial class RoomReverbPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Сколько тайлов максимум обходится при оценке помещения.
    /// </summary>
    [DataField]
    public int MaxTiles = 400;

    /// <summary>
    /// Как часто, в секундах, заново оценивается помещение.
    /// </summary>
    [DataField]
    public float AnalyzeInterval = 0.5f;

    /// <summary>
    /// Как часто, в секундах, обновляется эффект и подключаются новые звуки.
    /// </summary>
    [DataField]
    public float ApplyInterval = 0.1f;

    /// <summary>
    /// Скорость плавного перехода между настройками эха.
    /// </summary>
    [DataField]
    public float SmoothRate = 4f;

    /// <summary>
    /// Общая сила эха относительно пресетов.
    /// </summary>
    [DataField]
    public float Wetness = 0.7f;

    /// <summary>
    /// Насколько сильно захламлённость помещения глушит эхо, от 0 до 1.
    /// </summary>
    [DataField]
    public float ClutterDamping = 0.6f;

    /// <summary>
    /// Сколько твёрдых объектов на тайл считается полностью захламлённым помещением.
    /// </summary>
    [DataField]
    public float ClutterDensity = 0.5f;

    /// <summary>
    /// Площадь в тайлах, до которой помещение считается маленькой комнатой.
    /// </summary>
    [DataField]
    public float SmallRoomTiles = 9f;

    /// <summary>
    /// Площадь в тайлах, начиная с которой помещение считается большим залом.
    /// </summary>
    [DataField]
    public float HallTiles = 300f;

    /// <summary>
    /// Средняя ширина в тайлах, начиная с которой помещение перестаёт быть коридором.
    /// </summary>
    [DataField]
    public float CorridorMaxWidth = 3f;

    /// <summary>
    /// На сколько тайлов ширины коридор плавно переходит в комнату.
    /// </summary>
    [DataField]
    public float CorridorWidthFade = 1.5f;

    /// <summary>
    /// Длина в тайлах, начиная с которой узкое помещение может считаться коридором.
    /// </summary>
    [DataField]
    public float CorridorMinLength = 5f;

    /// <summary>
    /// На сколько тайлов длины узкое помещение плавно становится коридором.
    /// </summary>
    [DataField]
    public float CorridorLengthFade = 5f;

    /// <summary>
    /// Длина в тайлах, при которой коридор звучит как длинный проход.
    /// </summary>
    [DataField]
    public float LongPassageLength = 25f;

    /// <summary>
    /// Дальше какого расстояния в тайлах от слушателя новые звуки не получают эхо.
    /// </summary>
    [DataField]
    public float MaxSourceDistance = 12f;

    /// <summary>
    /// Пресет маленькой комнаты.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AudioPresetPrototype> SmallRoom;

    /// <summary>
    /// Пресет комнаты средних размеров.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AudioPresetPrototype> MediumRoom;

    /// <summary>
    /// Пресет большого зала.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AudioPresetPrototype> Hall;

    /// <summary>
    /// Пресет короткого коридора.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AudioPresetPrototype> ShortPassage;

    /// <summary>
    /// Пресет длинного коридора.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AudioPresetPrototype> LongPassage;
}
