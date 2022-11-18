using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Pong
{
    // The ball that's bounced around
    public class Ball
    {
        public static Vector2 InitialSpeed = new Vector2(60f, 60f);
        private Random rand = new Random(); //for randomization of direction

        public Vector2 Position = new Vector2();
        public Vector2 Speed;

        //boundaries
        public int LeftmostX { get; private set; }
        public int RightmostX { get; private set; }
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }
        //!end boundaries

        public void LoadContent(ContentManager content)
        {
            //todo sprites
        }

        /// <summary>
        /// Center the ball on the board
        /// </summary>
        public void Initialize()
        {
            // Center the ball
            Rectangle playAreaRect = new Rectangle(new Point(0, 0), new Point(0, 0));
            Position = playAreaRect.Center.ToVector2();
            Position = Vector2.Subtract(Position, new Point(8, 8).ToVector2() / 2f);

            //set the velocity
            Speed = InitialSpeed;

            //randomize direction
            if (rand.Next() % 2 == 1)
                Speed.X *= -1;
            if (rand.Next() % 2 == 1)
                Speed.Y *= -1;

            //set bounds
            LeftmostX = 0;
            RightmostX = 0;
            TopmostY = 0;
            BottommostY = 0;
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
    }
}

