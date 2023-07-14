using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StbRectPackSharp;

namespace UORenderer;

class TextureAtlas : IDisposable
{
    private readonly GraphicsDevice _device;
    private Packer _packer;
    private Texture2D _texture;

    public readonly SurfaceFormat SurfaceFormat;
    public readonly int Width;
    public readonly int Height;

    public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format)
    {
        SurfaceFormat = format;
        Width = width;
        Height = height;

        _device = device;
        _texture = new Texture2D(_device, width, height, false, format);
        _packer = new Packer(width, height);
    }

    public unsafe bool AddSprite<T>(Span<T> pixels, int width, int height, out Texture2D tex, out Rectangle bounds) where T : unmanaged
    {
        if (!_packer.PackRect(width, height, out bounds))
        {
            // Won't fit
            tex = null;
            return false;
        }

        tex = _texture;

        fixed (T* src = pixels)
        {
            tex.SetDataPointerEXT
            (
                0,
                bounds,
                (IntPtr)src,
                sizeof(T) * width * height
            );
        }

        return true;
    }

    public void Dispose()
    {
        if (!_texture.IsDisposed)
        {
            _texture.Dispose();
        }

        _packer.Dispose();
    }
}
