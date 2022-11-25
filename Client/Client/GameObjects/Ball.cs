using System;
using Client.ClientCode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client.GameObjects
{
    // The ball that's bounced around
    public class Ball
    {
        public static Vector2 InitialSpeed = new Vector2(60f, 60f);
        private Texture2D sprite;
        private Random rand = new Random(); //for randomization of direction

        public Vector2 Position = new Vector2();
        public Vector2 Speed;

        //boundaries
        public int LeftmostX { get; private set; }
        public int RightmostX { get; private set; }
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }
        //!end boundaries

        public Rectangle CollisionField
        {
            get { return new Rectangle(Position.ToPoint(), Costants.CostantBallSize); }
        }

        public void LoadContent(ContentManager content) => sprite = content.Load<Texture2D>("ball"); //todo sprites
        

        public void Initialize()
        {
            // Center the ball
            Rectangle playAreaRect = new Rectangle(new Point(0, 0), Costants.CostantPlayField);
            Position = playAreaRect.Center.ToVector2();
            Position = Vector2.Subtract(Position, Costants.CostantBallSize.ToVector2() / 2f);

            //set the velocity
            Speed = InitialSpeed;

            //randomize direction
            if (rand.Next() % 2 == 1)
                Speed.X *= -1;
            if (rand.Next() % 2 == 1)
                Speed.Y *= -1;

            //set bounds
            LeftmostX = 0;
            RightmostX = playAreaRect.Width - Costants.CostantBallSize.X;
            TopmostY = 0;
            BottommostY = playAreaRect.Height - Costants.CostantBallSize.Y;
        }

        /// <summary>
        /// Move ball on the server
        /// </summary>
        /// <param name="gameTime"></param>
        public void ServerSideUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position = Vector2.Add(Position, timeDelta * Speed); //ddd the distance
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) => spriteBatch.Draw(sprite, Position, Color.White);
        
    }
}

