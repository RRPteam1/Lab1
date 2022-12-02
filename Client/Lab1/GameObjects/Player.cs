namespace Lab1.GameObjects
{
    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public override string ToString() => $"Name={Name} Score={Score}";
    }
}
