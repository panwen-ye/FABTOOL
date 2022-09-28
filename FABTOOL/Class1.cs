using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace FABTOOL
{
    public class RegisterTableUnitity
    {
        #region 32位程序读写64注册表

        static UIntPtr HKEY_CLASSES_ROOT = (UIntPtr)0x80000000;
        static UIntPtr HKEY_CURRENT_USER = (UIntPtr)0x80000001;
        static UIntPtr HKEY_LOCAL_MACHINE = (UIntPtr)0x80000002;
        static UIntPtr HKEY_USERS = (UIntPtr)0x80000003;
        static UIntPtr HKEY_CURRENT_CONFIG = (UIntPtr)0x80000005;

        // 关闭64位（文件系统）的操作转向
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);
        // 开启64位（文件系统）的操作转向
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        // 获取操作Key值句柄
        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint RegOpenKeyEx(UIntPtr hKey, string lpSubKey, uint ulOptions, int samDesired, out UIntPtr phkResult);
        //关闭注册表转向（禁用特定项的注册表反射）
        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern long RegDisableReflectionKey(UIntPtr hKey);
        //使能注册表转向（开启特定项的注册表反射）
        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern long RegEnableReflectionKey(UIntPtr hKey);
        //获取Key值（即：Key值句柄所标志的Key对象的值）
        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegQueryValueEx(UIntPtr hKey, string lpValueName, int lpReserved,
                                                  out uint lpType, System.Text.StringBuilder lpData,
                                                   ref uint lpcbData);
        [DllImport("advapi32.dll", EntryPoint = "RegEnumKeyEx")]
        extern private static int RegEnumKeyEx(
           UIntPtr hkey,
           uint index,
           StringBuilder lpName,
           ref uint lpcbName,
           IntPtr reserved,
           IntPtr lpClass,
           IntPtr lpcbClass,
           out long lpftLastWriteTime);

        [DllImport("advapi32.dll", SetLastError = false)]
        static extern int RegCreateKeyEx(
            UIntPtr hKey,
            string lpSubKey,
            IntPtr Reserved,
            string lpClass,
            RegOption dwOptions,
            RegSAM samDesired,
            ref IntPtr lpSecurityAttributes,
            out UIntPtr phkResult,
            out RegResult lpdwDisposition);

        [Flags]
        public enum RegOption
        {
            NonVolatile = 0x0,
            Volatile = 0x1,
            CreateLink = 0x2,
            BackupRestore = 0x4,
            OpenLink = 0x8
        }

        [Flags]
        public enum RegSAM
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            WOW64_32Key = 0x0200,
            WOW64_64Key = 0x0100,
            WOW64_Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        public enum RegResult
        {
            CreatedNewKey = 0x00000001,
            OpenedExistingKey = 0x00000002
        }

        private static UIntPtr TransferKeyName(string keyName)
        {
            switch (keyName)
            {
                case "HKEY_CLASSES_ROOT":
                    return HKEY_CLASSES_ROOT;
                case "HKEY_CURRENT_USER":
                    return HKEY_CURRENT_USER;
                case "HKEY_LOCAL_MACHINE":
                    return HKEY_LOCAL_MACHINE;
                case "HKEY_USERS":
                    return HKEY_USERS;
                case "HKEY_CURRENT_CONFIG":
                    return HKEY_CURRENT_CONFIG;
            }

            return HKEY_CLASSES_ROOT;
        }

        private static DateTime GetDateTime(long lTime)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1601, 1, 1));
            // long lTime = ((long)timeStamp * 10000000);
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime targetDt = dtStart.Add(toNow);
            return targetDt;
        }

        public static List<string> Get64BitRegistryKey(string parentKeyName, string subKeyName, string keyName)
        {
            List<string> programKeyValueList = new List<string>();
            int KEY_QUERY_VALUE = (0x0001);
            int KEY_WOW64_64KEY = (0x0100);
            int KEY_ALL_WOW64 = (KEY_QUERY_VALUE | KEY_WOW64_64KEY);

            try
            {
                //将Windows注册表主键名转化成为不带正负号的整形句柄（与平台是32或者64位有关）
                UIntPtr hKey = TransferKeyName(parentKeyName);
                //记录读取到的Key值
                StringBuilder result = new StringBuilder("".PadLeft(1024));
                uint resultSize = 1024;
                uint lpType = 0;
                //关闭文件系统转向 
                IntPtr oldWOW64State = new IntPtr();
                //if (Wow64DisableWow64FsRedirection(ref oldWOW64State))
                {
                    RegResult regResult;
                    IntPtr lpSecurityAttributes = IntPtr.Zero;
                    //获得操作Key值的句柄
                    UIntPtr hKeyEx = UIntPtr.Zero;
                    int r = RegCreateKeyEx(hKey, subKeyName, IntPtr.Zero, null, RegOption.NonVolatile, RegSAM.EnumerateSubKeys | RegSAM.WOW64_64Key, ref lpSecurityAttributes, out hKeyEx, out regResult);
                    
                    StringBuilder subsubKeyName = new StringBuilder();
                    uint i = 0;
                    long lastWriteTime;
                    while (true)
                    {
                        uint cbMaxSubKey = 100; //键名的长度
                        int reke = RegEnumKeyEx(hKeyEx, i, subsubKeyName, ref cbMaxSubKey, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWriteTime);
                        Console.WriteLine(subsubKeyName);
                        DateTime dte = GetDateTime(lastWriteTime);
                        Console.WriteLine(dte);
                        if (reke == 0)
                        {
                            //声明将要获取Key值的句柄
                            UIntPtr pHKey = UIntPtr.Zero;
                            result = new StringBuilder("".PadLeft(1024));
                            //获得操作Key值的句柄
                            RegOpenKeyEx(hKey, subKeyName + "\\" + subsubKeyName, 0, (int)(RegSAM.QueryValue | RegSAM.WOW64_64Key), out pHKey);
                            //关闭注册表转向（禁止特定项的注册表反射）
                            //RegDisableReflectionKey(pHKey);

                            //获取访问的Key值
                            RegQueryValueEx(pHKey, keyName, 0, out lpType, result, ref resultSize);

                            //打开注册表转向（开启特定项的注册表反射）
                            //RegEnableReflectionKey(pHKey);

                            i++;
                            if (!string.IsNullOrEmpty(result.ToString().Trim()))
                            {
                                programKeyValueList.Add(result.ToString().Trim());
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //打开文件系统转向
                //Wow64RevertWow64FsRedirection(oldWOW64State);

                //返回Key值
                return programKeyValueList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static List<string> Get32BitRegistryKey(string parentKeyName, string subKeyName, string keyName)
        {
            List<string> programKeyValueList = new List<string>();
            int KEY_QUERY_VALUE = (0x0001);
            int KEY_WOW64_64KEY = (0x0100);
            int KEY_ALL_WOW64 = (KEY_QUERY_VALUE | KEY_WOW64_64KEY);

            try
            {
                //将Windows注册表主键名转化成为不带正负号的整形句柄（与平台是32或者64位有关）
                UIntPtr hKey = TransferKeyName(parentKeyName);
                //记录读取到的Key值
                StringBuilder result = new StringBuilder("".PadLeft(1024));
                uint resultSize = 1024;
                uint lpType = 0;
                //关闭文件系统转向 
                IntPtr oldWOW64State = new IntPtr();
                //if (Wow64DisableWow64FsRedirection(ref oldWOW64State))
                {
                    RegResult regResult;
                    IntPtr lpSecurityAttributes = IntPtr.Zero;
                    //获得操作Key值的句柄
                    UIntPtr hKeyEx = UIntPtr.Zero;
                    int r = RegCreateKeyEx(hKey, subKeyName, IntPtr.Zero, null, RegOption.NonVolatile, RegSAM.EnumerateSubKeys | RegSAM.WOW64_32Key, ref lpSecurityAttributes, out hKeyEx, out regResult);
                    StringBuilder subsubKeyName = new StringBuilder();
                    uint i = 0;
                    long lastWriteTime;
                    while (true)
                    {
                        uint cbMaxSubKey = 100; //键名的长度
                        int reke = RegEnumKeyEx(hKeyEx, i, subsubKeyName, ref cbMaxSubKey, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWriteTime);
                        if (reke == 0)
                        {
                            //声明将要获取Key值的句柄
                            UIntPtr pHKey = UIntPtr.Zero;
                            //获取键值结果
                            result = new StringBuilder("".PadLeft(1024));
                            // 获得操作Key值的句柄
                            RegOpenKeyEx(hKey, subKeyName + "\\" + subsubKeyName, 0, (int)(RegSAM.QueryValue | RegSAM.WOW64_32Key), out pHKey);
                            //关闭注册表转向（禁止特定项的注册表反射）
                            //RegDisableReflectionKey(pHKey);

                            // 获取访问的Key值
                            RegQueryValueEx(pHKey, keyName, 0, out lpType, result, ref resultSize);

                            //打开注册表转向（开启特定项的注册表反射）
                            //RegEnableReflectionKey(pHKey);

                            i++;

                            if (!string.IsNullOrEmpty(result.ToString().Trim()))
                            {
                                programKeyValueList.Add(result.ToString().Trim());
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //打开文件系统转向
                //Wow64RevertWow64FsRedirection(oldWOW64State);

                //返回Key值
                return programKeyValueList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取所有软件注册信息
        /// </summary>
        /// <returns></returns>
        public List<string> ProgramInstalledList()
        {
            try
            {
                List<string> displayNameList = new List<string>();
                displayNameList = RegisterTableUnitity.Get64BitRegistryKey("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Enum\USBSTOR", "DisplayName");//uninstallNode.GetValue("DisplayName");
                displayNameList.AddRange(RegisterTableUnitity.Get32BitRegistryKey("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "DisplayName"));//uninstallNode.GetValue("DisplayName"););
                return displayNameList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
    }
}