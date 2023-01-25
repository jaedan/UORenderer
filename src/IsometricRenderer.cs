using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace uoiso
{
    public class IsometricRenderer
    {
        private GraphicsDevice _gfxDevice;
        private BasicEffect _effect;

        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;

        private Matrix _world = Matrix.Identity;
        private Matrix _view = Matrix.Identity;
        private Matrix _projection = Matrix.Identity;

        private float TILE_SIZE = 22f;

        private int VIEW_ROWS = 36;
        private int VIEW_COLUMNS = 36;

        private int _primitives;

        public IsometricRenderer(GraphicsDevice device)
        {
            _gfxDevice = device;
            _effect = new BasicEffect(device);

            VertexPositionColor[] vertices = new VertexPositionColor[((VIEW_ROWS * VIEW_COLUMNS) + 1) * 4];

            Color[] colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.AntiqueWhite };

            int cur = 0;
            for (int y = 0; y < VIEW_COLUMNS; y++)
            {
                for (int x = 0; x < VIEW_ROWS; x++)
                {
                    var color = colors[(cur / 4) % 5];

                    vertices[cur++] = new VertexPositionColor(new Vector3(TILE_SIZE * x, TILE_SIZE * y, 0), color);
                    vertices[cur++] = new VertexPositionColor(new Vector3((TILE_SIZE * x) + TILE_SIZE, TILE_SIZE * y, 0), color);
                    vertices[cur++] = new VertexPositionColor(new Vector3(TILE_SIZE * x, (TILE_SIZE * y) + TILE_SIZE, 0), color);
                    vertices[cur++] = new VertexPositionColor(new Vector3((TILE_SIZE * x) + TILE_SIZE, (TILE_SIZE * y) + TILE_SIZE, 0), color);

                }
            }

            /* Last entry is the sprite */
            var tileX = VIEW_ROWS / 2;
            var tileY = VIEW_COLUMNS / 2;
            vertices[cur++] = new VertexPositionColor(new Vector3(tileX * TILE_SIZE, (tileY + 1) * TILE_SIZE, 20f), Color.Pink);
            vertices[cur++] = new VertexPositionColor(new Vector3((tileX + 1) * TILE_SIZE, tileY * TILE_SIZE, 20f), Color.Pink);
            vertices[cur++] = new VertexPositionColor(new Vector3(tileX * TILE_SIZE, (tileY + 1) * TILE_SIZE, 0f), Color.Pink);
            vertices[cur++] = new VertexPositionColor(new Vector3((tileX + 1) * TILE_SIZE, tileY * TILE_SIZE, 0f), Color.Pink);

            _vertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(vertices);

            short[] indices = new short[((VIEW_ROWS * VIEW_COLUMNS) + 1) * 6];
            short num = 0;
            for (int i = 0; i < indices.Length; i += 6)
            {
                indices[i + 0] = (short)(num + 0);
                indices[i + 1] = (short)(num + 1);
                indices[i + 2] = (short)(num + 2);
                indices[i + 3] = (short)(num + 1);
                indices[i + 4] = (short)(num + 3);
                indices[i + 5] = (short)(num + 2);

                num += 4;
            }

            _indexBuffer = new IndexBuffer(device, typeof(short), indices.Length, BufferUsage.WriteOnly);
            _indexBuffer.SetData(indices);

            _primitives = ((VIEW_ROWS * VIEW_COLUMNS) + 1) * 2;
        }

        public void Update(GameTime gameTime)
        {
            /* Game Y goes from top to bottom. Drawing Y from bottom to top. This just flips it over. */
            _world = Matrix.CreateReflection(new Plane(0, -1f, 0, 0));

            /* Where we are looking */
            Vector3 focus = new Vector3(((VIEW_ROWS / 2) * TILE_SIZE), -1f * ((VIEW_COLUMNS / 2) * TILE_SIZE), 0);

            /* Where the camera is */
            Vector3 camera = focus + new Vector3(0, 0, 255f);

            _view = Matrix.CreateLookAt(camera, focus, new Vector3(-1f, 1f, 0));

            Matrix ortho = Matrix.CreateOrthographic(1280f, 720f, 0f, 300f);

            float c = (float)Math.Cos(MathHelper.ToRadians(45)) * -1f;
            float s = (float)Math.Sin(MathHelper.ToRadians(45)) * 1f;

            Matrix oblique = new Matrix(
                                    1, 0, 0, 0,
                                    0, 1, 0, 0,
                                    c, s, 1, 0,
                                    0, 0, 0, 1);

            /*_projection = ortho * oblique; */
            _projection = ortho;
        }

        public void Draw(GameTime gameTime)
        {
            _gfxDevice.Clear(Color.CornflowerBlue);

            _effect.World = _world;
            _effect.View = _view;
            _effect.Projection = _projection;
            _effect.VertexColorEnabled = true;

            _gfxDevice.SetVertexBuffer(_vertexBuffer);
            _gfxDevice.Indices = _indexBuffer;

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            _gfxDevice.RasterizerState = rasterizerState;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexBuffer.VertexCount, 0, _primitives);
            }
        }
    }
}