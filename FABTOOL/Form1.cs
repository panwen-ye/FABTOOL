using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using static FABTOOL.RegisterTableUnitity;
using System.Threading;//���ÿռ�����
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

        //�Զ���һ����
        public class FileTimeInfo
        {
            public string? FileName;  //�ļ���3
            public string? DirectoryName;

            public DateTime FileCreateTime; //����ʱ��
            public DateTime LastWriteTime; //����ʱ��
            public DateTime LastAccessTime; //����ʱ��
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
            // ��������
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
            TimeSpan ts = endDate - startDate;	//����ʱ���
            int diff = ts.Days;
            if (diff <= 0)
            {
                MessageBox.Show("����ʱ��Ӧ�ô��ڿ�ʼʱ��");
                return;

            }
            if (diff > 100 || diff <= 0)
            {
                MessageBox.Show("ʱ������������10��");
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
                    // ���к�
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
            // �豸��Ϣ
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
            public string? FullPath;  //�ļ���3
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
                    p.StartInfo.FileName = @"D:\python\USB\dist\USBUsedTime.exe";//��ִ�г���·��
                    p.StartInfo.Arguments = param + " " + startDate1 + " " + endDate1;//�����Կո�ָ������ĳ������Ϊ�գ����Դ���""
                    p.StartInfo.UseShellExecute = false;//�Ƿ�ʹ�ò���ϵͳshell����
                    p.StartInfo.CreateNoWindow = true;//����ʾ���򴰿�
                    p.StartInfo.RedirectStandardOutput = true;//�ɵ��ó����ȡ�����Ϣ
                    p.StartInfo.RedirectStandardInput = true;   //�������Ե��ó����������Ϣ
                    p.StartInfo.RedirectStandardError = true;   //�ض����׼�������
                    p.Start();
                    p.WaitForExit();
                    //�������н����Żش���Ϊ0
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
        // �ݹ��ȡĳһĿ¼�µ�����Ŀ¼
        // </summary>
        // <param name="path">Ŀ¼·��</param>
        // <param name="isTiGui">�Ƿ�ݹ��ȡ</param>
        // <returns>����Ŀ¼</returns>            
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
                string[] tempdirs= Directory.GetDirectories(path);//�õ���Ŀ¼
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
                {  // ����û�ȡ���������������ݴ��� 
                    e.Cancel = true;
                    break;
                }
            }
        }

       

        private void button2_Click(object sender, EventArgs e)
        {
            this.backgroundWorker1.RunWorkerAsync(); // ���� backgroundWorker ���
            Form2 form = new Form2(this.backgroundWorker1);// ��ʾ����������
            form.ShowDialog(this);
            form.Close();
        }

        private delegate void SetPos(int ipos, string vinfo);//����

        private void button3_Click(object sender, EventArgs e)
        {
            Thread fThread = new Thread(new ThreadStart(SleepT));
            fThread.Start();
        }

        
        private delegate void delInfoList(string text);

        private void SetrichTextBox(string value)
        {

            if (richTextBox1.InvokeRequired)//�����̵߳���
            {
                delInfoList d = new delInfoList(SetrichTextBox);
                richTextBox1.Invoke(d, value);
            }
            else//���̵߳���
            {
                if (richTextBox1.Lines.Length > 100)
                {
                    richTextBox1.Clear();
                }

                richTextBox1.Focus(); //���ı����ȡ���� 
                richTextBox1.Select(richTextBox1.TextLength, 0);//���ù���λ�õ��ı�β
                richTextBox1.ScrollToCaret();//�������ؼ���괦 
                richTextBox1.AppendText(value);//�������
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
                SetrichTextBox("��ʱ :" + i + "\r\n");
                Thread.Sleep(100);
            }
      
    }

        private void button5_Click(object sender, EventArgs e)
        {
            on_off = true;
            SetrichTextBox("��ͣ�� :\r\n");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            on_off = false;
            ma.Set();
            SetrichTextBox("������ʱ :\r\n");
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

                string hdInfo = "";//Ӳ�����к�  

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
                

                Console.WriteLine("�����������ƣ�" + driveInfo.Name);
                Console.WriteLine("���������ͣ�" + driveInfo.DriveType);
                Console.WriteLine("���������ļ���ʽ��" + driveInfo.DriveFormat);
                Console.WriteLine("�������п��ÿռ��С��" + driveInfo.TotalFreeSpace);
                Console.WriteLine("�������ܴ�С��" + driveInfo.TotalSize);
            }
           
        }
        
    }
}
