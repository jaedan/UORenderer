using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UORenderer;

public static class TextureFormat
{
    public static SurfaceFormat SurfaceFormat { get; private set; }

    [DllImport("ntdll.dll", EntryPoint = "wine_get_version", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern string GetWineVersion();

    public static void DetectFormat(GraphicsDevice device)
    {
        SurfaceFormat format = SurfaceFormat.Bgra5551;
        if (OperatingSystem.IsWindows())
        {
            bool isWine = false;
            try
            {
                var version = GetWineVersion();
                isWine = true;
            }
            catch
            {
            }

            if (!isWine && !OperatingSystem.IsWindowsVersionAtLeast(6, 2, 0, 0))
            {
                // Windows 7 DirectX 11 does not support 16 bit textures
                format = SurfaceFormat.Color;
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Macs don't support 16 bit textures
            format = SurfaceFormat.Color;
        }

        // If we think we can use a format other than SurfaceFormat.Color, attempt to create a texture
        // now to see if it crashes.
        if (format != SurfaceFormat.Color)
        {
            try
            {
                var texture = new Texture2D(device, 64, 64, false, format);
            }
            catch
            {
                format = SurfaceFormat.Color;
            }
        }

        SurfaceFormat = format;
    }

    [ThreadStatic] private static uint[] _upscaleBuffer;

    private static readonly byte[] _table = new byte[32]
    {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B,
            0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Color16To32(ushort c)
    {
        return (uint)(_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16));
    }

    // The returned buffer is only valid until the next Upscale call.
    public static uint[] Upscale(Span<ushort> input)
    {
        // Upscale 16 to 32
        if (_upscaleBuffer == null || _upscaleBuffer.Length < (input.Length))
        {
            _upscaleBuffer = new uint[input.Length];
        }

        for (int i = 0; i < input.Length; i++)
        {
            if ((input[i] & 0x8000) != 0)
            {
                _upscaleBuffer[i] = Color16To32(input[i]) | 0xFF_00_00_00;
            }
            else
            {
                _upscaleBuffer[i] = 0;
            }
        }

        return _upscaleBuffer;
    }
}
static class TextureAtlasManager
{
    // Lists of "full" texture atlases
    private static LinkedList<TextureAtlas> _anims = new LinkedList<TextureAtlas>(); // Mobiles
    private static LinkedList<TextureAtlas> _art = new LinkedList<TextureAtlas>(); // Statics
    private static LinkedList<TextureAtlas> _gumps = new LinkedList<TextureAtlas>();
    private static LinkedList<TextureAtlas> _lights = new LinkedList<TextureAtlas>();
    private static LinkedList<TextureAtlas> _landTiles = new LinkedList<TextureAtlas>(); // Flat land tiles

    // The current "open" texture atlas that's being added to
    private static TextureAtlas _currentAnim;
    private static TextureAtlas _currentArt;
    private static TextureAtlas _currentGump;
    private static TextureAtlas _currentLight;
    private static TextureAtlas _currentLandTiles;

    private static GraphicsDevice _device;

    public static void Initialize(GraphicsDevice device)
    {
        _device = device;

        SurfaceFormat format = TextureFormat.SurfaceFormat;

        _currentAnim = new TextureAtlas(_device, 0x800, 0x800, format);
        _currentArt = new TextureAtlas(_device, 0x800, 0x800, format);
        _currentGump = new TextureAtlas(_device, 0x1000, 0x1000, format);
        _currentLight = new TextureAtlas(_device, 0x400, 0x400, format);
        _currentLandTiles = new TextureAtlas(_device, 0x800, 0x800, format);
    }

    private static int CalculateSize(TextureAtlas atlas)
    {
        int size = atlas.Width * atlas.Height;
        if (atlas.SurfaceFormat == SurfaceFormat.Bgra5551)
        {
            size *= 2;
        }
        else if (atlas.SurfaceFormat == SurfaceFormat.Color)
        {
            size *= 4;
        }
        else
        {
            throw new Exception("Unknown surface format");
        }

        return size / (1024 * 1024);
    }

    public static void GetMemoryUse(out int anim, out int art, out int gump, out int light, out int land)
    {
        anim = (1 + _anims.Count) * CalculateSize(_currentAnim);
        art = (1 + _art.Count) * CalculateSize(_currentArt);
        gump = (1 + _gumps.Count) * CalculateSize(_currentGump);
        light = (1 + _lights.Count) * CalculateSize(_currentLight);
        land = (1 + _landTiles.Count) * CalculateSize(_currentLandTiles);
    }

    private static unsafe Texture2D Add(ref TextureAtlas atlas, LinkedList<TextureAtlas> lru,
                                        Span<ushort> pixels, int width, int height,
                                        SurfaceFormat format,
                                        out Rectangle bounds)
    {
        Texture2D tex = null;
        bounds = Rectangle.Empty;

        if (format != atlas.SurfaceFormat)
        {
            if (format == SurfaceFormat.Bgra5551 && atlas.SurfaceFormat == SurfaceFormat.Color)
            {
                // Upscale 16 to 32
                var buffer = TextureFormat.Upscale(pixels);

                if (!atlas.AddSprite(buffer.AsSpan(0, pixels.Length), width, height, out tex, out bounds))
                {
                    // Failed to add to the existing texture atlas. Make a new one.
                    lru.AddFirst(atlas);
                    atlas = new TextureAtlas(_device, atlas.Width, atlas.Height, atlas.SurfaceFormat);

                    if (!atlas.AddSprite(buffer.AsSpan(0, pixels.Length), width, height, out tex, out bounds))
                    {
                        // Can't even add it to the new one.
                        return null;
                    }
                }
            }
            else
            {
                // Not supported.
            }
        }
        else
        {
            if (!atlas.AddSprite(pixels, width, height, out tex, out bounds))
            {
                // Failed to add to the existing texture atlas. Make a new one.
                lru.AddFirst(atlas);
                atlas = new TextureAtlas(_device, atlas.Width, atlas.Height, atlas.SurfaceFormat);

                if (!atlas.AddSprite(pixels, width, height, out tex, out bounds))
                {
                    // Can't even add it to the new one.
                    return null;
                }
            }
        }

        return tex;
    }

    public static unsafe Texture2D AddAnim(Span<ushort> pixels, int width, int height, out Rectangle bounds)
    {
        return Add(ref _currentAnim, _anims, pixels, width, height, SurfaceFormat.Bgra5551, out bounds);
    }

    public static unsafe Texture2D AddArt(Span<ushort> pixels, int width, int height, out Rectangle bounds)
    {
        return Add(ref _currentArt, _art, pixels, width, height, SurfaceFormat.Bgra5551, out bounds);
    }

    public static unsafe Texture2D AddGump(Span<ushort> pixels, int width, int height, out Rectangle bounds)
    {
        return Add(ref _currentGump, _gumps, pixels, width, height, SurfaceFormat.Bgra5551, out bounds);
    }

    public static unsafe Texture2D AddLight(Span<ushort> pixels, int width, int height, out Rectangle bounds)
    {
        return Add(ref _currentLight, _lights, pixels, width, height, SurfaceFormat.Bgra5551, out bounds);
    }

    public static unsafe Texture2D AddLandTile(Span<ushort> pixels, int width, int height, out Rectangle bounds)
    {
        return Add(ref _currentLandTiles, _landTiles, pixels, width, height, SurfaceFormat.Bgra5551, out bounds);
    }
}
