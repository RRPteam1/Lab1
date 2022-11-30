namespace Lab1
{
    public class Constants
    {
        public static readonly Point ConstantPlayField = new(800, 570); //set new resize event and add ctor
        public static readonly Point ConstantScreenCenter = new(ConstantPlayField.X / 2, ConstantPlayField.Y / 2);
        public static readonly Point ConstantBallSize = new(16, 16); //diff = 800 -> 16 then 320 -> 8 means = 1 -> x => 
        public static readonly Point ConstantPaddleSize = new(16, 168);
        public static readonly Point ConstantBallSpeed = new(3, 3);
        public static readonly int ConstantGoalSize = 12;
        public static readonly int ConstantPaddleSpeed = 8;
    }
}

//320 -> 8
//1 -> y
//y = (1*8)/320 = 0.025
//800 -> 16
//1 -> x
//x = (16*1)/800 = 0.02

//close enough for 0.02 => so we will use this diff

//800 600 => (16,16)

//мы можем использовать таймер в видовс формах, чтобы передовать его в качестве GameTimer