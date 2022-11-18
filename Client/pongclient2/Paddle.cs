using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Pong
{
    public enum PaddleSide : uint
    {
        None,
        Left,
        Right
    };

    public enum PaddleCollision
    {
        None,
        WithTop,
        WithFront,
        WithBottom
    };

    public class Paddle
    {
        public readonly PaddleSide Side;
        public int Score = 0;
        public Vector2 Position = new Vector2();
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }

        // Sets which side the paddle is
        public Paddle(PaddleSide side)
        {
            Side = side;
        }

        public void LoadContent(ContentManager content)
        {
            // todo
        }

        public void Initialize()
        {
            // todo
        }

        public void ClientSideUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float dist = timeDelta;

            KeyboardState kbs = Keyboard.GetState();
            if (kbs.IsKeyDown(Keys.Up))
                Position.Y -= dist;
            else if (kbs.IsKeyDown(Keys.Down))
                Position.Y += dist;

            // bounds checking
            if (Position.Y < TopmostY)
                Position.Y = TopmostY;
            else if (Position.Y > BottommostY)
                Position.Y = BottommostY;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // todo
        }

        public bool Collides(Ball ball, out PaddleCollision typeOfCollision)
        {
            typeOfCollision = default;
            // todo
            return true;
        }
    }
}

