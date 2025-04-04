using UORenderer.Assets;
using UORenderer.Utility;
using UORenderer.Utility.Logging;
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
        
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowResized;
    }

    protected override void Initialize()
    {
        Log.Start(LogTypes.All);

        ClientVersionHelper.TryParseFromFile(UORenderer.CurrentProject.GetFullPath("client.exe"), out string version);

        if (!ClientVersionHelper.IsClientVersionValid(version, out ClientVersion clientVersion))
        {
            throw new Exception("Could not discover client version");
        }

        UOFileManager.Load(clientVersion, UORenderer.CurrentProject.BasePath, false, "ENU");

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
    
            
    private void OnWindowResized(object? sender, EventArgs e) {
        if (sender is GameWindow window) 
            _mapManager.OnWindowsResized(window);
    }
}