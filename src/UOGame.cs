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

    protected override void Initialize()
    {
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