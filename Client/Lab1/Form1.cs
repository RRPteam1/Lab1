namespace Lab1
{
    public partial class Form1 : Form
    {
        int port = 6000;
        string ip = "127.0.0.1";
        string nickname = RandomNickname(10);

        public Form1()
        {
            InitializeComponent();
            textBox1.Text = nickname;
            textBox2.Text = ip;
            textBox3.Text = port.ToString();
        }

        public static string RandomNickname(int length)
        {
            Random random = new Random(); //for generator of random nickname
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //button setting clicked, set visible some elements
        private void button2_Click(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            panel2.Visible = true;


            button1.Enabled = false;
            button1.Visible = false;

            button2.Enabled = false;
            button2.Visible = false;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            textBox3.ShortcutsEnabled = false;
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar); //only accept numbers as input
        }
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            textBox2.ShortcutsEnabled = false;
            e.Handled = !(e.KeyChar == '.') && !char.IsNumber(e.KeyChar) && !char.IsControl(e.KeyChar); //only accept numbers and '.'
        }

        //button back clicked, set visible some elements
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Equals(string.Empty) || textBox2.Text.Equals(string.Empty) || textBox3.Text.Equals(string.Empty))
            {
                MessageBox.Show("Ни одно из полей не должно быть пустым!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            panel2.Enabled = false;
            panel2.Visible = false;


            button1.Enabled = true;
            button1.Visible = true;

            button2.Enabled = true;
            button2.Visible = true;
        }

        //button accept changes clicked
        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Equals(string.Empty) || textBox2.Text.Equals(string.Empty) || textBox3.Text.Equals(string.Empty))
            {
                MessageBox.Show("Ни одно из полей не должно быть пустым!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            nickname = textBox1.Text;
            ip = textBox2.Text;
            port = Convert.ToInt32(textBox3.Text);
            MessageBox.Show("Настройки успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            textBox1.MaxLength = 10; //max length for nickname
            textBox1.ShortcutsEnabled = false;
        }

        //connect button
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Form2 form2 = new(nickname, ip, port);
                form2.Show();
                //form2.ShowDialog();
                //this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Невозможно подключится", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //todo: закрывать приложение, если человек нажал да
            Application.Exit();
        }
    }
}