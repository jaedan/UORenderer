using ClassicUO.Assets;

namespace UORenderer;

public abstract class ArtFile : IDisposable
{
    public struct Sprite
    {
        public ushort[] Pixels;
        public int Width;
        public int Height;
    };

    public virtual int Max => 2048; // ?

    public abstract void Dispose();

    public abstract Sprite GetSprite(uint id);
}

public class TexMapsFile: ArtFile
{
    public TexMapsFile(string path)
    {
    }

    public override int Max => TexmapsLoader.Instance.Entries.Length;

    public override void Dispose()
    {

    }

    public override Sprite GetSprite(uint id)
    {
        ushort[] pixels = TexmapsLoader.Instance.GetLandTexture(id, out var width, out var height);

        return new Sprite()
        {
            Pixels = pixels,
            Width = width,
            Height = height
        };
    }
}

public class StaticsFile: ArtFile
{
    public StaticsFile(string path)
    {
    }

    public override int Max => ArtLoader.MAX_STATIC_DATA_INDEX_COUNT;

    public override void Dispose()
    {

    }

    public override Sprite GetSprite(uint id)
    {
        ushort[] pixels = ArtLoader.Instance.GetStaticTexture(id, out var bounds);

        return new Sprite()
        {
            Pixels = pixels,
            Width = bounds.Width,
            Height = bounds.Height
        };
    }
}

public class LandFile: ArtFile
{
    public LandFile(string path)
    {
    }

    public override int Max => ArtLoader.MAX_LAND_DATA_INDEX_COUNT;

    public override void Dispose()
    {

    }

    public override Sprite GetSprite(uint id)
    {
        ushort[] pixels = ArtLoader.Instance.GetStaticTexture(id, out var bounds);

        return new Sprite()
        {
            Pixels = pixels,
            Width = bounds.Width,
            Height = bounds.Height
        };
    }
}
