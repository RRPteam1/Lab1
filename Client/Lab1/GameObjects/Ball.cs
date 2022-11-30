namespace Lab1.GameObjects
{
    public class Ball
    {
        private readonly PictureBox sprite;
        private readonly Form destination;
        public Point Position { get => sprite.Location; set => sprite.Location = value; }
        //private readonly Point Speed;

        public Rectangle CollisionField { get => new(Position, new Size(Constants.ConstantBallSize)); }

        public Ball(Image rsc, Form destination)
        {
            this.destination = destination;
            sprite = new PictureBox
            {
                Size = new Size(Constants.ConstantBallSize),
                Image = rsc,
                Location = Constants.ConstantScreenCenter,
            };

            destination.Controls.Add(sprite);
        }
    }
}
