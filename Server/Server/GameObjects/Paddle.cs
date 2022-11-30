using System.Drawing;

namespace Server.GameObjects
{
    public class Paddle
    {
        private DateTime lastCollisiontime = DateTime.MinValue;
        private TimeSpan minCollisionTimeGap = TimeSpan.FromSeconds(0.2);

        public readonly PaddleSide Side;
        public int Score = 0;
        public Point Position = new();
        public int TopmostY { get; private set; }
        public int BottommostY { get; private set; }

        //set side of paddle
        public Paddle(PaddleSide side) => Side = side;

        //hit reg
        public Rectangle CollisionArea
        {
            get { return new Rectangle(Position, new Size(Utils.Constants.ConstantPaddleSize)); }
        }

        // Puts the paddle in the middle of where it can move
        public void Initialize()
        {
            // Figure out where to place the paddle
            int x;
            if (Side == PaddleSide.Left)
                x = Utils.Constants.ConstantGoalSize;
            else if (Side == PaddleSide.Right)
                x = Utils.Constants.ConstantPlayField.X - Utils.Constants.ConstantPaddleSize.X - Utils.Constants.ConstantGoalSize; //x = Utils.Constants.ConstantPlayField.X - Utils.Constants.ConstantPaddleSize.X * 2 - 10 - Utils.Constants.ConstantGoalSize;
            else
                throw new Exception("Side is not `Left` or `Right`");

            Position = new Point(x, (Utils.Constants.ConstantPlayField.Y / 2) - (Utils.Constants.ConstantPaddleSize.Y / 2));
            Score = 0;

            // Set bounds
            TopmostY = 0;
            BottommostY = Utils.Constants.ConstantPlayField.Y - Utils.Constants.ConstantPaddleSize.Y;
        }

        //what part gets hit
        public bool Collides(GameObjects.Ball ball, out PaddleCollision typeOfCollision)
        {
            typeOfCollision = PaddleCollision.None;

            //delta time to prevent buildup speed
            if (DateTime.Now < (lastCollisiontime.Add(minCollisionTimeGap))) return false;

            //Top & bottom -> first priority
            if (ball.CollisionArea.IntersectsWith(CollisionArea))
            {
                typeOfCollision = PaddleCollision.WithTop;
                lastCollisiontime = DateTime.Now;
                return true;
            }

            if (ball.Position.Y <= 0 || ball.Position.Y + Utils.Constants.ConstantBallSize.Y >= Utils.Constants.ConstantPlayField.Y)
            {
                typeOfCollision = PaddleCollision.WithBottom;
                lastCollisiontime = DateTime.Now;
                return true;
            }

            //check the front
            if (ball.Position.X <= 0 || ball.Position.X + Utils.Constants.ConstantBallSize.X >= Utils.Constants.ConstantPlayField.X)
            {
                typeOfCollision = PaddleCollision.WithFront;
                lastCollisiontime = DateTime.Now;
                return true;
            }

            //no collision
            return false;
        }
    }
}
