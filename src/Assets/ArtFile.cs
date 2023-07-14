namespace UORenderer;

public class ArtFile : IDisposable
{
    public struct Sprite
    {
        public ushort[] Pixels;
        public int Width;
        public int Height;
    };

    public ArtFile(string path)
    {
    }

    public int Max => 2048; // ?

    public void Dispose()
    {
    }

    public Sprite GetSprite(uint id)
    {
        return new Sprite();
    }
}