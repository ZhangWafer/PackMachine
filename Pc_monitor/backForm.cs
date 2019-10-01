using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pc_monitor
{
    public partial class backForm : Form
    {
        public backForm()
        {
            InitializeComponent();
        //  this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
          //  this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.Manual; //窗体的位置由Location属性决定
            this.Location = (Point)new Size(0, 0);         //窗体的起始位置为(x,y)
         //       this.Location = (Point)new Size(2000, 0);         //窗体的起始位置为(x,y)
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            richTextBox1.Text = "番茄炒蛋\n牛肉炒鸡腿\n";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1.TakeOrderBool = true;
            Form1.AllowTakeOrderBool = true;
            timer1.Start();
        }

        private void backForm_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();
            richTextBox1.ForeColor =Color.Red;
            richTextBox1.Font = new Font("黑体", 20);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
