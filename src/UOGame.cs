using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UORenderer;

internal class UOGame : Game
{
    private readonly GraphicsDeviceManager _gdm;

    private MapManager _mapManager;
    private UIManager _uiManager;

    public UOGame()
    {
        _gdm = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 900,
            IsFullScreen = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24
        };

        _gdm.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents; };

        IsMouseVisible = true;
    }

    protected override unsafe void Initialize()
    {
        Log.Start(LogTypes.All);

        ClientVersionHelper.TryParseFromFile(UORenderer.CurrentProject.GetFullPath("client.exe"), out string version);

        if (!ClientVersionHelper.IsClientVersionValid(version, out ClientVersion clientVersion))
        {
            throw new Exception("Could not discover client version");
        }

        UOFileManager.Load(clientVersion, UORenderer.CurrentProject.BasePath, false, "ENU");
        
        const int TEXTURE_WIDTH = 32;
        const int TEXTURE_HEIGHT = 3000;
        
        var hueSampler = new Texture2D(GraphicsDevice, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);

        fixed (uint* ptr = buffer) {
            HuesLoader.Instance.CreateShaderColors(buffer);
            hueSampler.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        System.Buffers.ArrayPool<uint>.Shared.Return(buffer);
        GraphicsDevice.Textures[2] = hueSampler;
        GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;

        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }

        _gdm.ApplyChanges();
        _mapManager = new MapManager(_gdm.GraphicsDevice);
        _uiManager = new UIManager(_gdm.GraphicsDevice);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _uiManager.Update(gameTime);
        _mapManager.Update(gameTime, !_uiManager.CapturingMouse, !_uiManager.CapturingKeyboard);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!IsActive)
            return;

        _mapManager.Draw();
        _uiManager.Draw();

        base.Draw(gameTime);
    }
}