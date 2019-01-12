using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;



namespace Pc_monitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //设置全屏
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            //this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
        }


        private DataTable PcTable;
        private DataTable WorkerTable;
        private DataTable All_OrderDetail;
        private DataTable All_OrderTable;

        private void Form1_Load(object sender, EventArgs e)
        {
            //启动定时器
            timer1.Enabled = true;
            timer1.Start();
            //显示第二显示屏画面
            // backForm bkForm = new backForm();
            // bkForm.Show();
            //读取用户表格---只在开机读取一次
            try
            {
                PcTable = SqlHelper.ExecuteDataTable("select * from Cater.PCStaff");
                WorkerTable = SqlHelper.ExecuteDataTable("select * from Cater.WorkerStaff");
                All_OrderDetail = SqlHelper.ExecuteDataTable("select * from Cater.CookbookSetInDateDetail");
                All_OrderTable = SqlHelper.ExecuteDataTable("select * from Cater.CookbookSetInDate");
            }
            catch (Exception exception)
            {
                MessageBox.Show("数据库连接失败!" + exception.Message);
            }
        }


        private int timer_count_10s = 59;
        //全局变量，存储当前二维码的
        private string Temp_pcNum = null;
        private string staffEnum = null;
        //CookbookSetInDate的表格
        DataTable dt2 = null;



        //打菜号暂时变量
        public static int TempOrderId = 0;
        public static bool TakeOrderBool = true;
        private string personId = null;
        public static bool AllowTakeOrderBool = true;
        int whole_catlocation = Properties.Settings.Default.catlocation;

        private void timer1_Tick(object sender, EventArgs e)
        {
            //1秒跑一次的程序1
            if (AllowTakeOrderBool == true)
            {
                if (button1.Enabled == false)
                {
                    button1.Enabled = true;
                }
            }
            //1秒跑一次的程序2
            if (richTextBox1.Text.Contains("\n"))
            {

                if (OrderFoodList.Count == 0)
                {
                    richTextBox1.Clear();
                    MessageBox.Show("请选择至少一个菜品");
                    return;
                }
                try
                {
                    //解析扫码数据，拿取关键信息
                    string jsonText = richTextBox1.Text;
                    JavaScriptObject jsonObj = JavaScriptConvert.DeserializeObject<JavaScriptObject>(jsonText);
                    Temp_pcNum = jsonObj["Num"].ToString();
                    personId = jsonObj["Id"].ToString();
                    staffEnum = jsonObj["staffEnum"].ToString();
                    //检查是否存在这个人
                    DataRow[] selectedResult = PcTable.Select("Id=" + personId);
                    DataRow[] selectedResult_worker = WorkerTable.Select("Id=" + personId);
                    if (selectedResult.Length == 0 && selectedResult_worker.Length == 0)
                    {
                        richTextBox1.Text = "";
                        label2.Font = new Font("宋体粗体", 30);
                        label2.ForeColor = Color.Red;
                        label2.Text = "查无此人";
                        return;
                    }


                    //显示扫码成功！大字体
                    richTextBox1.Text = "";
                    label2.Font = new Font("宋体粗体", 30);
                    label2.ForeColor = Color.GreenYellow;
                    label2.Text = "扫码成功！";

                    //扫码成功写入数据库
                    string[] orderFoodArray = OrderFoodList.ToArray(); //拼凑选中的字符串
                    string foodstring = string.Join(",", orderFoodArray);
                    InsertRecoed(staffEnum, personId, whole_catlocation.ToString(), foodstring,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                }
                catch (Exception EX)
                {
                    richTextBox1.Text = "";
                    //  MessageBox.Show(EX.Message);
                    label2.Text = "请出示正确的二维码";
                }
                //写入文本，写入记录
            }

            //60秒跑一次程序

        }

        //插入一条记录
        private void InsertRecoed(string personEnum, string personId, string staffCanteen, string OrderNames,
            string recordTime)
        {
            SqlConnection conn = new SqlConnection(Properties.Settings.Default.localsqlConn);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO [dbo].[TempRecord_Pack]([staffEnum],[staffId],[staffCanteen],[OrderNames],[time],[upDateBool])VALUES('" +
                personEnum + "','" + personId + "','" + staffCanteen + "','" + OrderNames + "','" + recordTime + "','false')";
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private int selectedNum = 0;


        private void button1_Click(object sender, EventArgs e)
        {
            TakeOrderBool = true;
            label2.Text = "请扫码！";
            label2.Font = new Font("宋体粗体", 30);
            label2.ForeColor = Color.Red;
            richTextBox1.Focus();
        }

        private void AppendXml(string Type, string Id, string CafeteriaId, string CookbookSetInDateId, string Datatime)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@"d:\User.xml");
            XmlNode root = xmlDoc.SelectSingleNode("Root"); //查找<bookstore>
            XmlElement xe1 = xmlDoc.CreateElement("User"); //创建一个<book>节点
            xe1.SetAttribute("Type", Type); //设置该节点的genre属性
            xe1.SetAttribute("Id", Id); //设置该节点的ISBN属性

            XmlElement xesub1 = xmlDoc.CreateElement("CafeteriaId"); //添加一个名字为title的子节点
            xesub1.InnerText = CafeteriaId; //设置文本NM
            xe1.AppendChild(xesub1); //把title添加到<book>节点中

            XmlElement xesub2 = xmlDoc.CreateElement("CookbookSetInDateId");
            xesub2.InnerText = CookbookSetInDateId;
            xe1.AppendChild(xesub2);

            XmlElement xesub3 = xmlDoc.CreateElement("Datatime");
            xesub3.InnerText = Datatime;
            xe1.AppendChild(xesub3);

            root.AppendChild(xe1); //把book添加到<bookstore>根节点中
            xmlDoc.Save(@"d:\User.xml");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //分割线·············分割线//
            int catlocation = Properties.Settings.Default.catlocation;
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            string st1 = Properties.Settings.Default.b1; //早餐前

            string st2 = Properties.Settings.Default.b2; //早餐后

            string st3 = Properties.Settings.Default.l1; //午餐前

            string st4 = Properties.Settings.Default.l2; //午餐后

            DateTime b1DateTime = Convert.ToDateTime(st1);

            DateTime b2DateTime = Convert.ToDateTime(st2);

            DateTime l1DateTime = Convert.ToDateTime(st3);

            DateTime l2DateTime = Convert.ToDateTime(st4);

            string currentCat = "";
            string showString = "";
            if (DateTime.Compare(currentTime, b1DateTime) > 0 && DateTime.Compare(currentTime, b2DateTime) < 0)
            {
                currentCat = "Breakfast";
                showString = "早餐";
            }
            else if (DateTime.Compare(currentTime, l1DateTime) > 0 && DateTime.Compare(currentTime, l2DateTime) < 0)
            {
                currentCat = "Lunch";
                showString = "午餐";
            }
            else
            {
                currentCat = "Supper";
                showString = "晚餐";
            }
            if (dt2 != null)
            {
                dt2.Clear();
            }

            //拿出今天的日期
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            try
            {
                dt2 =SqlHelper.ExecuteDataTable("select * from  Cater.CookbookSetInDate where CafeteriaId=" + catlocation +
                                               " and CookbookEnum='" + currentCat + "' and ChooseDate='" + todayDate +"'");
            }
            catch (Exception)
            {
                MessageBox.Show("查无排餐！");
                return;
            }

            int rowCounts = dt2.Rows.Count;
            var dtRows = dt2.Rows;
            try
            {
               var selectSetInDateId= dtRows[0][0];
               dt2 = SqlHelper.ExecuteDataTable("select * from Cater.CookbookSetInDateDetail where CookbookDateId='" + selectSetInDateId + "'");
               dtRows = dt2.Rows;
               rowCounts = dt2.Rows.Count;
            }
            catch (Exception)
            {
                MessageBox.Show("查无排餐！");
                return;
            }
                groupBox1.Controls.Clear();
                for (int i = 0; i < rowCounts; i++)
                {
                    //实例化GroupBox控件
                    Button button = new Button();
                    button.BackgroundImage = Properties.Resources.login_bg2;
                    button.ForeColor = Color.White;
                    button.Font = new Font("宋体粗体", 14);
                    button.TextAlign = ContentAlignment.MiddleCenter;
                    button.Name = "row*" + dtRows[i][0];
                    button.Text = dtRows[i][3].ToString();

                    //通过坐标设置位置
                    button.Size = new Size(200, 40);
                    if (i < 5)
                    {
                        button.Location = new Point(20 + 260 * i, 20);
                    }
                    else if (i >= 5 && i <= 9)
                    {
                        button.Location = new Point(20 + 260 * (i - 5), 80);
                    }
                    else if (i >= 10 && i <= 14)
                    {
                        button.Location = new Point(20 + 260 * (i - 10), 140);
                    }
                    else if (i >= 15 && i <= 19)
                    {
                        button.Location = new Point(20 + 260 * (i - 15), 200);
                    }
                    //将groubox添加到页面上
                    groupBox1.Controls.Add(button);
                    button.MouseClick += new MouseEventHandler(button_MouseClick);
}
        }
        List<string> OrderFoodList=new List<string>();
        public void button_MouseClick(object sender, EventArgs e)
        {
            //拿取数据
            Button button = (Button)sender;
            var NameArray= button.Name.Split('*');
            //调整label2字体
            label2.Font = new Font("黑体", 22);
            label2.ForeColor = Color.Red;
            //添加显示菜品
            if (label2.Text == "请出示正确的二维码" || label2.Text == "扫码成功！"||label2.Text=="请扫码！")
            {
                label2.Text = "";
            }
            label2.Text += button.Text + " ";
            //添加菜品进数组
            OrderFoodList.Add( NameArray[1]);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            label2.Text = "";
            OrderFoodList.Clear();
        }

    }
}



