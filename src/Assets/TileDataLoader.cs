#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using UORenderer.IO;
using UORenderer.Utility;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UORenderer.Assets
{
    public class TileDataLoader : UOFileLoader
    {
        private static TileDataLoader _instance;

        private static StaticTiles[] _staticData;
        private static LandTiles[] _landData;

        private TileDataLoader()
        {
        }

        public static TileDataLoader Instance => _instance ?? (_instance = new TileDataLoader());

        public ref LandTiles[] LandData => ref _landData;
        public ref StaticTiles[] StaticData => ref _staticData;

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("tiledata.mul");

                    FileSystemHelper.EnsureFileExists(path);

                    UOFileMul tileData = new UOFileMul(path);


                    bool isold = UOFileManager.Version < ClientVersion.CV_7090;
                    const int LAND_SIZE = 512;

                    int land_group = isold ? Marshal.SizeOf<LandGroupOld>() : Marshal.SizeOf<LandGroupNew>();
                    int static_group = isold ? Marshal.SizeOf<StaticGroupOld>() : Marshal.SizeOf<StaticGroupNew>();
                    int staticscount = (int)((tileData.Length - LAND_SIZE * land_group) / static_group);

                    if (staticscount > 2048)
                    {
                        staticscount = 2048;
                    }

                    tileData.Seek(0);

                    _landData = new LandTiles[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
                    _staticData = new StaticTiles[staticscount * 32];

                    byte* bufferString = stackalloc byte[20];

                    for (int i = 0; i < 512; i++)
                    {
                        tileData.Skip(4);

                        for (int j = 0; j < 32; j++)
                        {
                            if (tileData.Position + (isold ? 4 : 8) + 2 + 20 > tileData.Length)
                            {
                                goto END;
                            }

                            int idx = i * 32 + j;
                            ulong flags = isold ? tileData.ReadUInt() : tileData.ReadULong();
                            ushort textId = tileData.ReadUShort();

                            for (int k = 0; k < 20; ++k)
                            {
                                bufferString[k] = tileData.ReadByte();
                            }

                            string name = string.Intern(Encoding.UTF8.GetString(bufferString, 20).TrimEnd('\0'));

                            LandData[idx] = new LandTiles(flags, textId, name);
                        }
                    }

                END:

                    for (int i = 0; i < staticscount; i++)
                    {
                        if (tileData.Position >= tileData.Length)
                        {
                            break;
                        }

                        tileData.Skip(4);

                        for (int j = 0; j < 32; j++)
                        {
                            if (tileData.Position + (isold ? 4 : 8) + 13 + 20 > tileData.Length)
                            {
                                goto END_2;
                            }

                            int idx = i * 32 + j;

                            ulong flags = isold ? tileData.ReadUInt() : tileData.ReadULong();
                            byte weight = tileData.ReadByte();
                            byte layer = tileData.ReadByte();
                            int count = tileData.ReadInt();
                            ushort animId = tileData.ReadUShort();
                            ushort hue = tileData.ReadUShort();
                            ushort lightIndex = tileData.ReadUShort();
                            byte height = tileData.ReadByte();

                            for (int k = 0; k < 20; ++k)
                            {
                                bufferString[k] = tileData.ReadByte();
                            }

                            string name = string.Intern(Encoding.UTF8.GetString(bufferString, 20).TrimEnd('\0'));

                            StaticData[idx] = new StaticTiles
                            (
                                flags,
                                weight,
                                layer,
                                count,
                                animId,
                                hue,
                                lightIndex,
                                height,
                                name
                            );
                        }
                    }


                //path = Path.Combine(FileManager.UoFolderPath, "tileart.uop");

                //if (File.Exists(path))
                //{
                //    UOFileUop uop = new UOFileUop(path, ".bin");
                //    DataReader reader = new DataReader();
                //    for (int i = 0; i < uop.Entries.Length; i++)
                //    {
                //        long offset = uop.Entries[i].Offset;
                //        int csize = uop.Entries[i].Length;
                //        int dsize = uop.Entries[i].DecompressedLength;

                //        if (offset == 0)
                //            continue;

                //        uop.Seek(offset);
                //        byte[] cdata = uop.ReadArray<byte>(csize);
                //        byte[] ddata = new byte[dsize];

                //        ZLib.Decompress(cdata, 0, ddata, dsize);

                //        reader.SetData(ddata, dsize);

                //        ushort version = reader.ReadUShort();
                //        uint stringDicOffset = reader.ReadUInt();
                //        uint tileID = reader.ReadUInt();

                //        reader.Skip(1 + // bool unk
                //                    1 + // unk
                //                    4 + // float unk
                //                    4 + // float unk
                //                    4 + // fixed zero ?
                //                    4 + // old id ?
                //                    4 + // unk
                //                    4 + // unk
                //                    1 + // unk
                //                    4 + // 3F800000
                //                    4 + // unk
                //                    4 + // float light
                //                    4 + // float light
                //                    4   // unk
                //                    );

                //        ulong flags = reader.ReadULong();
                //        ulong flags2 = reader.ReadULong();

                //        reader.Skip(4); // unk

                //        reader.Skip(24); // EC IMAGE OFFSET
                //        byte[] imageOffset = reader.ReadArray(24); // 2D IMAGE OFFSET


                //        if (tileID + 0x4000 == 0xa28d)
                //        {
                //            TileFlag f = (TileFlag) flags;

                //        }

                //        int count = reader.ReadByte();
                //        for (int j = 0; j < count; j++)
                //        {
                //            byte prop = reader.ReadByte();
                //            uint value = reader.ReadUInt();
                //        }

                //        count = reader.ReadByte();
                //        for (int j = 0; j < count; j++)
                //        {
                //            byte prop = reader.ReadByte();
                //            uint value = reader.ReadUInt();
                //        }

                //        count = reader.ReadInt(); // Gold Silver
                //        for (int j = 0; j < count; j++)
                //        {
                //            uint amount = reader.ReadUInt();
                //            uint id = reader.ReadUInt();
                //        }

                //        count = reader.ReadInt();

                //        for (int j = 0; j < count; j++)
                //        {
                //            byte val = reader.ReadByte();

                //            if (val != 0)
                //            {
                //                if (val == 1)
                //                {
                //                    byte unk = reader.ReadByte();
                //                    uint unk1 = reader.ReadUInt();
                //                }

                //            }
                //            else
                //            {
                //                int subCount = reader.ReadInt();

                //                for (int k = 0; k < subCount; k++)
                //                {
                //                    uint unk = reader.ReadUInt();
                //                    uint unk1 = reader.ReadUInt();
                //                }
                //            }
                //        }

                //        count = reader.ReadByte();

                //        if (count != 0)
                //        {
                //            uint unk = reader.ReadUInt();
                //            uint unk1 = reader.ReadUInt();
                //            uint unk2 = reader.ReadUInt();
                //            uint unk3 = reader.ReadUInt();
                //        }


                //        if (StaticData[tileID].AnimID == 0)
                //        {
                //            //StaticData[tileID] = new StaticTiles(flags, 0, 0, 0, );
                //        }


                //    }

                //    uop.Dispose();
                //    reader.ReleaseData();
                //}



                END_2:
                    tileData.Dispose();
                }
            );
        }
    }

    public struct LandTiles
    {
        public LandTiles(ulong flags, ushort textId, string name)
        {
            Flags = (TileFlag)flags;
            TexID = textId;
            Name = name;
        }

        public TileFlag Flags;
        public ushort TexID;
        public string Name;

        public bool IsWet => (Flags & TileFlag.Wet) != 0;
        public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
        public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroup
    {
        public uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTiles[] Tiles;
    }

    public struct StaticTiles
    {
        public StaticTiles
        (
            ulong flags,
            byte weight,
            byte layer,
            int count,
            ushort animId,
            ushort hue,
            ushort lightIndex,
            byte height,
            string name
        )
        {
            Flags = (TileFlag)flags;
            Weight = weight;
            Layer = layer;
            Count = count;
            AnimID = animId;
            Hue = hue;
            LightIndex = lightIndex;
            Height = height;
            Name = name;
        }

        public TileFlag Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        public string Name;

        public bool IsAnimated => (Flags & TileFlag.Animation) != 0;
        public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
        public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
        public bool IsSurface => (Flags & TileFlag.Surface) != 0;
        public bool IsWearable => (Flags & TileFlag.Wearable) != 0;
        public bool IsInternal => (Flags & TileFlag.Internal) != 0;
        public bool IsBackground => (Flags & TileFlag.Background) != 0;
        public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
        public bool IsWet => (Flags & TileFlag.Wet) != 0;
        public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
        public bool IsRoof => (Flags & TileFlag.Roof) != 0;
        public bool IsTranslucent => (Flags & TileFlag.Translucent) != 0;
        public bool IsPartialHue => (Flags & TileFlag.PartialHue) != 0;
        public bool IsStackable => (Flags & TileFlag.Generic) != 0;
        public bool IsTransparent => (Flags & TileFlag.Transparent) != 0;
        public bool IsContainer => (Flags & TileFlag.Container) != 0;
        public bool IsDoor => (Flags & TileFlag.Door) != 0;
        public bool IsWall => (Flags & TileFlag.Wall) != 0;
        public bool IsLight => (Flags & TileFlag.LightSource) != 0;
        public bool IsNoShoot => (Flags & TileFlag.NoShoot) != 0;
        public bool IsWeapon => (Flags & TileFlag.Weapon) != 0;
        public bool IsMultiMovable => (Flags & TileFlag.MultiMovable) != 0;
        public bool IsWindow => (Flags & TileFlag.Window) != 0;
    }

    // old

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupOld
    {
        public uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesOld
    {
        public uint Flags;
        public ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupOld
    {
        public uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public StaticTilesOld[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticTilesOld
    {
        public uint Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }

    // new 

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandGroupNew
    {
        public uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public LandTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTilesNew
    {
        public TileFlag Flags;
        public ushort TexID;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticGroupNew
    {
        public uint Unk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public StaticTilesNew[] Tiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticTilesNew
    {
        public TileFlag Flags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 20)]
        public string Name;
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
}