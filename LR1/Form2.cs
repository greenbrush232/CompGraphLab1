using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LR1
{
    public partial class Form2 : Form
    {
        public int[,] StructElem;
        public Form2()
        {
            StructElem = null;
            InitializeComponent();
            textBox1.Text = "0";
            textBox2.Text = "0";
            textBox3.Text = "0";
            textBox4.Text = "0";
            textBox5.Text = "0";
            textBox6.Text = "0";
            textBox7.Text = "0";
            textBox8.Text = "0";
            textBox9.Text = "0";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((textBox1.Text != "1" && textBox1.Text != "0")
                || (textBox2.Text != "1" && textBox2.Text != "0")
                || (textBox3.Text != "1" && textBox3.Text != "0")
                || (textBox4.Text != "1" && textBox4.Text != "0")
                || (textBox5.Text != "1" && textBox5.Text != "0")
                || (textBox6.Text != "1" && textBox6.Text != "0")
                || (textBox7.Text != "1" && textBox7.Text != "0")
                || (textBox8.Text != "1" && textBox8.Text != "0")
                || (textBox9.Text != "1" && textBox9.Text != "0"))
                MessageBox.Show("Неправильный ввод данных", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                StructElem = new int[3, 3] { { Convert.ToInt32(textBox1.Text), Convert.ToInt32(textBox2.Text), Convert.ToInt32(textBox3.Text) },
                {Convert.ToInt32(textBox4.Text), Convert.ToInt32(textBox5.Text), Convert.ToInt32(textBox6.Text) },
                {Convert.ToInt32(textBox7.Text), Convert.ToInt32(textBox8.Text), Convert.ToInt32(textBox9.Text) } };
                Close();
            }
        }
    }
}
