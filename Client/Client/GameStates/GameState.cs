using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client.GameStates
{
    public abstract class GameState
    {
        protected ContentManager content;
        protected GraphicsDevice graphicsDevice;
        protected ClientCode.Client clientGame;

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
        public abstract void PostUpdate(GameTime gameTime);
        public GameState(ClientCode.Client clientGame, GraphicsDevice graphicsDevice, ContentManager content)
        {
            this.clientGame = clientGame;
            this.graphicsDevice = graphicsDevice;
            this.content = content;
        }
        public abstract void Update(GameTime gameTime);
    }
}
