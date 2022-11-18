namespace Server
{
    public enum PaddleSide : uint
    {
        None,
        Left,
        Right
    };

    public class Paddle
    {
        public readonly PaddleSide Side;
        public Paddle(PaddleSide side)
        {
            Side = side;
        }
        public void Initialize() { }
    }
}
