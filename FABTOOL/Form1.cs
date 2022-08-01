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

        public class FabInfo {
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



        private void button1_Click(object sender, EventArgs e)
        {
            String name = textBox1.Text;
            String userId = textBox2.Text;
            String startStr = dateTimePicker1.Text;
            if (string.IsNullOrEmpty(name) ) {
                MessageBox.Show("name can't be empty !");
            }
            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("empId can't be empty !");
            }
            if (string.IsNullOrEmpty(startStr))
            {
                MessageBox.Show("startStr can't be empty !");
            }

            DateTime dt1 = Convert.ToDateTime(startStr);
            bool flag = ItemCheckedEventArgs(dt1);

            FabInfo fabInfo = new FabInfo();

            fabInfo.CoputerName = Dns.GetHostName();
            fabInfo.TaskTime = startStr;
            fabInfo.UserId = userId;



            List<String> dirvers = getDirvers();
            
            string[] exts = new string[] { };
            /*string[] exts = new string[] {".TXT" , ".DOC" , ".EXE" };*/

            List<FileTimeInfo> list1 = new List<FileTimeInfo>();

            
            foreach (String dir in dirvers) {
                List< FileTimeInfo> a =  GetLatestFileTimeInfo(dir, exts);
                list1.AddRange(a);
            };

            List<FileTimeInfo> list2 = new List<FileTimeInfo>();
            foreach (FileTimeInfo fileTimeInfo in list1)
            {
               
                // file last modify time > start time
                if (DateTime.Compare(dt1, fileTimeInfo.LastWriteTime) < 0)
                    list2.Add(fileTimeInfo);
            }
            fabInfo.Infos = list2;


            string jsondata = JsonConvert.SerializeObject(fabInfo);  //class类转string
            using (StreamWriter sw = new StreamWriter(@"FabTool.json"))  //将string 写入json文件
            {
                sw.WriteLine(jsondata);
            }

            using StreamWriter file = new(@"FabTool1.txt", false);
            file.WriteLine("name : " + fabInfo.UserId);
            file.WriteLine("computername : " + fabInfo.CoputerName);
            file.WriteLine("taskTime : " + fabInfo.TaskTime);
            foreach (FileTimeInfo line in fabInfo.Infos)
            {

                file.WriteLine(JsonConvert.SerializeObject(line));
            }


            MessageBox.Show("file write over");
        }


        private bool ItemCheckedEventArgs(DateTime startD ) {
            string eventID = "207";
            string LogSource = "Microsoft-Windows-StorageSpaces-Driver/Operational";
            /*string sQuery = "*[System/EventID=" + eventID + "]";*/
            

            string sQuery = string.Format("*[System/EventID=" + eventID + "] and *[System[TimeCreated[@SystemTime >= '{0}']]]",
            startD.ToUniversalTime().ToString("o"));

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
            return false;
        }



        private List<String> getDirvers() {
            List<String> list = new List<string>();
            var drivers = DriveInfo.GetDrives();
            foreach (var driver in drivers)
            {
                list.Add(driver.Name);
            }

            return list;

        }


       

        //获取最近创建的文件名和创建时间
        //如果没有指定类型的文件，返回null
        static List<FileTimeInfo> GetLatestFileTimeInfo(string dir, string[] exts)
        {
            List<FileTimeInfo> list = new List<FileTimeInfo>();
            DirectoryInfo root = new DirectoryInfo(dir);

            foreach (FileInfo fi in root.GetFiles())
            {
                if (exts.Length != 0)
                {
                    if (exts.Contains(fi.Extension.ToUpper()))
                    {
                        list.Add(new FileTimeInfo()
                        {
                            FileName = fi.FullName,
                            FileCreateTime = fi.CreationTime,
                            LastWriteTime = fi.LastWriteTime,
                            LastAccessTime = fi.LastAccessTime
                        });

                    }

                }
                else {
                    list.Add(new FileTimeInfo()
                    {
                        FileName = fi.FullName,
                        DirectoryName =fi.DirectoryName,
                        FileCreateTime = fi.CreationTime,
                        LastWriteTime = fi.LastWriteTime,
                        LastAccessTime = fi.LastAccessTime
                    });


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
