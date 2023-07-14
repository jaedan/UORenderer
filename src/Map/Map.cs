namespace UORenderer;

public struct LandTile
{
    public ushort ID;
    public sbyte Z;

    /*
    public LandData Data => TileData.LandTable[ID & TileData.MaxLandValue];
    */
}

public struct StaticTile
{
    public ushort ID;
    public int X;
    public int Y;
    public int Z;
    public ushort Hue;

    /*
    public ItemData Data => TileData.ItemTable[ID & TileData.MaxItemValue];
    */
}

public class Map
{
    private struct Sector
    {
        public StaticTile[,][] Statics;
        public LandTile[,] Land;
    }

    private Sector[,] m_Sectors;

    private LandTile[,] m_InvalidLandSector;
    private StaticTile[,][] m_EmptyStaticSector;

    private MemoryMappedReader? m_Map;

    private FileStream? m_Index;
    private BinaryReader? m_IndexReader;

    private MemoryMappedReader? m_Statics;

    private int m_SectorWidth;
    private int m_SectorHeight;

    public Map(int fileIndex, int width, int height)
    {
        m_SectorWidth = width >> 3;
        m_SectorHeight = height >> 3;

        if (fileIndex != 0x7F)
        {
            string mapPath = UORenderer.CurrentProject.GetFullPath($"map{fileIndex}.mul");

            if (File.Exists(mapPath))
            {
                m_Map = new MemoryMappedReader(new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            }

            string indexPath = UORenderer.CurrentProject.GetFullPath($"staidx{fileIndex}.mul");

            if (File.Exists(indexPath))
            {
                m_Index = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                m_IndexReader = new BinaryReader(m_Index);
            }

            string staticsPath = UORenderer.CurrentProject.GetFullPath($"statics{fileIndex}.mul");

            if (File.Exists(staticsPath))
                m_Statics = new MemoryMappedReader(new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        m_EmptyStaticSector = new StaticTile[8, 8][];

        for (int i = 0; i < 8; ++i)
        {
            for (int j = 0; j < 8; ++j)
                m_EmptyStaticSector[i, j] = Array.Empty<StaticTile>();
        }

        m_InvalidLandSector = new LandTile[8, 8];

        m_Sectors = new Sector[m_SectorWidth, m_SectorHeight];
    }

    private StaticTile[,][] GetStaticSector(int x, int y)
    {
        if (x < 0 || y < 0 || x >= m_SectorWidth || y >= m_SectorHeight || m_Statics == null || m_Index == null)
            return m_EmptyStaticSector;

        ref var Sector = ref m_Sectors[x, y];

        if (Sector.Statics == null)
        {
            Sector.Statics = ReadStaticSector(x, y);
        }

        return Sector.Statics;
    }

    public StaticTile[] GetStaticTiles(int x, int y)
    {
        StaticTile[,][] Sector = GetStaticSector(x >> 3, y >> 3);

        var tiles = Sector[(x & 0x7), (y & 0x7)];

        if (tiles == null)
            return Array.Empty<StaticTile>();

        return tiles;
    }

    private LandTile[,] GetLandSector(int x, int y)
    {
        if (x < 0 || y < 0 || x >= m_SectorWidth || y >= m_SectorHeight || m_Map == null)
            return m_InvalidLandSector;

        ref var Sector = ref m_Sectors[x, y];

        if (Sector.Land == null)
        {
            Sector.Land = ReadLandSector(x, y);
        }

        return Sector.Land;
    }

    public LandTile GetLandTile(int x, int y)
    {
        LandTile[,] tiles = GetLandSector(x >> 3, y >> 3);

        return tiles[(x & 0x7), (y & 0x7)];
    }

    private unsafe StaticTile[,][] ReadStaticSector(int x, int y)
    {
        m_IndexReader.BaseStream.Seek(((x * m_SectorHeight) + y) * 12, SeekOrigin.Begin);

        int lookup = m_IndexReader.ReadInt32();
        int length = m_IndexReader.ReadInt32();

        if (lookup < 0 || length <= 0)
        {
            return m_EmptyStaticSector;
        }

        int count = length / 7;

        m_Statics.Seek(lookup, SeekOrigin.Begin);

        StaticTile[,][] tiles = new StaticTile[8, 8][];

        for (int i = 0; i < count; i++)
        {
            var id = m_Statics.ReadUInt16();
            var offsetX = m_Statics.ReadByte();
            var offsetY = m_Statics.ReadByte();
            var offsetZ = m_Statics.ReadSByte();
            m_Statics.ReadUInt16();

            ref var tileList = ref tiles[offsetX, offsetY];
            if (tileList == null)
            {
                tileList = new StaticTile[1];
            }
            else
            {
                Array.Resize(ref tileList, tileList.Length + 1);
            }

            ref var tile = ref tileList[tileList.Length - 1];

            tile.ID = id;
            tile.X = (x * 8) + offsetX;
            tile.Y = (y * 8) + offsetY;
            tile.Z = offsetZ;
        }

        return tiles;
    }

    private unsafe LandTile[,] ReadLandSector(int x, int y)
    {
        int offset = ((x * m_SectorHeight) + y) * 196 + 4;

        m_Map.Seek(offset, SeekOrigin.Begin);

        LandTile[,] tiles = new LandTile[8, 8];

        for (int ty = 0; ty < 8; ty++)
        {
            for (int tx = 0; tx < 8; tx++)
            {
                ref LandTile tile = ref tiles[tx, ty];

                tile.ID = m_Map.ReadUInt16();
                tile.Z = m_Map.ReadSByte();
            }
        }

        return tiles;
    }

    public void Dispose()
    {
        if (m_Map != null)
            m_Map.Close();

        if (m_Statics != null)
            m_Statics.Close();

        if (m_IndexReader != null)
            m_IndexReader.Close();
    }
}