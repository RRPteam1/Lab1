using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using PongGame;
using System;

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
        private Texture2D sprite;
        private DateTime lastCollisiontime = DateTime.MinValue;
        private TimeSpan minCollisionTimeGap = TimeSpan.FromSeconds(0.2);

        public readonly PaddleSide Side;
        public int Score = 0;
        public Vector2 Position = new Vector2();
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }

        #region Collision objects
        public Rectangle TopCollisionArea
        {
            get { return new Rectangle(Position.ToPoint(), new Point(Costants.CostantPaddleSize.X, 4)); }
        }

        public Rectangle BottomCollisionArea
        {
            get
            {
                return new Rectangle(
                    (int)Position.X, FrontCollisionArea.Bottom,
                    Costants.CostantPaddleSize.X, 4
                );
            }
        }

        public Rectangle FrontCollisionArea
        {
            get
            {
                Point pos = Position.ToPoint();
                pos.Y += 4;
                Point size = new Point(Costants.CostantPaddleSize.X, Costants.CostantPaddleSize.Y - 8);

                return new Rectangle(pos, size);
            }
        }
        #endregion // Collision objects

        // Sets which side the paddle is
        public Paddle(PaddleSide side)
        {
            Side = side;
        }

        public void LoadContent(ContentManager content)
        {
            sprite = content.Load<Texture2D>("paddle.png"); // todo
        }

        public void Initialize()
        {
            // Figure out where to place the paddle
            int x;
            if (Side == PaddleSide.Left)
                x = Costants.CostantGoalSize;
            else if (Side == PaddleSide.Right)
                x = Costants.CostantPlayField.X - Costants.CostantPaddleSize.X - Costants.CostantGoalSize;
            else
                throw new Exception("Side is not `Left` or `Right`");

            Position = new Vector2(x, (Costants.CostantPlayField.Y / 2) - (Costants.CostantPaddleSize.Y / 2));
            Score = 0;

            // Set bounds
            TopmostY = 0;
            BottommostY = Costants.CostantPlayField.Y - Costants.CostantPaddleSize.Y; // todo
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
            spriteBatch.Draw(sprite, Position); // todo
        }

        public bool Collides(Ball ball, out PaddleCollision typeOfCollision)
        {
            typeOfCollision = default;
            // Make sure enough time has passed for a new collisions
            // (this prevents a bug where a user can build up a lot of speed in the ball)
            if (DateTime.Now < (lastCollisiontime.Add(minCollisionTimeGap)))
                return false;

            // Top & bottom get first priority
            if (ball.CollisionField.Intersects(TopCollisionArea))
            {
                typeOfCollision = PaddleCollision.WithTop;
                lastCollisiontime = DateTime.Now;
                return true;
            }

            if (ball.CollisionField.Intersects(BottomCollisionArea))
            {
                typeOfCollision = PaddleCollision.WithBottom;
                lastCollisiontime = DateTime.Now;
                return true;
            }

            // And check the front
            if (ball.CollisionField.Intersects(FrontCollisionArea))
            {
                typeOfCollision = PaddleCollision.WithFront;
                lastCollisiontime = DateTime.Now;
                return true;
            }
            // todo
            return true;
        }
    }
}

