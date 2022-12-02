namespace Lab1.GameObjects
{
    public class Paddle
    {
        private PictureBox sprite;
        public int Score = 0;
        public readonly PaddleSide Side;
        public string nickname;
        public Form destination { get; private set; }

        public Point Position { get => sprite.Location; set => sprite.Location = value; }

        public Paddle(Image rsc, PaddleSide Side, Form form)
        {
            this.Side = Side;
            destination = form;

            sprite = new PictureBox
            {
                Size = new Size(Constants.ConstantPaddleSize),
                Image = rsc
            };

            switch (Side)
            {
                case PaddleSide.Left:
                    sprite.Location = new Point(0, (form.Height - sprite.Size.Height) / 2);
                    break;
                case PaddleSide.Right:
                    sprite.Location = new Point(form.Width - (sprite.Size.Width * 2), (form.Height - sprite.Size.Height) / 2); //sprite.Size.Height
                    break;
                default: throw new Exception("How? Side is not left and not right");
            }

            nickname = string.Empty;
            Score = 0;
            form.Controls.Add(sprite);
        }
    }
}
