using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebSiteChecker
{
    public partial class Form1 : Form
    {
        private BackgroundWorker bw = new BackgroundWorker();
        private HttpWebRequest myRequest;
        private string currDir = Directory.GetCurrentDirectory();
        private string fileName = "website.txt";
        private int updateTime = 1000 * 60;

        public Form1()
        {
            InitializeComponent();
            bw.DoWork += new DoWorkEventHandler(checkWebSite);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("Status");
            listView1.Columns.Add("Site");
            listView1.Columns.Add("LastCheckTime");
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            this.Icon = new Icon("icon001.ico");
            notifyIcon1.Icon = new Icon("icon001.ico");

            if (File.Exists(string.Format("{0}/{1}", currDir, fileName)))
            {
                StreamReader sr = new StreamReader(string.Format("{0}/{1}", currDir, fileName));
                while (!sr.EndOfStream)
                {
                    string _line = sr.ReadLine();
                    listView1.Items.Insert(0, new ListViewItem(new string[] { _line.Split(new char[] { ',' })[0], _line.Split(new char[] { ',' })[1], _line.Split(new char[] { ',' })[2] }));
                    listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
                sr.Close();
            }

            updateTimer();

            if (!bw.IsBusy)
            {
                bw.RunWorkerAsync(new object[] { listView1 });
            }
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            updateTimer();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Regex _regex = new Regex("http(|s)://.*");
            string _text = textBox1.Text;


            if (_text != string.Empty)
            {
                if (_regex.IsMatch(_text))
                {
                    bool itemExists = false;

                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (_text == item.SubItems[1].Text)
                        {
                            itemExists = true;
                            break;
                        }
                    }

                    if (!itemExists)
                    {

                        listView1.Items.Insert(0, new ListViewItem(new string[] { "Checking...", _text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }));
                        listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                        textBox1.Text = string.Empty;

                        try
                        {
                            if (bw.IsBusy != true)
                            {
                                object[] args = { listView1, 0 };
                                bw.RunWorkerAsync(args);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        textBox1.Text = "Url 重複輸入！";
                        textBox1.Focus();
                        textBox1.SelectAll();
                    }
                }
                else
                {
                    textBox1.Text = "Url 格式輸入錯誤！";
                    textBox1.Focus();
                    textBox1.SelectAll();
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                button1.PerformClick();
            }
        }

        private void checkWebSite(object sender, DoWorkEventArgs e)
        {
            Invoke(new MethodInvoker(delegate
            {
                try
                {
                    Console.WriteLine("Running...");
                    object[] objs = (object[])e.Argument;
                    ListView _ls = (ListView)objs[0];

                    foreach (ListViewItem item in _ls.Items)
                    {
                        Console.WriteLine(string.Format("WebSite : {0}", item.SubItems[1].Text));
                        Console.WriteLine(string.Format("Status : {0}", item.SubItems[0].Text));
                        item.SubItems[0].Text = "Checking...";
                        listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                        myRequest = (HttpWebRequest)HttpWebRequest.Create(item.SubItems[1].Text);
                        myRequest.Method = WebRequestMethods.Http.Get;
                        myRequest.BeginGetResponse(new AsyncCallback(finishWebRequest), myRequest);

                        if (objs.Length == 2 && item.Index == 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }));
        }

        void finishWebRequest(IAsyncResult result) {
            Invoke(new MethodInvoker(delegate {
                string _website = string.Empty;
                try
                {
                    HttpWebRequest request = result.AsyncState as HttpWebRequest;
                    _website = request.RequestUri.OriginalString;
                    HttpWebResponse response = request.EndGetResponse(result) as HttpWebResponse;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        foreach (ListViewItem item in listView1.Items)
                        {
                            if (item.SubItems[1].Text == response.ResponseUri.OriginalString)
                            {
                                item.SubItems[0].Text = response.StatusCode.ToString();
                                item.SubItems[2].Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                                break;
                            }
                        }
                    }

                    response.Close();

                    Console.WriteLine(response.StatusCode);
                }
                catch (Exception ex)
                {
                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (item.SubItems[1].Text.Equals(_website))
                        {
                            item.SubItems[0].Text = ex.Message;
                            item.SubItems[2].Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                            break;
                        }
                    }
                }
            }));
        }

        private void updateTimer()
        {
            timer1.Interval = (hScrollBar1.Value + 1) * 10 * updateTime;
            timer1.Start();
            toolStripStatusLabel1.Text = string.Format("更新時間：{0}分鐘", (hScrollBar1.Value + 1) * 10);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!bw.IsBusy)
            {
                bw.RunWorkerAsync(new object[] { listView1 });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine(e.Cancel);
            Console.WriteLine(e.CloseReason.ToString());

            StreamWriter sw = new StreamWriter(string.Format("{0}/{1}", currDir, fileName));

            foreach (ListViewItem item in listView1.Items)
            {
                sw.WriteLine(string.Format("{0},{1},{2}", item.SubItems[0].Text, item.SubItems[1].Text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            sw.Close();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Button);
            if (e.Button == MouseButtons.Right)
            {
                listView1.SelectedItems[0].Remove();
            }
        }
    }
}
