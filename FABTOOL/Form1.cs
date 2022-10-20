using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using static FABTOOL.RegisterTableUnitity;
using System.Threading;//引用空间名称
using System.Management;

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
            if (endD > DateTime.Now) { 
                endD = DateTime.Now.AddMinutes(-10); ;
            }
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
                IList<EventProperty> eve = eventInstance.Properties;
                bool flag = false;
                foreach (EventProperty property in eve) {
                    if (checkIfOnUsing(property.Value.ToString())) {
                        flag = true;
                        break;
                    }

                }
                if (flag) {
                    continue;
                }
                //Access event properties here:
                //eventInstance.LogName;
                //eventInstance.ProviderName;                
                
                eventList.Add(eventInstance);
            }
            return eventList;
        }

        static AutoResetEvent myResetEvent = new AutoResetEvent(false);


        bool checkIfOnUsing(string serialNo) {
            Console.WriteLine("oncheck "  + serialNo);
            foreach (string str in _serialNumber) {
                if (serialNo.Contains(str)) { 
                return true;
                }
            }
            return false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MatchDriveLetterWithSerial();
            DoScanTask1();
        }

        public void DoScanTask1() {
            // 解析参数
            String name = textBox1.Text;
            String userId = textBox2.Text;
            String startDateStr = dateTimePicker1.Text;
            String endDateStr = dateTimePicker2.Text;
            bool flag = InputCheck(name, userId, startDateStr, endDateStr);

           /* if (!flag)
            {
                return;
            }*/
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

        private static List<string> _serialNumber = new List<string>();
        private static List<string> _serialNumber1 = new List<string>();

        private static void MatchDriveLetterWithSerial()
        {
            string[] diskArray;
            string driveNumber;
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDiskToPartition");
            foreach (ManagementObject dm in searcher.Get())
            {
                GetValueInQuotes(dm["Dependent"].ToString());
                diskArray = GetValueInQuotes(dm["Antecedent"].ToString()).Split(',');
                driveNumber = diskArray[0].Remove(0, 6).Trim();
                var disks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (ManagementObject disk in disks.Get())
                {                 
                    
                    if (disk["Name"].ToString() == ("\\\\.\\PHYSICALDRIVE" + driveNumber) & disk["InterfaceType"].ToString() == "USB")
                    {
                        _serialNumber.Add(ParseSerialFromDeviceID(disk["PNPDeviceID"].ToString()));
                    }
                    // 序列号
                    string serialNumber1111 = disk["SerialNumber"].ToString();
                    if (!_serialNumber.Contains(serialNumber1111)) {
                        _serialNumber.Add(serialNumber1111);
                    }
                    
                }
            }

            foreach (string id in _serialNumber)
            {
                Trace.WriteLine(id);
            }
        }

        private static string ParseSerialFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            int arrayLen = splitDeviceId.Length - 1;
            string[] serialArray = splitDeviceId[arrayLen].Split('&');
            string serial = serialArray[0];
            return serial;
        }

        private static string GetValueInQuotes(string inValue)
        {
            int posFoundStart = inValue.IndexOf("\"");
            int posFoundEnd = inValue.IndexOf("\"", posFoundStart + 1);
            string parsedValue = inValue.Substring(posFoundStart + 1, (posFoundEnd - posFoundStart) - 1);
            return parsedValue;
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
            List<UsbStorInfo> listRegit = GetUsbStor(startDate, endDate);
            if (listRegit != null && listRegit.Count != 0)
            {
                Flag = false;
            }
            List<EventRecord>  listEvent = ItemCheckedEventArgs(startDate, endDate);
            if (listEvent != null && listEvent.Count != 0)
            {
                Flag = false;
            }


            List<String> listFile = new List<String>();
            for (int j = 0; j < cnt; j++)
            {
                String driverPath = dirvers[j];
                // get file modified
                List<string> list1 = GetFileModified(driverPath ,startDate, endDate);
                if (list1 != null && list1.Count != 0)
                {
                    Flag = false;
                    listFile.AddRange(list1);
                }               
                           
            }
            RegeditInfoRecord(path, listRegit);
            EventInfoRecord(path, listEvent);
            FileModifyRecord(path , listFile);
            
        }

        public void FileModifyRecord(String path, List<string> list)
        {
            File.AppendAllText(path, "\r\n" + "# FileModifyInfo");
            if (list == null || list.Count == 0)
            {
                File.AppendAllText(path, "\r\n" + "PASS");
            }
            else {
                foreach (string s in list)
                {
                    
                    File.AppendAllText(path , s + "\r\n");
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
                        if (checkIfOnUsing(re.KeyPath))
                        {
                            continue;
                        }
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
        public static List<string> GetFileModified(string path , DateTime startDate, DateTime endDate)
        {
            List<string> strings = new List<string>();
            if (path.Contains("Window")) {
                return strings;
            }
            
            
            try
            {
                var directory = new DirectoryInfo(path);
                var files = directory.GetFiles()
              .Where(file => file.LastWriteTime >= startDate && file.LastWriteTime <= endDate);
                foreach (var file in files)
                {
                    string str = "Path: "+ file.FullName + " ; " + "LastWriteTime: " + file.LastWriteTime.ToString("G");
                    strings.Add(str);
                }
            }
            catch (Exception e)
            {

            }

                        
            try {
                string[] tempdirs= Directory.GetDirectories(path);//得到子目录
                foreach (string str in tempdirs) {
                    strings.AddRange(GetFileModified(str, startDate, endDate));
                }
                
            }
            catch (Exception e) { 
            
            }
            
            return strings;
        }
        // get file list where modified between startDate and now
     


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

        private void SleepT()
        {
            for (int i = 0; i < 500; i++)
            {
                System.Threading.Thread.Sleep(10);
               
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            thread = new Thread(Runtime);
            thread.Start();
        }

        Thread thread;
        ManualResetEvent ma;
        bool on_off = false;
        bool stop = false;
       
        void Runtime()
        {
            for (int i = 1; i <= 100; i++)
            {
                if (stop)
                    return;
                if (on_off)
                {
                    ma = new ManualResetEvent(false);
                    ma.WaitOne();
                }
                SetrichTextBox("计时 :" + i + "\r\n");
                Thread.Sleep(100);
            }
      
    }

        private void button5_Click(object sender, EventArgs e)
        {
            on_off = true;
            SetrichTextBox("暂停中 :\r\n");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            on_off = false;
            ma.Set();
            SetrichTextBox("继续计时 :\r\n");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            /*getDirverInfo();*/
            List<String> dirvers = GetDrivers();
            foreach (string str in dirvers)
            {
              string str1=  str.Replace("\\" , "" );
               MessageBox.Show(GetHardDiskID(str1)) ;
            }
        }

        public static string GetHardDiskID(string driver)

        {

            try

            {

                string hdInfo = "";//硬盘序列号  

                ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"F:\"");

                hdInfo = disk.Properties["VolumeSerialNumber"].Value.ToString();

                disk = null;

                return hdInfo.Trim();

            }

            catch (Exception e)

            {

                return "uHnIk";

            }

        }
        public string GetHd()
        {
            ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher();

            wmiSearcher.Query = new SelectQuery(
            "Win32_DiskDrive",
            "",
            new string[] { "PNPDeviceID" }
            );
            ManagementObjectCollection myCollection = wmiSearcher.Get();
            ManagementObjectCollection.ManagementObjectEnumerator em =
            myCollection.GetEnumerator();
            em.MoveNext();
            ManagementBaseObject mo = em.Current;
            string id = mo.Properties["PNPDeviceID"].Value.ToString().Trim();
            return id;
        }

       

        private void getDirverInfo() {
            List<String> dirvers = GetDrivers();
            foreach (string str in dirvers) {
                DriveInfo driveInfo = new DriveInfo(str);
                
                MessageBox.Show(driveInfo.Name);
                MessageBox.Show(driveInfo.DriveType.ToString());
                MessageBox.Show(driveInfo.DriveFormat);
                MessageBox.Show(driveInfo.TotalFreeSpace.ToString());
                MessageBox.Show(driveInfo.TotalSize.ToString());
                MessageBox.Show(driveInfo.VolumeLabel.ToString());
                

                Console.WriteLine("驱动器的名称：" + driveInfo.Name);
                Console.WriteLine("驱动器类型：" + driveInfo.DriveType);
                Console.WriteLine("驱动器的文件格式：" + driveInfo.DriveFormat);
                Console.WriteLine("驱动器中可用空间大小：" + driveInfo.TotalFreeSpace);
                Console.WriteLine("驱动器总大小：" + driveInfo.TotalSize);
            }
           
        }
        
    }
}
