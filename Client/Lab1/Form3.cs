namespace Lab1
{
    public partial class Form3 : Form
    {
        Form toClose;
        List<GameObjects.Player> players = new();
        public Form3(Form fromForm, string _players)
        {
            InitializeComponent();
            toClose = fromForm;
            _players = Utils.Converter.ToStr(_players);

            var arr1 = _players.Split('\n');
            foreach (var player in arr1)
            {
                var corted = player.Split('\t');
                if (corted.Length == 1) break;
                int sc = Convert.ToInt32(corted[1]);
                players.Add(new GameObjects.Player() { Name = corted[0].Replace("\0", string.Empty), Score = sc });
            }

            listBox1.Items.AddRange(players.ToArray());
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            toClose.Close();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e) => toClose.Close();       
    }
}
