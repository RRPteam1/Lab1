using Microsoft.Xna.Framework;

namespace Client.ClientCode
{
    public static class Costants
    {
        public static readonly Point CostantPlayField = new Point(320, 240);
        public static readonly Vector2 CostantScreenCenter = new Vector2(CostantPlayField.X / 2f, CostantPlayField.Y / 2f);
        public static readonly Point CostantBallSize = new Point(8, 8);
        public static readonly Point CostantPaddleSize = new Point(8, 44);
        public static readonly int CostantGoalSize = 12;
        public static readonly float CostantPaddleSpeed = 100f;
    }
}
