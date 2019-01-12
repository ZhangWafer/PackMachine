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
            this.Location = (Point)new Size(2000, 0);         //窗体的起始位置为(x,y)
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Form1.TakeOrderBool == false)
            {
                int tempId = Form1.TempOrderId;
                DataTable dtOrderTable = SqlHelper.ExecuteDataTable("select * from Cater.CookbookSetInDateDetail where CookbookDateId = '" + tempId + "'");
           
                for (int i = 0; i < dtOrderTable.Rows.Count; i++)
                {
                    label2.Text += dtOrderTable.Rows[i][3].ToString() + "\r\n\r\n";
                }
                timer1.Stop();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1.TakeOrderBool = true;
            Form1.AllowTakeOrderBool = true;
            timer1.Start();
            label2.Text = "";
            
            
        }

        private void backForm_Load(object sender, EventArgs e)
        {
            timer1.Enabled=true;
            timer1.Start();
        }
    }
}
