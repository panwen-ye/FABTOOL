using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;

namespace FABTOOL

{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dateTimePicker1.MaxDate = DateTime.Now;
        }

        public class TaskInfo
        {
            public string? CoputerName;
            public string? UserId;
            public string? TaskTime;

            public List<FileTimeInfo>? Infos;
        }

        //自定义一个类
        public class FileTimeInfo
        {
            public string? FileName;  //文件名3
            public string? DirectoryName;

            public DateTime FileCreateTime; //创建时间
            public DateTime LastWriteTime; //创建时间
            public DateTime LastAccessTime; //创建时间
        }




        private bool InputCheck(string name, string userId, string startStr)
        {

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Name can't be empty !");
                return false;
            }
            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("EmpId can't be empty !");
                return false;
            }
            userId = userId.Trim().ToUpper();
            if (!userId.StartsWith('E') && !userId.StartsWith('L'))
            {
                MessageBox.Show("EmpId invalid !");
                return false;
            }
            if (string.IsNullOrEmpty(startStr))
            {
                MessageBox.Show("StartDate can't be empty !");
                return false;
            }


            return true;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            String name = textBox1.Text;
            String userId = textBox2.Text;
            String startDateStr = dateTimePicker1.Text;
            bool flag = InputCheck(name, userId, startDateStr);
            if (!flag) {
                return;
            }
            DateTime startDate = Convert.ToDateTime(startDateStr);

            // get usbstror info
            string result = GetUsbStor();


            // get file modified
            List<string> list = GetFileModified(startDate);

            TaskInfo taskInfo = new()
            {
                CoputerName = Dns.GetHostName(),
                TaskTime = startDateStr,
                UserId = userId,

            };
            MessageBox.Show("file write over");
        }

        private string GetUsbStor()
        {
        

            List<UsbStorInfo> list = new();
            
            RegistryKey USBKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR", false);
            string param = "";
            foreach (string sub1 in USBKey.GetSubKeyNames())
            {
                RegistryKey sub1key = USBKey.OpenSubKey(sub1, false);
                foreach (string sub2 in sub1key.GetSubKeyNames())
                {
                    try
                    {

                        RegistryKey sub2key = sub1key.OpenSubKey(sub2, false);

                        if (sub2key.GetValue("Service", "").Equals("disk"))
                        {
                            UsbStorInfo usbStorInfo = new UsbStorInfo();
                            String Path = "USBSTOR" + "\\" + sub1 + "\\" + sub2;
                            String Name = (string)sub2key.GetValue("FriendlyName", "");
                            usbStorInfo.Name = Name;
                            usbStorInfo.Uid = sub2;
                            usbStorInfo.path = Path;

                            list.Add(usbStorInfo);
                            param += Path;
                            param += ",";
                        }
                    }
                    catch (Exception msg) //异常处理
                    {
                        MessageBox.Show(msg.Message);
                    }
                }

            }
            if (list.Count == 0) {
                return "";
            }


            return ExecuteExe(param);
        }


        private string ExecuteExe(string param) {

            string result = "";

            object output;
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = @"D:\python\USB\dist\USBUsedTime.exe";//可执行程序路径
                    p.StartInfo.Arguments = param;//参数以空格分隔，如果某个参数为空，可以传入""
                    p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
                    p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                    p.Start();
                    p.WaitForExit();
                    //正常运行结束放回代码为0
                    if (p.ExitCode != 0)
                    {
                        output = p.StandardError.ReadToEnd();
                        output = output.ToString().Replace(System.Environment.NewLine, string.Empty);
                        output = output.ToString().Replace("\n", string.Empty);
                        throw new Exception(output.ToString());
                    }
                    else
                    {
                        output = p.StandardOutput.ReadToEnd();
                        result = output.ToString();
                    }
                }
                Console.WriteLine(output);
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
            return result;

        }


        class UsbStorInfo {
            public string? Name;
            public string? Uid;
            public string? path;
        }

        private List<String> GetDrivers()
        {
            List<String> list = new ();
            var drivers = DriveInfo.GetDrives();
            foreach (var driver in drivers)
            {
                list.Add(driver.Name);
            }

            return list;

        }

        // get file list where modified between startDate and now
        private List<string> GetFileModified(DateTime startDate) {
            List<string> list = new();
            List<String> dirvers = GetDrivers();
            foreach (string driver in dirvers) {
                var directory = new DirectoryInfo(driver);                
                DateTime to_date = DateTime.Now;
                var files = directory.GetFiles()
                  .Where(file => file.LastWriteTime >= startDate && file.LastWriteTime <= to_date);
                foreach (var file in files) {
                    list.Add(file.Name);
                }
            }
            return list;
            
        }



        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void dateStart_ValueChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void dateTimeChoser1_Load(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {


        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
