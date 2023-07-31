using System.Text;

namespace UORenderer;

public struct LandData
{
    private TileFlag m_Flags;

    public LandData(TileFlag flags)
    {
        m_Flags = flags;
    }

    public TileFlag Flags {
        get { return m_Flags; }
        set { m_Flags = value; }
    }
}

public struct ItemData
{
    private string m_Name;
    private TileFlag m_Flags;
    private byte m_Weight;
    private byte m_Quality;
    private byte m_Quantity;
    private byte m_Stack;
    private byte m_Value;
    private byte m_Height;

    public ItemData(string name, TileFlag flags, int weight, int quality, int quantity, int stack, int value, int height)
    {
        m_Name = name;
        m_Flags = flags;
        m_Weight = (byte)weight;
        m_Quality = (byte)quality;
        m_Quantity = (byte)quantity;
        m_Stack = (byte)stack;
        m_Value = (byte)value;
        m_Height = (byte)height;
    }

    public string Name {
        get { return m_Name; }
        set { m_Name = value; }
    }

    public TileFlag Flags {
        get { return m_Flags; }
        set { m_Flags = value; }
    }

    public bool Bridge {
        get { return (m_Flags & TileFlag.Bridge) != 0; }
        set {
            if (value)
                m_Flags |= TileFlag.Bridge;
            else
                m_Flags &= ~TileFlag.Bridge;
        }
    }

    public bool Impassable {
        get { return (m_Flags & TileFlag.Impassable) != 0; }
        set {
            if (value)
                m_Flags |= TileFlag.Impassable;
            else
                m_Flags &= ~TileFlag.Impassable;
        }
    }

    public bool Surface {
        get { return (m_Flags & TileFlag.Surface) != 0; }
        set {
            if (value)
                m_Flags |= TileFlag.Surface;
            else
                m_Flags &= ~TileFlag.Surface;
        }
    }

    public bool Wet {
        get { return (m_Flags & TileFlag.Wet) != 0; }
        set {
            if (value)
                m_Flags |= TileFlag.Wet;
            else
                m_Flags &= ~TileFlag.Wet;
        }
    }

    public int Weight {
        get { return m_Weight; }
        set { m_Weight = (byte)value; }
    }

    public int Quality {
        get { return m_Quality; }
        set { m_Quality = (byte)value; }
    }

    public int Quantity {
        get { return m_Quantity; }
        set { m_Quantity = (byte)value; }
    }

    public int Stack {
        get { return m_Stack; }
        set { m_Stack = (byte)value; }
    }

    public int Value {
        get { return m_Value; }
        set { m_Value = (byte)value; }
    }

    public int Height {
        get { return m_Height; }
        set { m_Height = (byte)value; }
    }

    public int CalcHeight {
        get {
            if ((m_Flags & TileFlag.Bridge) != 0)
                return m_Height / 2;
            else
                return m_Height;
        }
    }
}

[Flags]
public enum TileFlag : ulong
{
    /// <summary>
    ///     Nothing is flagged.
    /// </summary>
    None = 0,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Background = 1ul << 0,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Weapon = 1ul << 1,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Transparent = 1ul << 2,
    /// <summary>
    ///     The tile is rendered with partial alpha-transparency.
    /// </summary>
    Translucent = 1ul << 3,
    /// <summary>
    ///     The tile is a wall.
    /// </summary>
    Wall = 1ul << 4,
    /// <summary>
    ///     The tile can cause damage when moved over.
    /// </summary>
    Damaging = 1ul << 5,
    /// <summary>
    ///     The tile may not be moved over or through.
    /// </summary>
    Impassable = 1ul << 6,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Wet = 1ul << 7,
    /// <summary>
    ///     Unknown.
    /// </summary>
    Unknown1 = 1ul << 8,
    /// <summary>
    ///     The tile is a surface. It may be moved over, but not through.
    /// </summary>
    Surface = 1ul << 9,
    /// <summary>
    ///     The tile is a stair, ramp, or ladder.
    /// </summary>
    Bridge = 1ul << 10,
    /// <summary>
    ///     The tile is stackable
    /// </summary>
    Generic = 1ul << 11,
    /// <summary>
    ///     The tile is a window. Like <see cref="TileFlag.NoShoot" />, tiles with this flag block line of sight.
    /// </summary>
    Window = 1ul << 12,
    /// <summary>
    ///     The tile blocks line of sight.
    /// </summary>
    NoShoot = 1ul << 13,
    /// <summary>
    ///     For single-amount tiles, the string "a " should be prepended to the tile name.
    /// </summary>
    ArticleA = 1ul << 14,
    /// <summary>
    ///     For single-amount tiles, the string "an " should be prepended to the tile name.
    /// </summary>
    ArticleAn = 1ul << 15,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Internal = 1ul << 16,
    /// <summary>
    ///     The tile becomes translucent when walked behind. Boat masts also have this flag.
    /// </summary>
    Foliage = 1ul << 17,
    /// <summary>
    ///     Only gray pixels will be hued
    /// </summary>
    PartialHue = 1ul << 18,
    /// <summary>
    ///     Unknown.
    /// </summary>
    NoHouse = 1ul << 19,
    /// <summary>
    ///     The tile is a map--in the cartography sense. Unknown usage.
    /// </summary>
    Map = 1ul << 20,
    /// <summary>
    ///     The tile is a container.
    /// </summary>
    Container = 1ul << 21,
    /// <summary>
    ///     The tile may be equiped.
    /// </summary>
    Wearable = 1ul << 22,
    /// <summary>
    ///     The tile gives off light.
    /// </summary>
    LightSource = 1ul << 23,
    /// <summary>
    ///     The tile is animated.
    /// </summary>
    Animation = 1ul << 24,
    /// <summary>
    ///     Gargoyles can fly over
    /// </summary>
    NoDiagonal = 1ul << 25,
    /// <summary>
    ///     Unknown.
    /// </summary>
    Unknown2 = 1ul << 26,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    Armor = 1ul << 27,
    /// <summary>
    ///     The tile is a slanted roof.
    /// </summary>
    Roof = 1ul << 28,
    /// <summary>
    ///     The tile is a door. Tiles with this flag can be moved through by ghosts and GMs.
    /// </summary>
    Door = 1ul << 29,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    StairBack = 1ul << 30,
    /// <summary>
    ///     Not yet documented.
    /// </summary>
    StairRight = 1ul << 31,
    /// <summary>
    ///     Blend Alphas, tile blending.
    /// </summary>
    AlphaBlend = 1ul << 32,
    /// <summary>
    ///     Uses new art style?
    /// </summary>
    UseNewArt = 1ul << 33,
    /// <summary>
    ///     Has art being used?
    /// </summary>
    ArtUsed = 1ul << 34,
    /// <summary>
    ///     Prevents clipping under tiles at the same z
    /// </summary>
    NoClip = 1ul << 35,
    /// <summary>
    ///     Disallow shadow on this tile, lightsource? lava?
    /// </summary>
    NoShadow = 1ul << 36,
    /// <summary>
    ///     Let pixels bleed in to other tiles? Is this Disabling Texture Clamp?
    /// </summary>
    PixelBleed = 1ul << 37,
    /// <summary>
    ///     Play tile animation once.
    /// </summary>
    PlayAnimOnce = 1ul << 38,
    /// <summary>
    ///     Unused
    /// </summary>
    _40 = 1ul << 39,
    /// <summary>
    ///     Movable multi? Cool ships and vehicles etc?
    /// </summary>
    MultiMovable = 1ul << 40,
    /// <summary>
    ///     Unused
    /// </summary>
    _42 = 1ul << 41,
    /// <summary>
    ///     Unused
    /// </summary>
    _43 = 1ul << 42,
    /// <summary>
    ///     Unused
    /// </summary>
    _44 = 1ul << 43,
    /// <summary>
    ///     Unused
    /// </summary>
    _45 = 1ul << 44,
    /// <summary>
    ///     Unused
    /// </summary>
    _46 = 1ul << 45,
    /// <summary>
    ///     Unused
    /// </summary>
    _47 = 1ul << 46,
    /// <summary>
    ///     Unused
    /// </summary>
    _48 = 1ul << 47,
    /// <summary>
    ///     Unused
    /// </summary>
    _49 = 1ul << 48,
    /// <summary>
    ///     Prevents art rendering
    /// </summary>
    NoDraw = 1ul << 49,
    /// <summary>
    ///     The hue of this object will be used to color its light effect
    /// </summary>
    HuedLight = 1ul << 50,
    /// <summary>
    ///     Unused
    /// </summary>
    _52 = 1ul << 51,
    /// <summary>
    ///     Unused
    /// </summary>
    _53 = 1ul << 52,
    /// <summary>
    ///     Unused
    /// </summary>
    _54 = 1ul << 53,
    /// <summary>
    ///     Unused
    /// </summary>
    _55 = 1ul << 54,
    /// <summary>
    ///     Unused
    /// </summary>
    _56 = 1ul << 55,
    /// <summary>
    ///     Unused
    /// </summary>
    _57 = 1ul << 56,
    /// <summary>
    ///     Unused
    /// </summary>
    _58 = 1ul << 57,
    /// <summary>
    ///     Unused
    /// </summary>
    _59 = 1ul << 58,
    /// <summary>
    ///     Unused
    /// </summary>
    _60 = 1ul << 59,
    /// <summary>
    ///     Unused
    /// </summary>
    _61 = 1ul << 60,
    /// <summary>
    ///     Unused
    /// </summary>
    _62 = 1ul << 61,
    /// <summary>
    ///     Unused
    /// </summary>
    _63 = 1ul << 62,
    /// <summary>
    ///     Unused
    /// </summary>
    _64 = 1ul << 63
}

public static class TileData
{
    private static LandData[] m_LandData;
    private static ItemData[] m_ItemData;

    public static LandData[] LandTable {
        get {
            return m_LandData;
        }
    }

    public static ItemData[] ItemTable {
        get {
            return m_ItemData;
        }
    }

    private static int m_MaxLandValue;
    private static int m_MaxItemValue;

    public static int MaxLandValue {
        get { return m_MaxLandValue; }
    }

    public static int MaxItemValue {
        get { return m_MaxItemValue; }
    }

    private static byte[] m_StringBuffer = new byte[20];

    private static string ReadNameString(BinaryReader bin)
    {
        bin.Read(m_StringBuffer, 0, 20);

        int count;

        for (count = 0; count < 20 && m_StringBuffer[count] != 0; ++count) ;

        return Encoding.ASCII.GetString(m_StringBuffer, 0, count);
    }

    static TileData()
    {
        string filePath = UORenderer.CurrentProject.GetFullPath("tiledata.mul");

        if (File.Exists(filePath))
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryReader bin = new BinaryReader(fs);

                m_LandData = new LandData[0x4000];

                for (int i = 0; i < 0x4000; ++i)
                {
                    if (i == 1 || (i > 0 && (i & 0x1F) == 0))
                    {
                        bin.ReadInt32(); // header
                    }

                    TileFlag flags = (TileFlag)bin.ReadInt64();
                    bin.ReadInt16(); // skip 2 bytes -- textureID

                    ReadNameString(bin);
                    m_LandData[i] = new LandData(flags);
                }

                m_ItemData = new ItemData[0x10000];

                for (int i = 0; i < 0x10000; ++i)
                {
                    if ((i & 0x1F) == 0)
                    {
                        bin.ReadInt32(); // header
                    }

                    TileFlag flags = (TileFlag)bin.ReadInt64();
                    int weight = bin.ReadByte();
                    int quality = bin.ReadByte();
                    bin.ReadInt16();
                    bin.ReadByte();
                    int quantity = bin.ReadByte();
                    bin.ReadInt32();
                    int stack = bin.ReadByte();
                    int value = bin.ReadByte();
                    int height = bin.ReadByte();

                    m_ItemData[i] = new ItemData(ReadNameString(bin), flags, weight, quality, quantity, stack, value, height);
                }
            }

            m_MaxLandValue = m_LandData.Length - 1;
            m_MaxItemValue = m_ItemData.Length - 1;
        }
        else
        {
            throw new Exception($"TileData: {filePath} not found");
        }
    }
}
