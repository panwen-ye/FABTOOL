using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FABTOOL
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

        }

        private void Form3_Load_1(object sender, EventArgs e)
        {

        }

        

        private delegate void delInfoList(string text);

        private void SetrichTextBox(string value)
        {

            if (richTextBox1.InvokeRequired)//其它线程调用
            {
                delInfoList d = new delInfoList(SetrichTextBox);
                richTextBox1.Invoke(d, value);
            }
            else//本线程调用
            {
                if (richTextBox1.Lines.Length > 100)
                {
                    richTextBox1.Clear();
                }

                richTextBox1.Focus(); //让文本框获取焦点 
                richTextBox1.Select(richTextBox1.TextLength, 0);//设置光标的位置到文本尾
                richTextBox1.ScrollToCaret();//滚动到控件光标处 
                richTextBox1.AppendText(value);//添加内容
            }
        }

        static AutoResetEvent myResetEvent = new AutoResetEvent(false);

        private void 多线程()//创建线程执行 计时
        {
            ThreadStart entry = new ThreadStart(runtask);
            Thread workThread = new Thread(entry);
            workThread.IsBackground = true;
            workThread.Start();
            //a = workThread.GetHashCode();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            多线程();
            backThread();
            
        }

        private void runtask() {
            SetrichTextBox("逐个计数到100为止\r\n");
            for (int i = 0; i < 10; i++)
            {
                
                
                Thread.Sleep(1000);
                //myResetEvent.WaitOne();
            }
            PD = false;

        }

        private void backThread()//创建后台线程执行 判断能否执行
        {
            ThreadStart entry = new ThreadStart(recordSth);
            Thread workThread = new Thread(entry);
            workThread.IsBackground = true;
            workThread.Start();
        }

        private void recordSth() {

            while (PD)
            {
                Thread.Sleep(1000);
                
                SetrichTextBox(DateTime.Now.ToString() + "   运行中\r\n");
            }

        }

        private void 计时()
        {
            SetrichTextBox("逐个计数到100为止\r\n");
            for (int i = 0; i < 101; i++)
            {
               SetrichTextBox("逐秒计时" + i + "\r\n");
                Thread.Sleep(1000);
                myResetEvent.WaitOne();
            }
        }

        

        private bool PD = true; //false

        private void 判断能否执行()
        {
            while (PD)
            {
                myResetEvent.Set();
            }

        }

        private void PT()//创建后台线程执行 判断能否执行
        {
            ThreadStart entry = new ThreadStart(判断能否执行);
            Thread workThread = new Thread(entry);
            workThread.IsBackground = true;
            workThread.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            PD = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            PD = true;
            PT();
        }
    }
}

