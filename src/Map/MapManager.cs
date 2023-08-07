using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace UORenderer;

public class MapManager
{
    private readonly GraphicsDevice _gfxDevice;

    private readonly MapEffect _mapEffect;
    private readonly MapRenderer _mapRenderer;

    private readonly RenderTarget2D _shadowTarget;

    private readonly PostProcessRenderer _postProcessRenderer;

    // Currently loaded map
    private Map _map;

    private struct LandTexture
    {
        public Texture2D Texture;
        public Rectangle Bounds;
        public bool Rotate;
    }

    private LandTexture[] _landTextures;

    private struct StaticTexture
    {
        public Texture2D Texture;
        public Rectangle Bounds;
    }

    private StaticTexture[] _staticTextures;

    private ArtFile _staticArt;

    private Camera _camera = new Camera();
    private Camera _lightSourceCamera = new Camera();

    private LightingState _lightingState = new LightingState();
    private DepthStencilState _depthStencilState = new DepthStencilState()
    {
        DepthBufferEnable = true,
        DepthBufferWriteEnable = true,
        DepthBufferFunction = CompareFunction.Less,
        StencilEnable = false
    };

    private float TILE_SIZE = 31.11f;
    private float TILE_Z_SCALE = 4f;

    private void DarkenTexture(ushort[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            ushort c = pixels[i];

            int red = (c >> 10) & 0x1F;
            int green = (c >> 5) & 0x1F;
            int blue = c & 0x1F;

            red = (int)(red * 0.85355339f);
            green = (int)(green * 0.85355339f);
            blue = (int)(blue * 0.85355339f);

            pixels[i] = (ushort)((1 << 15) | (red << 10) | (green << 5) | blue);
        }
    }

    public MapManager(GraphicsDevice gd)
    {
        _gfxDevice = gd;

        TextureFormat.DetectFormat(gd);
        TextureAtlasManager.Initialize(gd);

        _mapEffect = new MapEffect(gd);
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];

        _mapRenderer = new MapRenderer(gd);

        _shadowTarget = new RenderTarget2D(
                                gd,
                                gd.PresentationParameters.BackBufferWidth * 2,
                                gd.PresentationParameters.BackBufferHeight * 2,
                                false,
                                SurfaceFormat.Single,
                                DepthFormat.Depth24);

        _postProcessRenderer = new PostProcessRenderer(gd);

        _map = new Map(0, 7168, 4096);

        var focus = _map.GetLandTile(1440, 1688);

        _camera.LookAt = new Vector3(1440 * TILE_SIZE, 1688 * TILE_SIZE, focus.Z * TILE_Z_SCALE);
        _camera.ScreenSize.X = 0;
        _camera.ScreenSize.Y = 0;
        _camera.ScreenSize.Width = gd.PresentationParameters.BackBufferWidth;
        _camera.ScreenSize.Height = gd.PresentationParameters.BackBufferHeight;

        // This has to match the LightDirection below
        _lightSourceCamera.LookAt = _camera.LookAt;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightingState.LightDirection = new Vector3(0, -1, -1f);
        _lightingState.LightDiffuseColor = Vector3.Normalize(new Vector3(1, 1, 1));
        _lightingState.LightSpecularColor = Vector3.Zero;
        _lightingState.AmbientLightColor = new Vector3(
            1f - _lightingState.LightDiffuseColor.X,
            1f - _lightingState.LightDiffuseColor.Y,
            1f - _lightingState.LightDiffuseColor.Z
        );

        using var texFile = new TexMapsFile(UORenderer.CurrentProject.GetFullPath("texmaps.uoo"));
        using var artFile = new LandFile(UORenderer.CurrentProject.GetFullPath("landtiles.uoo"));

        _landTextures = new LandTexture[artFile.Max];

        /* Pre-load the land */
        for (uint i = 0; i < _landTextures.Length; i++)
        {
            var sprite = artFile.GetSprite(i);
            var tex = texFile.GetSprite(i);

            if (tex.Pixels == null && sprite.Pixels == null)
                continue;

            if (i == 2)
            {
                /* black out the no-draw tile */
                for (int j = 0; j < tex.Pixels.Length; j++)
                {
                    if (tex.Pixels[j] != 0x8000 && tex.Pixels[j] != 0)
                    {
                        tex.Pixels[j] = 0x8000;
                    }
                }
            }

            ref var tile = ref _landTextures[i];

            if (tex.Pixels != null)
            {
                DarkenTexture(tex.Pixels);
                tile.Texture = TextureAtlasManager.AddLandTile(tex.Pixels, tex.Width, tex.Height, out tile.Bounds);
            }
            else
            {
                tile.Texture = TextureAtlasManager.AddLandTile(sprite.Pixels, sprite.Width, sprite.Height, out tile.Bounds);
                tile.Rotate = true;
            }
        }

        /* Lazy load the statics */
        _staticArt = new StaticsFile(UORenderer.CurrentProject.GetFullPath("art.uoo"));
        _staticTextures = new StaticTexture[_staticArt.Max];

    }

    private enum MouseDirection
    {
        North,
        Northeast,
        East,
        Southeast,
        South,
        Southwest,
        West,
        Northwest
    }

    // This is all just a fast math way to figure out what the direction of the mouse is.
    private MouseDirection ProcessMouseMovement(ref MouseState mouseState, out float distance)
    {
        Vector2 vec = new Vector2(mouseState.X - (_camera.ScreenSize.Width / 2), mouseState.Y - (_camera.ScreenSize.Height / 2));

        int hashf = 100 * (Math.Sign(vec.X) + 2) + 10 * (Math.Sign(vec.Y) + 2);

        distance = vec.Length();
        if (distance == 0)
        {
            return MouseDirection.North;
        }

        vec.X = Math.Abs(vec.X);
        vec.Y = Math.Abs(vec.Y);

        if (vec.Y * 5 <= vec.X * 2)
        {
            hashf = hashf + 1;
        }
        else if (vec.Y * 2 >= vec.X * 5)
        {
            hashf = hashf + 3;
        }
        else
        {
            hashf = hashf + 2;
        }

        switch (hashf)
        {
            case 111: return MouseDirection.Southwest;
            case 112: return MouseDirection.West;
            case 113: return MouseDirection.Northwest;
            case 120: return MouseDirection.Southwest;
            case 131: return MouseDirection.Southwest;
            case 132: return MouseDirection.South;
            case 133: return MouseDirection.Southeast;
            case 210: return MouseDirection.Northwest;
            case 230: return MouseDirection.Southeast;
            case 311: return MouseDirection.Northeast;
            case 312: return MouseDirection.North;
            case 313: return MouseDirection.Northwest;
            case 320: return MouseDirection.Northeast;
            case 331: return MouseDirection.Northeast;
            case 332: return MouseDirection.East;
            case 333: return MouseDirection.Southeast;
        }

        return MouseDirection.North;
    }


    private int _lastScrollWheel;
    private readonly float WHEEL_DELTA = 1200f;

    public void Update(GameTime gameTime, bool processMouse, bool processKeyboard)
    {
        if (processMouse)
        {
            var mouse = Mouse.GetState();

            if (mouse.RightButton == ButtonState.Pressed)
            {
                var direction = ProcessMouseMovement(ref mouse, out var distance);

                int increment = distance > 200 ? 10 : 5;
                switch (direction)
                {
                    case MouseDirection.North:
                        _camera.LookAt.Y -= increment;
                        break;
                    case MouseDirection.Northeast:
                        _camera.LookAt.Y -= increment;
                        _camera.LookAt.X += increment;
                        break;
                    case MouseDirection.East:
                        _camera.LookAt.X += increment;
                        break;
                    case MouseDirection.Southeast:
                        _camera.LookAt.X += increment;
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.South:
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.Southwest:
                        _camera.LookAt.X -= increment;
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.West:
                        _camera.LookAt.X -= increment;
                        break;
                    case MouseDirection.Northwest:
                        _camera.LookAt.X -= increment;
                        _camera.LookAt.Y -= increment;
                        break;
                }
            }

            if (mouse.ScrollWheelValue != _lastScrollWheel)
            {
                _camera.Zoom += (mouse.ScrollWheelValue - _lastScrollWheel) / WHEEL_DELTA;
                _lastScrollWheel = mouse.ScrollWheelValue;
            }
        }

        if (processKeyboard)
        {
            var keyboard = Keyboard.GetState();

            foreach (var key in keyboard.GetPressedKeys())
            {
                switch (key)
                {
                    case Keys.E:
                        _camera.Rotation += 1;
                        break;
                    case Keys.Q:
                        _camera.Rotation -= 1;
                        break;
                    case Keys.Escape:
                        _camera.Rotation = 0;
                        _camera.Zoom = 1;
                        break;
                    case Keys.A:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y += 10;
                        break;
                    case Keys.D:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y -= 10;
                        break;
                    case Keys.W:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y -= 10;
                        break;
                    case Keys.S:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y += 10;
                        break;
                    case Keys.Z:
                        _camera.Zoom += 0.1f;
                        break;
                    case Keys.X:
                        _camera.Zoom -= 0.1f;
                        if (_camera.Zoom < 0.5f)
                            _camera.Zoom = 0.5f;
                        break;

                }
            }
        }

        var t = _map.GetLandTile((int)(_camera.LookAt.X / TILE_SIZE), (int)(_camera.LookAt.Y / TILE_SIZE));
        _camera.LookAt.Z = t.Z;

        _camera.Update();

        _lightSourceCamera.LookAt = _camera.LookAt;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightSourceCamera.Update();
    }

    private void CalculateViewRange(out int minTileX, out int minTileY, out int maxTileX, out int maxTileY)
    {
        float zoom = _camera.Zoom;

        int screenWidth = _camera.ScreenSize.Width;
        int screenHeight = _camera.ScreenSize.Height;

        /* Calculate the size of the drawing diamond in pixels */
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom / 2f;

        Vector3 center = _camera.LookAt;

        minTileX = (int)Math.Ceiling((center.X - screenDiamondDiagonal) / TILE_SIZE);
        minTileY = (int)Math.Ceiling((center.Y - screenDiamondDiagonal) / TILE_SIZE);

        // Render a few extra rows at the bottom to deal with things at higher z
        maxTileX = (int)Math.Ceiling((center.X + screenDiamondDiagonal) / TILE_SIZE) + 4;
        maxTileY = (int)Math.Ceiling((center.Y + screenDiamondDiagonal) / TILE_SIZE) + 4;
    }

    private static (Vector2, Vector2)[] _offsets = new[]
    {
        (new Vector2(1, 0), new Vector2(0, 1)),
        (new Vector2(0, 1), new Vector2(-1, 0)),
        (new Vector2(-1, 0), new Vector2(0, -1)),
        (new Vector2(0, -1), new Vector2(1, 0))
    };

    public Vector3 ComputeNormal(int tileX, int tileY)
    {
        var t = _map.GetLandTile(tileX, tileY);

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _offsets.Length; i++)
        {
            (var tu, var tv) = _offsets[i];

            var tx = _map.GetLandTile((int)(tileX + tu.X), (int)(tileY + tu.Y));
            var ty = _map.GetLandTile((int)(tileX + tv.X), (int)(tileY + tv.Y));

            if (tx.ID == 0 || ty.ID == 0)
                continue;

            Vector3 u = new Vector3(tu.X * TILE_SIZE, tu.Y * TILE_SIZE, tx.Z - t.Z);
            Vector3 v = new Vector3(tv.X * TILE_SIZE, tv.Y * TILE_SIZE, ty.Z - t.Z);

            var tmp = Vector3.Cross(u, v);
            normal = Vector3.Add(normal, tmp);
        }

        return Vector3.Normalize(normal);
    }

    public Vector4 GetCornerZ(int x, int y)
    {
        var top = _map.GetLandTile(x, y);
        var right = _map.GetLandTile(x + 1, y);
        var left = _map.GetLandTile(x, y + 1);
        var bottom = _map.GetLandTile(x + 1, y + 1);

        return new Vector4(
            top.Z * TILE_Z_SCALE,
            right.Z * TILE_Z_SCALE,
            left.Z * TILE_Z_SCALE,
            bottom.Z * TILE_Z_SCALE
        );
    }

    private bool IsRock(ushort id)
    {
        switch (id)
        {
            case 4945:
            case 4948:
            case 4950:
            case 4953:
            case 4955:
            case 4958:
            case 4959:
            case 4960:
            case 4962:
                return true;

            default:
                return id >= 6001 && id <= 6012;
        }
    }

    private bool IsTree(ushort id)
    {
        switch (id)
        {
            case 3274:
            case 3275:
            case 3276:
            case 3277:
            case 3280:
            case 3283:
            case 3286:
            case 3288:
            case 3290:
            case 3293:
            case 3296:
            case 3299:
            case 3302:
            case 3394:
            case 3395:
            case 3417:
            case 3440:
            case 3461:
            case 3476:
            case 3480:
            case 3484:
            case 3488:
            case 3492:
            case 3496:
            case 3230:
            case 3240:
            case 3242:
            case 3243:
            case 3273:
            case 3320:
            case 3323:
            case 3326:
            case 3329:
            case 4792:
            case 4793:
            case 4794:
            case 4795:
            case 12596:
            case 12593:
            case 3221:
            case 3222:
            case 12602:
            case 12599:
            case 3238:
            case 3225:
            case 3229:
            case 12881:
            case 3228:
            case 3227:
            case 39290:
            case 39280:
            case 39219:
            case 39215:
            case 39223:
            case 39288:
            case 39217:
            case 39225:
            case 39284:
            case 46822:
            case 14492:
                return true;
        }

        return false;
    }

    private void DrawShadowMap(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--)
            {
                var statics = _map.GetStaticTiles(x, y);

                for (int i = statics.Length - 1; i >= 0; i--)
                {
                    ref var s = ref statics[i];

                    ref var data = ref TileDataLoader.Instance.StaticData[s.ID];

                    if (!IsRock(s.ID) && !IsTree(s.ID) && !data.Flags.HasFlag(TileFlag.Foliage))
                        continue;

                    DrawStatic(ref s, x, y, (statics.Length - 1 - i) * 0.0001f);
                }
            }
        }

        DrawLand(minTileX, minTileY, maxTileX, maxTileY);
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

    private enum HueMode
    {
        NONE = 0,
        HUED = 1,
        PARTIAL = 2
    }
    
    private Vector3 GetHueVector(StaticTile s) {
        var hue = s.Hue;
        var partial = TileDataLoader.Instance.StaticData[s.ID].IsPartialHue;
        HueMode mode;
        
        if ((s.Hue & 0x8000) != 0)
        {
            partial = true;
            hue &= 0x7FFF;
        }

        if (hue == 0)
        {
            partial = false;
        }
        
        if (hue != 0) {
            // hue -= 1; //ClassicUO does this decrement, but I works without it

            mode = partial ? HueMode.PARTIAL : HueMode.HUED;
        }
        else
        {
            mode = HueMode.NONE;
        }

        return new Vector3(hue, (int)mode, 0);
    }

    private void DrawStatic(ref StaticTile s, int x, int y, float depthOffset)
    {
        if (!CanDrawStatic(s.ID))
            return;

        ref var data = ref TileDataLoader.Instance.StaticData[s.ID];

        ref var staticTex = ref _staticTextures[s.ID];

        if (staticTex.Texture == null)
        {
            var sprite = _staticArt.GetSprite(s.ID);

            if (sprite.Pixels == null)
            {
                return;
            }

            staticTex.Texture = TextureAtlasManager.AddArt(sprite.Pixels, sprite.Width, sprite.Height, out staticTex.Bounds);
        }

        bool cylindrical = data.Flags.HasFlag(TileFlag.Foliage) || IsRock(s.ID) || IsTree(s.ID);

        var hueCoords = GetHueVector(s);

        _mapRenderer.DrawBillboard(
            new Vector3(x * TILE_SIZE, y * TILE_SIZE, s.Z * TILE_Z_SCALE),
            depthOffset,
            staticTex.Texture,
            staticTex.Bounds,
            hueCoords,
            cylindrical
        );
    }

    public void DrawStatics(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--)
            {
                var statics = _map.GetStaticTiles(x, y);

                for (int i = statics.Length - 1; i >= 0; i--)
                {
                    ref var s = ref statics[i];

                    DrawStatic(ref s, x, y, (statics.Length - 1 - i) * 0.0001f);
                }
            }
        }
    }

    private void DrawLand(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--)
            {
                var tile = _map.GetLandTile(x, y);

                ref var tileTex = ref _landTextures[tile.ID];

                if (tileTex.Texture == null)
                    continue;

                ref var data = ref TileDataLoader.Instance.LandData[tile.ID];

                if ((data.Flags & TileFlag.Wet) != 0)
                {
                    /* Water tiles are always flat */
                    _mapRenderer.DrawTile(
                        new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                        new Vector4(tile.Z * TILE_Z_SCALE),
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        tileTex.Texture,
                        tileTex.Bounds,
                        tileTex.Rotate
                    );
                }
                else
                {
                    _mapRenderer.DrawTile(
                        new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                        tile.CornerZ,
                        tile.NormalTop,
                        tile.NormalRight,
                        tile.NormalLeft,
                        tile.NormalBottom,
                        tileTex.Texture,
                        tileTex.Bounds,
                        tileTex.Rotate
                    );
                }


            }
        }
    }

    public void Draw()
    {
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.Viewport = new Viewport(0, 0, _gfxDevice.PresentationParameters.BackBufferWidth, _gfxDevice.PresentationParameters.BackBufferHeight);

        CalculateViewRange(out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);

        _mapEffect.WorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.LightSource.Enabled = false;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["ShadowMap"];

        _mapRenderer.Begin(_shadowTarget, _mapEffect, _lightSourceCamera, RasterizerState.CullNone, SamplerState.PointClamp, _depthStencilState, BlendState.AlphaBlend, null);
        DrawShadowMap(minTileX, minTileY, maxTileX, maxTileY);
        _mapRenderer.End();

        _mapEffect.WorldViewProj = _camera.WorldViewProj;
        _mapEffect.LightWorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.AmbientLightColor = _lightingState.AmbientLightColor;
        _mapEffect.LightSource.Direction = _lightingState.LightDirection;
        _mapEffect.LightSource.DiffuseColor = _lightingState.LightDiffuseColor;
        _mapEffect.LightSource.SpecularColor = _lightingState.LightSpecularColor;
        _mapEffect.LightSource.Enabled = true;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp, _depthStencilState, BlendState.AlphaBlend, _shadowTarget);
        DrawStatics(minTileX, minTileY, maxTileX, maxTileY);
        _mapRenderer.End();

        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp, _depthStencilState, BlendState.AlphaBlend, _shadowTarget);
        DrawLand(minTileX, minTileY, maxTileX, maxTileY);
        _mapRenderer.End();
    }
}