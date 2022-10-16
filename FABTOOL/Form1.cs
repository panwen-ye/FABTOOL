using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using static FABTOOL.RegisterTableUnitity;
using System.Threading;//引用空间名称

namespace FABTOOL

{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dateTimePicker1.MaxDate = DateTime.Now;
        }

        public class TaskInfo
        {
            public string? CoputerName;
            public string? Operator;
            public string? Host;
            public string? TaskTime;
            public string? StartTime;
            public string? EndTime;
            public string? Result;

            
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




        private bool InputCheck(string name, string userId, string startStr , string endStr)
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
            if (string.IsNullOrEmpty(endStr))
            {
                MessageBox.Show("EndDate can't be empty !");
                return false;
            }

            
            return true;

        }

        private List<EventRecord> ItemCheckedEventArgs(DateTime startD , DateTime endD)
        {
            string eventID = "207";
            string LogSource = "Microsoft-Windows-StorageSpaces-Driver/Operational";
            /*string sQuery = "*[System/EventID=" + eventID + "]";*/
            string sQuery = string.Format("*[System/EventID=" + eventID + "]" +
                " and *[System[TimeCreated[@SystemTime >= '{0}'and @SystemTime <= '{1}']]] ",
            startD.ToUniversalTime().ToString("o"),
            endD.ToUniversalTime().ToString("o")
            );

            var elQuery = new EventLogQuery(LogSource, PathType.LogName, sQuery);
            var elReader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery);

            List<EventRecord> eventList = new List<EventRecord>();
            for (EventRecord eventInstance = elReader.ReadEvent();
                null != eventInstance; eventInstance = elReader.ReadEvent())
            {
                //Access event properties here:
                //eventInstance.LogName;
                //eventInstance.ProviderName;
                eventList.Add(eventInstance);
            }
            return eventList;
        }
         

        private void button1_Click(object sender, EventArgs e)
        {
            Thread fThread = new Thread(new ThreadStart(DoScanTask1));
            fThread.Start();

        }

        public void DoScanTask1() {
            // 解析参数
            String name = textBox1.Text;
            String userId = textBox2.Text;
            String startDateStr = dateTimePicker1.Text;
            String endDateStr = dateTimePicker2.Text;
            bool flag = InputCheck(name, userId, startDateStr, endDateStr);

            if (!flag)
            {
                return;
            }
            DateTime startDate = Convert.ToDateTime(startDateStr);
            DateTime endDateT = Convert.ToDateTime(endDateStr);
            DateTime endDate = new DateTime(endDateT.Year, endDateT.Month, endDateT.Day, 23, 59, 59);
            TimeSpan ts = endDate - startDate;	//计算时间差
            int diff = ts.Days;
            if (diff <= 0)
            {
                MessageBox.Show("结束时间应该大于开始时间");
                return;

            }
            if (diff > 100 || diff <= 0)
            {
                MessageBox.Show("时间间隔不允许超过10天");
                return;

            }
            TaskInfo taskInfo = new()
            {
                CoputerName = Dns.GetHostName(),
                TaskTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                StartTime = startDateStr,
                EndTime = endDateStr,
                Host = userId,
                Operator = name,


            };
           
            DoScanTask(startDate , endDate , taskInfo);
            MessageBox.Show("Task Over");

        }



        public void DoScanTask(DateTime startDate , DateTime endDate , TaskInfo taskInfo) {
            bool Flag = true;
            string directory = taskInfo.CoputerName + taskInfo.TaskTime;
            DirectoryInfo di = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory + "/" + directory);
            di.Create();
            // 设备信息
            String path = directory + "/result.txt";
            List<String> dirvers = GetDrivers();
            int cnt = dirvers.Count;
            int n = 80 / cnt;
            List<FileInfo> listFile = new List<FileInfo>();
            List<UsbStorInfo> result1 = new List<UsbStorInfo>();
            List<EventRecord> list = new List<EventRecord>();

            for (int i = 0; i < 100;) {
                System.Threading.Thread.Sleep(10);
                if (i < 1) {
                    // get usbstror info from regedit
                    result1 = GetUsbStor(startDate, endDate);
                    if (result1 != null && result1.Count != 0)
                    {
                        Flag = false;
                    }
                    i = 5;

                } else
                if (i == 5) {
                    // get usb record from event log
                    list = ItemCheckedEventArgs(startDate, endDate);
                    if (list != null && list.Count != 0)
                    {
                        Flag = false;
                    }
                    i = 10;
                    
                } else if (i>=10 && i<90) {
                    for (int j = 0; j < cnt; j++)
                    {
                        String driverPath = dirvers[j];
                        // get file modified
                        List<FileInfo> list1 = GetFileModified(startDate, endDate, driverPath);
                        if (list1 != null && list1.Count != 0)
                        {
                            Flag = false;
                            listFile.AddRange(list1);
                        }
                        if (j == cnt - 1)
                        {
                            i = 90;
                        }
                        else
                        {
                            i = 10 + j * n;
                        }
                        System.Threading.Thread.Sleep(1000);
                        SetTextMesssage(i, i.ToString() + "\r\n");

                    }
                }
                

                if (i >= 90 && i<92 ) {
                    string result = "Pass";
                    if (!Flag)
                    {
                        result = "Fail";
                    }
                    textBox3.Text = result;
                    taskInfo.Result = result;
                    
                    
                    TaskInfoRecord(path, taskInfo);
                    i = 92;
                }else

                if (i >= 92 && i < 94) {
                    // 注册表信息
                    RegeditInfoRecord(path, result1);
                    i = 94;
                }else

                if (i >= 94 && i < 96) {
                    // 日志时间信息
                    EventInfoRecord(path, list);
                    i = 96;
                }else

                if (i >= 96) {
                    // 文件修改
                    FileModifyRecord(path, listFile);
                    i = 100;
                }
                SetTextMesssage(i, i.ToString() + "\r\n");


            }
            

            

           

           
           
            
        }

        public void FileModifyRecord(String path, List<FileInfo> list)
        {
            File.AppendAllText(path, "\r\n" + "# FileModifyInfo");
            if (list == null || list.Count == 0)
            {
                File.AppendAllText(path, "\r\n" + "PASS");
            }
            else {
                foreach (FileInfo s in list)
                {
                    FileInfoT fileInfoT = new FileInfoT()
                    {
                        FullPath = s.FullName,
                        LastWirteTime = s.LastWriteTime.ToString("G"),
                    };
                    string info = JsonConvert.SerializeObject(fileInfoT);
                    File.AppendAllText(path, "\r\n" + info);
                }
            }
            
        }

        public void EventInfoRecord(String path, List<EventRecord> list)
        {
            File.AppendAllText(path, "\r\n" + "# EventInfo");
            if (list == null || list.Count == 0)
            {
                File.AppendAllText(path, "\r\n" + "PASS");
            }
            else
            {
                foreach (EventRecord eventInfo in list)
                {
                    string info = JsonConvert.SerializeObject(eventInfo);
                    File.AppendAllText(path, "\r\n" + info);
                }
            }
            
        }

        

      public void RegeditInfoRecord(String path, List<UsbStorInfo> list) {
            File.AppendAllText(path, "\r\n" + "# RegeditInfo");
            if (list == null || list.Count == 0)
            {
                File.AppendAllText(path, "\r\n" + "PASS");
            }
            else {
                foreach (UsbStorInfo usbStorInfo in list)
                {
                    string info = JsonConvert.SerializeObject(usbStorInfo);
                    File.AppendAllText(path, "\r\n" + info);
                }
            }
            

        }

        public void TaskInfoRecord(String path , TaskInfo taskInfo) {
            
            File.AppendAllText(path, "\r\n" + "# TaskInfo");
            File.AppendAllText(path, "\r\n" + "CoputerName" + taskInfo.CoputerName);
            File.AppendAllText(path, "\r\n" + "TaskTime" + taskInfo.TaskTime);
            File.AppendAllText(path, "\r\n" + "StartTime" + taskInfo.StartTime);
            File.AppendAllText(path, "\r\n" + "EndTime" + taskInfo.EndTime);
            File.AppendAllText(path, "\r\n" + "Host" + taskInfo.Host);
            File.AppendAllText(path, "\r\n" + "Operator" + taskInfo.Operator);
            File.AppendAllText(path, "\r\n" + "Result" + taskInfo.Result);
            
        }

        class FileInfoT {
            public string? FullPath;  //文件名3
            public string? LastWirteTime;
        }

        private List<UsbStorInfo> GetUsbStor(DateTime startDate , DateTime endDate)
        {
            List<UsbStorInfo> list = new();
            String parentPath = @"SYSTEM\CurrentControlSet\Enum\USBSTOR";
            RegistryKey USBKey = Registry.LocalMachine.OpenSubKey(parentPath, false);
            if (USBKey != null) {

                foreach (string sub1 in USBKey.GetSubKeyNames())
                {
                    List<RegistryKeyInfo> list1 = RegisterTableUnitity.GetRegistryKeyLastWritetime("HKEY_LOCAL_MACHINE", parentPath + @"\" + sub1);
                    foreach (RegistryKeyInfo re in list1) {
                        DateTime lastWriteTime = re.LastWriteTime;
                        if (lastWriteTime >= startDate && lastWriteTime <= endDate) {
                            UsbStorInfo usbStorinfo = new UsbStorInfo();
                            usbStorinfo.path = re.KeyPath;
                            usbStorinfo.Uid = sub1;
                            usbStorinfo.LastWriteTime = re.LastWriteTime;
                            list.Add(usbStorinfo);
                        }
                    }
                    
                }
            }
            return list;
        }

        public static long GetTimeStamp(DateTime date)

        {
            DateTime DateTime1970 = new DateTime(1970, 1, 1).ToLocalTime();
            return (long)(date.ToLocalTime() - DateTime1970).TotalSeconds;
        }

        private string ExecuteExe(string param , DateTime startDate, DateTime endDate) {
            
            string result = "";
            long startDate1 = GetTimeStamp(startDate);
            long endDate1 = GetTimeStamp(endDate);
            object output;
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = @"D:\python\USB\dist\USBUsedTime.exe";//可执行程序路径
                    p.StartInfo.Arguments = param + " " + startDate1 + " " + endDate1;//参数以空格分隔，如果某个参数为空，可以传入""
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


        public class UsbStorInfo {
            public String Name;
            public String Uid;
            public String path;

            public DateTime LastWriteTime;
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

        // <summary>
        // 递归获取某一目录下的所有目录
        // </summary>
        // <param name="path">目录路径</param>
        // <param name="isTiGui">是否递归获取</param>
        // <returns>所有目录</returns>            
        public static List<string> GetAllDirectoriesFromPath(string path, bool isTiGui = true )
        {
            
            List<string> dirs = new List<string>();
            dirs.Add(path);
            try {
                string[] tempdirs = Directory.GetDirectories(path);//得到子目录
                if (isTiGui)
                    foreach (var str in tempdirs)
                        dirs.AddRange(GetAllDirectoriesFromPath(str, isTiGui));
                dirs.AddRange(tempdirs);
            }
            catch (Exception e) { 
            
            }
            
            return dirs;
        }
        // get file list where modified between startDate and now
        private List<FileInfo> GetFileModified(DateTime startDate ,DateTime endDate,String dirvierPath) {
            List<FileInfo> list = new();
            List<string> list2 = GetAllDirectoriesFromPath(dirvierPath);
            foreach (string dir1 in list2)
            {
                if (dir1.Contains("Admin") || dir1.Contains("Windows") || dir1.Contains("Microsoft"))
                {
                    continue;
                }
                var directory = new DirectoryInfo(dir1);
                try
                {
                    var files = directory.GetFiles()
                  .Where(file => file.LastWriteTime >= startDate && file.LastWriteTime <= endDate);
                    foreach (var file in files)
                    {
                        list.Add(file);
                    }
                }
                catch (Exception e)
                {

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

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged_1(object sender, EventArgs e)
        {
            

        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(100);
                worker.ReportProgress(i);
                if (worker.CancellationPending)
                {  // 如果用户取消则跳出处理数据代码 
                    e.Cancel = true;
                    break;
                }
            }
        }

       

        private void button2_Click(object sender, EventArgs e)
        {
            this.backgroundWorker1.RunWorkerAsync(); // 运行 backgroundWorker 组件
            Form2 form = new Form2(this.backgroundWorker1);// 显示进度条窗体
            form.ShowDialog(this);
            form.Close();
        }

        private delegate void SetPos(int ipos, string vinfo);//代理

        private void button3_Click(object sender, EventArgs e)
        {
            Thread fThread = new Thread(new ThreadStart(SleepT));
            fThread.Start();
        }

        private void SetTextMesssage(int ipos, string vinfo)
        {
            if (this.InvokeRequired)
            {
                SetPos setpos = new SetPos(SetTextMesssage);
                this.Invoke(setpos, new object[] { ipos, vinfo });
            }
            else
            {
                this.label4.Text = ipos.ToString() + "/1000";
                this.progressBar1.Value = Convert.ToInt32(ipos);
                this.textBox4.AppendText(vinfo);
            }
        }

        private void SleepT()
        {
            for (int i = 0; i < 500; i++)
            {
                System.Threading.Thread.Sleep(10);
                SetTextMesssage(100 * i / 500, i.ToString() + "\r\n");
            }
        }
    }
}
