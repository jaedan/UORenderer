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
    None = 0x00000000,
    Background = 0x00000001,
    Weapon = 0x00000002,
    Transparent = 0x00000004,
    Translucent = 0x00000008,
    Wall = 0x00000010,
    Damaging = 0x00000020,
    Impassable = 0x00000040,
    Wet = 0x00000080,
    Unknown1 = 0x00000100,
    Surface = 0x00000200,
    Bridge = 0x00000400,
    Generic = 0x00000800,
    Window = 0x00001000,
    NoShoot = 0x00002000,
    ArticleA = 0x00004000,
    ArticleAn = 0x00008000,
    Internal = 0x00010000,
    Foliage = 0x00020000,
    PartialHue = 0x00040000,
    Unknown2 = 0x00080000,
    Map = 0x00100000,
    Container = 0x00200000,
    Wearable = 0x00400000,
    LightSource = 0x00800000,
    Animation = 0x01000000,
    NoDiagonal = 0x02000000,
    Unknown3 = 0x04000000,
    Armor = 0x08000000,
    Roof = 0x10000000,
    Door = 0x20000000,
    StairBack = 0x40000000,
    StairRight = 0x80000000,

    HS33 = 0x0000000100000000,
    HS34 = 0x0000000200000000,
    HS35 = 0x0000000400000000,
    HS36 = 0x0000000800000000,
    HS37 = 0x0000001000000000,
    HS38 = 0x0000002000000000,
    HS39 = 0x0000004000000000,
    HS40 = 0x0000008000000000,
    HS41 = 0x0000010000000000,
    HS42 = 0x0000020000000000,
    HS43 = 0x0000040000000000,
    HS44 = 0x0000080000000000,
    HS45 = 0x0000100000000000,
    HS46 = 0x0000200000000000,
    HS47 = 0x0000400000000000,
    HS48 = 0x0000800000000000,
    HS49 = 0x0001000000000000,
    HS50 = 0x0002000000000000,
    HS51 = 0x0004000000000000,
    HS52 = 0x0008000000000000,
    HS53 = 0x0010000000000000,
    HS54 = 0x0020000000000000,
    HS55 = 0x0040000000000000,
    HS56 = 0x0080000000000000,
    HS57 = 0x0100000000000000,
    HS58 = 0x0200000000000000,
    HS59 = 0x0400000000000000,
    HS60 = 0x0800000000000000,
    HS61 = 0x1000000000000000,
    HS62 = 0x2000000000000000,
    HS63 = 0x4000000000000000,
    HS64 = 0x8000000000000000
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
