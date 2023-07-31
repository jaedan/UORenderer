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

using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class ArtLoader : UOFileLoader
    {
        private static ArtLoader _instance;
        private UOFile _file;
        private readonly ushort _graphicMask;

        public const int MAX_LAND_DATA_INDEX_COUNT = 0x4000;
        public const int MAX_STATIC_DATA_INDEX_COUNT = 0x14000;

        private ArtLoader(int staticCount, int landCount)
        {
            _graphicMask = UOFileManager.IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;
        }

        public static ArtLoader Instance => _instance ?? (_instance = new ArtLoader(MAX_STATIC_DATA_INDEX_COUNT, MAX_LAND_DATA_INDEX_COUNT));


        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string filePath = UOFileManager.GetUOFilePath("artLegacyMUL.uop");

                    if (UOFileManager.IsUOPInstallation && File.Exists(filePath))
                    {
                        _file = new UOFileUop(filePath, "build/artlegacymul/{0:D8}.tga");
                        Entries = new UOFileIndex[Math.Max(((UOFileUop) _file).TotalEntriesCount, MAX_STATIC_DATA_INDEX_COUNT)];
                    }
                    else
                    {
                        filePath = UOFileManager.GetUOFilePath("art.mul");
                        string idxPath = UOFileManager.GetUOFilePath("artidx.mul");

                        if (File.Exists(filePath) && File.Exists(idxPath))
                        {
                            _file = new UOFileMul(filePath, idxPath, MAX_STATIC_DATA_INDEX_COUNT);
                        }
                    }

                    _file.FillEntries(ref Entries);
                }
            );
        }

        private bool LoadData(Span<ushort> data, int g, out short width, out short height, bool isTerrain)
        {
            ref UOFileIndex entry = ref GetValidRefEntry(g);

            if (isTerrain)
            {
                if (entry.Length == 0)
                {
                    width = 0;
                    height = 0;
                    return false;
                }

                width = 44;
                height = 44;

                if (data == null || data.Length < (width * height))
                {
                    return false;
                }

                /* 
                 * Since the data only contains the diamond shape, we may not actually read
                 * into every pixel in 'data'. We must zero the buffer here since it is
                 * re-used. But we only have to zero out the (44 * 44) worth.
                 */
                data.Slice(0, (width * height)).Fill(0);

                _file.SetData(entry.Address, entry.FileSize);
                _file.Seek(entry.Offset);

                for (int i = 0; i < 22; ++i)
                {
                    int start = 22 - (i + 1);
                    int pos = i * 44 + start;
                    int end = start + ((i + 1) << 1);

                    for (int j = start; j < end; ++j)
                    {
                        data[pos++] = (ushort)(_file.ReadUShort() | 0x8000);
                    }
                }

                for (int i = 0; i < 22; ++i)
                {
                    int pos = (i + 22) * 44 + i;
                    int end = i + ((22 - i) << 1);

                    for (int j = i; j < end; ++j)
                    {
                        data[pos++] = (ushort)(_file.ReadUShort() | 0x8000);
                    }
                };
            }
            else
            {
                if (ReadHeader(_file, ref entry, out width, out height))
                {
                    if (data.Length < (width * height))
                    {
                        return false;
                    }

                    /* 
                     * Since the data is run-length-encoded, we may not actually read
                     * into every pixel in 'data'. We must zero the buffer here since it is
                     * re-used. But we only have to zero out the (width * height) worth.
                     */
                    data.Slice(0, (width * height)).Fill(0);

                    ushort fixedGraphic = (ushort)(g - 0x4000);

                    if (ReadData(data, width, height, _file))
                    {
                        // keep the cursor graphic check to cleanup edges
                        if ((fixedGraphic >= 0x2053 && fixedGraphic <= 0x2062) || (fixedGraphic >= 0x206A && fixedGraphic <= 0x2079))
                        {
                            for (int i = 0; i < width; i++)
                            {
                                data[i] = 0;
                                data[(height - 1) * width + i] = 0;
                            }

                            for (int i = 0; i < height; i++)
                            {
                                data[i * width] = 0;
                                data[i * width + width - 1] = 0;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public ushort[] GetLandTexture(uint g, out Rectangle bounds)
        {
            g &= _graphicMask;

            ushort[] data = null;

            if (!LoadData(data, (int)g, out var width, out var height, true))
            {
                data = new ushort[width * height];

                if (!LoadData(data, (int)g, out width, out height, true))
                {
                    bounds = Rectangle.Empty;
                    return null;
                }
            }

            bounds = new Rectangle(0, 0, width, height);
            return data;
        }

        public ushort[] GetStaticTexture(uint g, out Rectangle bounds)
        {
            g += 0x4000;

            ushort[] data = null;

            if (!LoadData(data, (int)g, out var width, out var height, false))
            {
                data = new ushort[width * height];

                if (!LoadData(data, (int)g, out width, out height, false))
                {
                    bounds = Rectangle.Empty;
                    return null;
                }
            }

            bounds = new Rectangle(0, 0, width, height);
            return data;
        }

        private bool ReadHeader(DataReader file, ref UOFileIndex entry, out short width, out short height)
        {
            if (entry.Length == 0)
            {
                width = 0;
                height = 0;

                return false;
            }

            file.SetData(entry.Address, entry.FileSize);
            file.Seek(entry.Offset);
            file.Skip(4);
            width = file.ReadShort();
            height = file.ReadShort();

            return width > 0 && height > 0;
        }

        private unsafe bool ReadData(Span<ushort> pixels, int width, int height, DataReader file)
        {
            ushort* ptr = (ushort*)file.PositionAddress;
            ushort* lineoffsets = ptr;
            byte* datastart = (byte*)ptr + height * 2;
            int x = 0;
            int y = 0;
            ptr = (ushort*)(datastart + lineoffsets[0] * 2);

            while (y < height)
            {
                ushort xoffs = *ptr++;
                ushort run = *ptr++;

                if (xoffs + run >= 2048)
                {
                    return false;
                }

                if (xoffs + run != 0)
                {
                    x += xoffs;
                    int pos = y * width + x;

                    for (int j = 0; j < run; ++j, ++pos)
                    {
                        ushort val = *ptr++;

                        if (val != 0)
                        {
                            pixels[pos] = (ushort)(val | 0x8000);
                        }
                    }

                    x += run;
                }
                else
                {
                    x = 0;
                    ++y;
                    ptr = (ushort*)(datastart + lineoffsets[y] * 2);
                }
            }

            return true;
        }
    }
}