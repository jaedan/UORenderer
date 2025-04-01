using UORenderer.Assets;
using Microsoft.Xna.Framework;

namespace UORenderer;

public struct LandTile
{
    public ushort ID;
    public sbyte Z;

    public Vector4 CornerZ;

    public Vector3 NormalTop;
    public Vector3 NormalRight;
    public Vector3 NormalBottom;
    public Vector3 NormalLeft;

    public LandTiles Data => TileDataLoader.Instance.LandData[ID];
}

public struct StaticTile
{
    public ushort ID;
    public int X;
    public int Y;
    public int Z;
    public ushort Hue;

    public StaticTiles Data => TileDataLoader.Instance.StaticData[ID];
}

public class Map
{
    public static float TILE_SIZE = 31.11f;
    public static float TILE_Z_SCALE = 4f;

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

        ref var sector = ref m_Sectors[x, y];

        if (sector.Land == null)
        {
            sector.Land = ReadLandSector(x, y);

            for (int tx = 0; tx < 8; tx++)
            {
                for (int ty = 0; ty < 8; ty++)
                {
                    var tileX = x * 8 + tx;
                    var tileY = y * 8 + ty;

                    ref var tile = ref sector.Land[tx, ty];

                    tile.CornerZ = GetCornerZ(tileX, tileY);

                    tile.NormalTop = ComputeNormal(tileX, tileY);
                    tile.NormalRight = ComputeNormal(tileX + 1, tileY);
                    tile.NormalLeft = ComputeNormal(tileX, tileY + 1);
                    tile.NormalBottom = ComputeNormal(tileX + 1, tileY + 1);
                }
            }

        }

        return sector.Land;
    }

    public LandTile GetLandTile(int x, int y)
    {
        LandTile[,] tiles = GetLandSector(x >> 3, y >> 3);

        return tiles[(x & 0x7), (y & 0x7)];
    }

    private bool CanDrawStatic(ushort id)
    {
        if (id >= TileDataLoader.Instance.StaticData.Length)
            return false;

        ref StaticTiles data = ref TileDataLoader.Instance.StaticData[id];

        if ((data.Flags & TileFlag.NoDraw) != 0)
            return false;

        switch (id)
        {
            case 0x0001:
            case 0x21BC:
            case 0x63D3:
                return false;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
                return ((data.Flags & TileFlag.Background) == 0 &&
                        (data.Flags & TileFlag.Surface) == 0 &&
                        (data.Flags & TileFlag.NoDraw) == 0);

            case 0x2198:
            case 0x2199:
            case 0x21A0:
            case 0x21A1:
            case 0x21A2:
            case 0x21A3:
            case 0x21A4:
                return false;
        }

        return true;
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

            if (!CanDrawStatic(id))
                continue;

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

        if (x == 180 && y == 211)
        {
            int q = 5;
        }

        /* Sort all the tile lists by Z */
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ref var l = ref tiles[i, j];
                if (l == null)
                    continue;

                Array.Sort(l, (a, b) =>
                {
                    int zdiff = a.Z.CompareTo(b.Z);
                    if (zdiff != 0)
                    {
                        return zdiff;
                    }

                    // Tie breaks for tiles at the same Z, so it remains
                    // deterministic

                    if (a.Data.Flags.HasFlag(TileFlag.Foliage) && !b.Data.Flags.HasFlag(TileFlag.Foliage))
                    {
                        return 1;
                    }
                    else if (b.Data.Flags.HasFlag(TileFlag.Foliage))
                    {
                        return -1;
                    }

                    if (a.Data.Flags.HasFlag(TileFlag.Background) && !b.Data.Flags.HasFlag(TileFlag.Background))
                    {
                        return -1;
                    }
                    else if (b.Data.Flags.HasFlag(TileFlag.Background))
                    {
                        return 1;
                    }

                    if (a.Data.Flags.HasFlag(TileFlag.Roof) && !b.Data.Flags.HasFlag(TileFlag.Roof))
                    {
                        return 1;
                    }
                    else if (b.Data.Flags.HasFlag(TileFlag.Roof))
                    {
                        return -1;
                    }

                    return 0;
                });
            }
        }

        return tiles;
    }

    private static (Vector2, Vector2)[] _offsets = new[]
    {
        (new Vector2(1, 0), new Vector2(0, 1)),
        (new Vector2(0, 1), new Vector2(-1, 0)),
        (new Vector2(-1, 0), new Vector2(0, -1)),
        (new Vector2(0, -1), new Vector2(1, 0))
    };

    private Vector3 ComputeNormal(int tileX, int tileY)
    {
        /* To avoid recursion, this doesn't use the cache and always re-reads
         * the map file */
        var t = ReadLandSector(tileX >> 3, tileY >> 3)[(tileX & 0x7), (tileY & 0x7)];

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _offsets.Length; i++)
        {
            (var tu, var tv) = _offsets[i];

            int ux = (int)(tileX + tu.X);
            int uy = (int)(tileY + tu.Y);

            int vx = (int)(tileX + tv.X);
            int vy = (int)(tileY + tv.Y);


            var tx = ReadLandSector(ux >> 3, uy >> 3)[(ux & 0x7), (uy & 0x7)];
            var ty = ReadLandSector(vx >> 3, vy >> 3)[(vx & 0x7), (vy & 0x7)];

            if (tx.ID == 0 || ty.ID == 0)
                continue;

            Vector3 u = new Vector3(tu.X * TILE_SIZE, tu.Y * TILE_SIZE, tx.Z - t.Z);
            Vector3 v = new Vector3(tv.X * TILE_SIZE, tv.Y * TILE_SIZE, ty.Z - t.Z);

            var tmp = Vector3.Cross(u, v);
            normal = Vector3.Add(normal, tmp);
        }

        return Vector3.Normalize(normal);
    }

    private Vector4 GetCornerZ(int x, int y)
    {
        /* To avoid recursion, this doesn't use the cache and always re-reads
         * the map file */

        var top = ReadLandSector(x >> 3, y >> 3)[(x & 0x7), (y & 0x7)];
        var right = ReadLandSector((x + 1) >> 3, y >> 3)[((x + 1) & 0x7), (y & 0x7)];
        var left = ReadLandSector(x >> 3, (y + 1) >> 3)[(x & 0x7), ((y + 1) & 0x7)];
        var bottom = ReadLandSector((x + 1) >> 3, (y + 1) >> 3)[((x + 1) & 0x7), ((y + 1) & 0x7)];

        return new Vector4(
            top.Z * TILE_Z_SCALE,
            right.Z * TILE_Z_SCALE,
            left.Z * TILE_Z_SCALE,
            bottom.Z * TILE_Z_SCALE
        );
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