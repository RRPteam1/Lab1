using System.Drawing;

namespace Server.GameObjects
{
    public class Ball
    {
        private Random random = new();
        public Point Position = Utils.Constants.ConstantScreenCenter;
        public Point Speed = Utils.Constants.ConstantBallSpeed;

        //boundaries
        public int LeftmostX { get; private set; }
        public int RightmostX { get; private set; }
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }

        public Rectangle CollisionArea
        {
            get { return new Rectangle(Position, new Size(Utils.Constants.ConstantBallSize)); }
        }
        public void Initialize()
        {
            Rectangle playAreaRect = new(new Point(0, 0), new Size(Utils.Constants.ConstantPlayField));
            Position = Utils.Constants.ConstantScreenCenter;
            Speed = Utils.Constants.ConstantBallSpeed;

            //randomize direction
            if (random.Next() % 2 == 1) Speed.X *= -1;
            if (random.Next() % 2 == 1) Speed.Y *= -1;

            LeftmostX = 0;
            RightmostX = playAreaRect.Width - Utils.Constants.ConstantBallSize.X;
            TopmostY = 0;
            BottommostY = playAreaRect.Height - Utils.Constants.ConstantBallSize.Y;
        }

        /// <summary>
        /// Move ball on the server
        /// </summary>
        /// <param name="gameTime"></param>
        public void ServerSideUpdate(DateTime gameTime)
        {
            Position.X += Speed.X;
            Position.Y += Speed.Y;
        }
    }
}
