using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Pc_monitor;
using XinYu.Framework.Library.Factory;
using XinYu.Framework.Library.Interface;
using Newtonsoft.Json;

namespace Pc_monitor
{
    public partial class login : Form
    {
        public login()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string account = textBox1.Text;
            string password = textBox2.Text;

            var staticHashCryptorgrapher = CommonLibraryFactory.CreateInstance<IHashCryptographer>();
            var hashedOldPwd = staticHashCryptorgrapher.CreateHash(password);
            //hash加密

            if (account.Length<=14)
            {
                DataTable accountTable = SqlHelper.ExecuteDataTable("select * from Cater.PCStaff where PCNum=" + account);
                string tableData = accountTable.Rows[0][5].ToString();

                if (hashedOldPwd == tableData )
                {
                    MessageBox.Show("登陆成功！");
                }
               
            }
            else 
            {
                DataTable accountTable2 = SqlHelper.ExecuteDataTable("select * from Cater.WorkerStaff where InformationNum=" + account);
                if (hashedOldPwd == accountTable2.Rows[0][1])
                {
                    MessageBox.Show("登陆成功！");
                }
               
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = SqlHelper.ExecuteDataTable("select * from Cater.PCStaff");
                MessageBox.Show("成功！");
            }
            catch (Exception exxx)
            {

                MessageBox.Show(exxx.Message);
            }

        }

      

    }

}
