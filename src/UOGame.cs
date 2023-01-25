using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace uoiso
{
    internal unsafe class UOGame : Game
    {
        private GraphicsDeviceManager _gdm;
        private IsometricRenderer _renderer;

        public UOGame()
        {
            _gdm = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                IsFullScreen = false,
                SynchronizeWithVerticalRetrace = true
            };
        }

        protected override void Initialize()
        {
            if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
            {
                _gdm.GraphicsProfile = GraphicsProfile.HiDef;
            }

            _gdm.ApplyChanges();

            _renderer = new IsometricRenderer(_gdm.GraphicsDevice);

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
            _renderer.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _renderer.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}