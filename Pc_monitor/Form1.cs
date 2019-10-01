using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using System.Speech;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using XinYu.Framework.Library.Implement.Security;


namespace Pc_monitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //设置全屏
            if (Properties.Settings.Default.全屏)
            {
                 this.FormBorderStyle = FormBorderStyle.None;
                 this.WindowState =FormWindowState.Maximized;
            }

        }

        private DataTable All_cookBook;

        private void Form1_Load(object sender, EventArgs e)
        {
            //启动定时器
            timer1.Enabled = true;
            timer1.Start();

            //显示第二显示屏画面
            // backForm bkForm = new backForm();
            // bkForm.Show();
            //读取用户表格---只在开机读取一次
        }


        //全局变量，存储当前二维码的
        private string Temp_pcNum = null;
        private string staffEnum = null;
        //CookbookSetInDate的表格
        DataTable dt2 = null;


        //语音
        SpeechRecognitionEngine recEngine = new SpeechRecognitionEngine();
        SpeechSynthesizer speech = new SpeechSynthesizer();

        public void SpeechVideo_Read(int rate, int volume, string speektext) //读
        {
            speech.Rate = rate;
            speech.Volume = volume;
            speech.SpeakAsync(speektext);
        }

        //打菜号暂时变量
        public static string TempOrderId = "";
        public static bool TakeOrderBool = true;
        private string personId = null;
        public static bool AllowTakeOrderBool = true;
        int whole_catlocation = Properties.Settings.Default.catlocation;

        private void timer1_Tick(object sender, EventArgs e)
        {
            //1秒跑一次的程序
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

                if (foodDic.Count == 0)
                {
                    richTextBox1.Clear();
                    MessageBox.Show("请选择至少一个菜品");
                    return;
                }
                try
                {
                    //解析扫码数据，拿取关键信息
                    var richText = richTextBox1.Text.Split('\n');
                    //二维码解码
                    var jsonText = Encrypt.Decode(richText[0]);
                    //json数据格式整理
                    JavaScriptObject jsonObj = JavaScriptConvert.DeserializeObject<JavaScriptObject>(jsonText);
                    Temp_pcNum = jsonObj["Num"].ToString();
                    personId = jsonObj["Id"].ToString();
                    staffEnum = jsonObj["staffEnum"].ToString();
                    var personCardId = jsonObj["Num"].ToString();//身份证号码
                    //如果是家属 则return
                    if (staffEnum == "Family")
                    {
                        richTextBox1.Text = "";
                        MessageBox.Show("家属不可打包");
                        return;
                    }
                    //检查是否存在这个人
                    Object o_result = null;
                    //检查是否存在这个人
                    //DataRow[] selectedResult = PcTable.Select("Id=" + personId);
                    if (staffEnum == "Police")
                    {
                        string select_Exist_pc = "select * from Cater.PCStaff where [Id]='" + personId + "'";
                        o_result = SqlHelper.ExecuteScalar(select_Exist_pc);
                    }
                    else
                    {
                        string select_Exist_worker = "select * from Cater.WorkerStaff where [Id]='" + personId + "'";
                        o_result = SqlHelper.ExecuteScalar(select_Exist_worker);
                    }
                    if (o_result == null)
                    {
                        richTextBox1.Text = "";
                        label2.Font = new Font("宋体粗体", 30);
                        label2.ForeColor = Color.Red;
                        label2.Text = "查无此人";
                        OrderFoodList.Clear();
                        OrderFoodPrice.Clear();
                       // ButtonNumClear();
                        foodDic.Clear();
                        Refresh();
                        return;
                    }
                    //查看是否过期以及余额是否足够
                    string imforUrl = null;
                    if (staffEnum == "Police")
                    {
                        imforUrl = "http://" + Properties.Settings.Default.header_url + @"/Interface/PC/GetPcStaff.ashx?InformationNum=" + personCardId;
                    }
                    else
                    {
                        imforUrl = "http://" + Properties.Settings.Default.header_url + "/Interface/Worker/GetWorkerStaff.ashx?informationNum=" + personCardId;
                    }
                    string dateResponse = "";
                    try
                    {
                        dateResponse = GetFunction(imforUrl);//照片url回复

                    }
                    catch (Exception ex)
                    {
                        richTextBox1.Text = "";
                        label2.Text = "网络错误";
                        OrderFoodList.Clear();
                        OrderFoodPrice.Clear();
                        //ButtonNumClear();
                        foodDic.Clear();
                        Refresh();
                        return;
                    }
                    JavaScriptObject jsonResponse2 = JavaScriptConvert.DeserializeObject<JavaScriptObject>(dateResponse);
                    JavaScriptObject json;
                    if (jsonResponse2["Msg"].ToString() == "失败")
                    {
                        richTextBox1.Text = "";
                        label2.Text = "账号验证失败";
                        OrderFoodList.Clear();
                        OrderFoodPrice.Clear();
                        //ButtonNumClear();
                        foodDic.Clear();
                        Refresh();
                        return;
                    }
                    if (staffEnum == "Police")
                    {
                        json = (JavaScriptObject)jsonResponse2["pcInfo"];
                    }
                    else
                    {
                        json = (JavaScriptObject)jsonResponse2["workerInfo"];
                    }

                    var effectDate = json["ValidityDate"];
                    if (effectDate != null)
                    {
                        TimeSpan ts = Convert.ToDateTime(effectDate.ToString().Split('T')[0]) - DateTime.Now;
                        if (ts.Hours < 0)
                        {
                            label2.Text = "用户已过期！";
                            richTextBox1.Text = "";
                            SpeechVideo_Read(0, 100, "用户已过期！");
                            OrderFoodList.Clear();
                            OrderFoodPrice.Clear();
                            foodDic.Clear();
                            Refresh();
                            //ButtonNumClear();
                            return;
                        }
                    }
                    string money = json["Amount"].ToString();
                    if ((Convert.ToDouble(money) - Convert.ToDouble(priceSum())) < 0)
                    {
                        label2.Text = "余额不足！";
                        richTextBox1.Text = "";
                        SpeechVideo_Read(0, 100, "余额不足！");
                        OrderFoodList.Clear();
                        OrderFoodPrice.Clear();
                        //ButtonNumClear();
                        foodDic.Clear();
                        Refresh();
                        return;
                    }

                    //显示扫码成功！大字体
                    richTextBox1.Text = "";
                    label2.Font = new Font("宋体粗体", 30);
                    label2.ForeColor = Color.Green;
                    label2.Text = "扫码成功！";
                    SpeechVideo_Read(0, 100, "扫码成功！");
                    //ButtonNumClear();

                    //扫码成功写入数据库
                    string foodstring=null;
                    foreach (var item in foodDic)
                    {
                        var str = item.Key;
                        str = str.Substring(0, str.Length - 1);
                        //定义正则表达式规则
                        Regex reg = new Regex(@"\d+\.\d+");
                        //返回一个结果集，找出菜名字！
                        MatchCollection result = reg.Matches(str);
                        str = str.Replace(result[0].Value, "");
                        //查数据库找出detailID
                        var selectID= SqlHelper.ExecuteScalar(@"SELECT [Id] FROM[XinYuSiteDB].[Cater].[CookbookSetInDateDetail] where CookbookDateId = '" + TempOrderId + "' and CookbookName = '"+ str + "'");
                        foodstring += selectID + ",";
                    }
                    foodstring = foodstring.Substring(0, foodstring.Length - 1);
                    //计算总价
                    string orderPrice = priceSum();
                    InsertRecoed(staffEnum, personId, whole_catlocation.ToString(), TempOrderId.ToString(), foodstring, orderPrice,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    //还原buuton的enable状态！
                    Return_Button();
                    //清楚已选列表
                    OrderFoodList.Clear();
                    OrderFoodPrice.Clear();
                    foodDic.Clear();
                    Refresh();
                }
                catch (Exception EX)
                {                  //  MessageBox.Show(EX.Message);
                    richTextBox1.Text = "";
                    //  MessageBox.Show(EX.Message);
                    label2.Text = "请出示正确的二维码";
                    SpeechVideo_Read(0, 100, "扫码错误！");
                    OrderFoodList.Clear();
                    OrderFoodPrice.Clear();
                    //ButtonNumClear();
                    Return_Button();
                }
                //写入文本，写入记录
            }

        }

        //插入一条记录
        private void InsertRecoed(string personEnum, string personId, string staffCanteen, string OrderId, string OrderNames, string orderPrice,
            string recordTime)
        {
            SqlConnection conn = new SqlConnection(Properties.Settings.Default.localsqlConn);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO [dbo].[TempRecord_Pack]([staffEnum],[staffId],[staffCanteen],[OrderId],[OrderNames],[orderPrice],[time],[upDateBool])VALUES('" +
                personEnum + "','" + personId + "','" + staffCanteen + "','" + OrderId + "','" + OrderNames + "','" + orderPrice + "','" + recordTime + "','false')";
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private int selectedNum = 0;

        private string priceSum()
        {
            double sumPrice = 0;
            foreach (var item in foodDic)
            {
               var  str = Regex.Replace(item.Key, @"[\u4e00-\u9fa5]", ""); //去除汉字
                sumPrice += Convert.ToDouble(str) * Convert.ToInt16(item.Value);
            }
            return sumPrice.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TakeOrderBool = true;
            label2.Text = "请扫码！总价：" + priceSum();
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

        //将菜单转换成字典
        private Dictionary<string, string> changeCookBookIntoDictionary(DataTable cookbookTable)
        {
            Dictionary<string, string> tempDictionary = new Dictionary<string, string>();
            for (int i = 0; i < cookbookTable.Rows.Count; i++)
            {
                tempDictionary.Add(cookbookTable.Rows[i][0].ToString(), cookbookTable.Rows[i][3].ToString());
            }
            return tempDictionary;
        }
        List<Button> btnAmountList = new List<Button>();
        private void button2_Click(object sender, EventArgs e)
        {
            //每次点更新更新一次数据库表
            All_cookBook = SqlHelper.ExecuteDataTable("select * from Cater.Cookbook");

            //价格表转字典
            Dictionary<string, string> cookDictionary = changeCookBookIntoDictionary(All_cookBook);

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
                dt2 = SqlHelper.ExecuteDataTable("select * from  Cater.CookbookSetInDate where CafeteriaId=" + catlocation +
                                               " and CookbookEnum='" + currentCat + "' and ChooseDate='" + todayDate + "'");
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
                var selectSetInDateId = dtRows[0][0];
                TempOrderId = selectSetInDateId.ToString();
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
                button.BackColor = Color.DarkGray;
                //   button.BackgroundImage = Properties.Resources.login_bg2;
                button.ForeColor = Color.Black;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Font = new Font("黑体粗体", 12);
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Name = "row*" + dtRows[i][0];
                //通过字典拿到价格
                button.Text = dtRows[i][3].ToString() + "\n " + cookDictionary[dtRows[i][2].ToString()] + "元";

                //通过坐标设置位置
                button.Size = new Size(110, 100);
                if (i < 6)
                {
                    button.Location = new Point(20 + 130 * i, 20);
                }
                else if (i >= 6 && i <= 12)
                {
                    button.Location = new Point(20 + 130 * (i - 6), 140);
                }
                else if (i >= 10 && i <= 14)
                {
                    button.Location = new Point(20 + 260 * (i - 10), 260);
                }
                else if (i >= 15 && i <= 19)
                {
                    button.Location = new Point(20 + 260 * (i - 15), 380);
                }
                //将groubox添加到页面上
                groupBox1.Controls.Add(button);
                //将button加入list
                btnAmountList.Add(button);
                button.MouseClick += new MouseEventHandler(button_MouseClick);
            }
        }
        List<string> OrderFoodList = new List<string>();
        List<string> OrderFoodPrice = new List<string>();


        Dictionary<string, int> foodDic = new Dictionary<string, int>();
        Dictionary<string, int> foodDic_g = new Dictionary<string, int>();

        public void button_MouseClick(object sender, EventArgs e)
        {
            //拿取数据
            Button button = (Button)sender;
            string namename = button.Text;
            namename = namename.Replace("\n", string.Empty);
            //把《菜名连同他妈的价格》加进去字典！！！！
            if (!foodDic.ContainsKey(namename))
            {
                foodDic.Add(namename, 1);
                Refresh();//刷新界面
            }
            else
            {
                foodDic[namename] = foodDic[namename] + 1;
                Refresh();//刷新界面
            }

            //调整label2字体
            label2.Font = new Font("宋体粗体", 30);
            label2.ForeColor = Color.Red;
            //添加显示菜品
            if (label2.Text == "请出示正确的二维码" || label2.Text == "扫码成功！" || label2.Text == "请扫码！")
            {
                label2.Text = "";
            }
            if (OrderFoodList.Count == 3)
            {
                label2.Text += "\n";
            }

        }

        private void ButtonNumClear()
        {
            foreach (var item in btnAmountList)
            {
                var itemList = item.Text.ToString().Split('\n');
                itemList[2] = "0";
                item.Text = itemList[0] + "\n" + itemList[1] + "\n" + itemList[2];
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            // ButtonNumClear();
            foodDic.Clear();
            Refresh();
            label2.Text = "";
           // OrderFoodPrice.Clear();
           // OrderFoodList.Clear();
            Return_Button();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            string header_url = Properties.Settings.Default.header_url;
            try
            {
                DataTable recorDataTable = GetTempRecord("Police");
                //提交字符串url
                if (recorDataTable.Rows.Count != 0)
                {
                    for (int i = 0; i < recorDataTable.Rows.Count; i++)
                    {
                        string get_url = "http://" + header_url + "/Interface/Synchronize/PCPackingSynchronize.ashx?pcId=" + recorDataTable.Rows[i][2] + "&cafeteriId=" + recorDataTable.Rows[i][3] + "&cookbookSetInDateId=" + recorDataTable.Rows[i][4] + "&cookbookSetInDateDetailIds=" + recorDataTable.Rows[i][5] + "&prices=" + recorDataTable.Rows[i][6];
                        GetFunction(get_url);
                    }
                }

                DataTable recorDataTable2 = GetTempRecord("Worker");
                if (recorDataTable2.Rows.Count != 0)
                {
                    //提交字符串url
                    for (int i = 0; i < recorDataTable2.Rows.Count; i++)
                    {
                        string get_url = "http://" + header_url + "/Interface/Synchronize/WorkerPackingSynchronize.ashx?workerId=" + recorDataTable2.Rows[i][2] + "&cafeteriId=" + recorDataTable2.Rows[i][3] + "&cookbookSetInDateId=" + recorDataTable2.Rows[i][4] + "&cookbookSetInDateDetailIds=" + recorDataTable2.Rows[i][5] + "&prices=" + recorDataTable2.Rows[i][6];

                        GetFunction(get_url);
                    }
                }
                this.Enabled = true;
                MessageBox.Show("同步完成！");
                ChangeUpdateTable();
            }
            catch (Exception essException)
            {
                this.Enabled = true;
                MessageBox.Show("同步错误！：" + essException.Message);
            }

        }

        //get方法
        //get方法
        private string GetFunction(string url)
        {

            System.Net.HttpWebRequest request;
            // 创建一个HTTP请求  
            request = (System.Net.HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 5000;
            //request.Method="get";  
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.StreamReader myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseText = myreader.ReadToEnd();
            myreader.Close();
            return responseText;
        }

        //拿本地记录表
        //获取record表
        private DataTable GetTempRecord(string Pc_Worker)
        {
            SqlConnection conn = new SqlConnection(Properties.Settings.Default.localsqlConn);
            conn.Open();
            SqlCommand sqlCommand = new SqlCommand("select * from dbo.TempRecord_Pack where staffEnum='" + Pc_Worker + "' and upDateBool='0'", conn);
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataTable tempDatetable = new DataTable();
            sqlDataAdapter.Fill(tempDatetable);
            conn.Close();
            return tempDatetable;
        }

        //更新updatebool标志位
        private void ChangeUpdateTable()
        {
            SqlConnection conn = new SqlConnection(Properties.Settings.Default.localsqlConn);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE [LocalRecord].[dbo].[TempRecord_Pack] SET [upDateBool] = 'true' WHERE [upDateBool]='false'";
            cmd.ExecuteNonQuery();
            conn.Close();
        }


        private void Return_Button()
        {
            foreach (Control c in groupBox1.Controls)
            {
                if (c is Button)
                {
                    //这里写代码逻辑
                    c.Enabled = true;
                }
            }
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定关机吗？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //关机代码
                System.Diagnostics.Process bootProcess = new System.Diagnostics.Process();
                bootProcess.StartInfo.FileName = "shutdown";
                bootProcess.StartInfo.Arguments = "/s";
                bootProcess.Start();
            }
        }
        private void Refresh()
        {
            groupBox2.Controls.Clear();
            int i = 0;
            foreach (var item in foodDic)
            {
                //实例化GroupBox控件  减号按钮
                Button button = new Button();
                button.Name = item.Key;
                button.Image = Properties.Resources.减号按钮;
                button.BackColor = Color.Transparent;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                //通过坐标设置位置
                button.Size = new Size(40, 40);
                button.Location = new Point(350, 20+i*50);
                //将groubox添加button到页面上
                groupBox2.Controls.Add(button);

                //////////////////////////////////////lable添加代码
                Label label = new Label();
                label.Text = item.Key + "×" + item.Value;
                label.ForeColor = Color.Black;
                label.Font = new Font("黑体粗体", 18);
                label.Location = new Point(5, 20+i*50);
                label.Size = new Size(320, 35);
                label.TextAlign = ContentAlignment.MiddleCenter;
                groupBox2.Controls.Add(label);
                //将button事件加入
                button.MouseClick += new MouseEventHandler(button_MouseClick2);
                i++;
            }
        }

        public void button_MouseClick2(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if(foodDic[button.Name]>0)
            {
                foodDic[button.Name] = foodDic[button.Name] - 1;
                if (foodDic[button.Name] == 0)
                {
                    foodDic.Remove(button.Name);
                }
                Refresh();
            }
        }
    }
}



