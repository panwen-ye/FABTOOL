using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

class Program1
{
    const int HKEY_LOCAL_MACHINE = unchecked((int)0x80000002);
    const uint KEY_ENUMERATE_SUB_KEYS = 0x0008;
    const uint ERROR_NO_MORE_ITEMS = 259;
    

    public static void DoGetTime()
    {
        GetTime1(@"SYSTEM\CurrentControlSet\Enum\USBSTOR");
    }


    private static void GetTime1(String subKey)
    {
        SafeRegistryHandle hKey = null;
        int result = AdvApi32.RegOpenKeyEx(
            new IntPtr(HKEY_LOCAL_MACHINE),
            subKey,
            0,
            KEY_ENUMERATE_SUB_KEYS,
            out hKey
        );

        if (result != 0)
        {
            Console.WriteLine($"RegOpenKeyEx failed with error code {result}");
            return;
        }


        StringBuilder subKeyName = new StringBuilder(1024);
        uint cbMaxSubKeyName = (uint)subKeyName.Capacity;
      

        for (int i = 0; ; i++)
        {
            uint resultEnumKey = AdvApi32.RegEnumKeyEx(
                hKey,
                (uint)i,
                subKeyName,
                ref cbMaxSubKeyName,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                out long lastWriteTime
            );
            if (resultEnumKey == ERROR_NO_MORE_ITEMS)
            {
                break;
            }
            GetTime1(subKey + "\\" + subKeyName);

            Console.WriteLine($"{subKey + "\\" + subKeyName} last write time: {GetDateTime(lastWriteTime)}");
            cbMaxSubKeyName = (uint)subKeyName.Capacity;
        }

        hKey.Close();

    }



    private static DateTime GetDateTime(long lTime)
    {
        DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1601, 1, 1));
        // long lTime = ((long)timeStamp * 10000000);
        TimeSpan toNow = new TimeSpan(lTime);
        DateTime targetDt = dtStart.Add(toNow);
        return targetDt;
    }
}

public static class AdvApi32
{
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int RegOpenKeyEx(
        IntPtr hKey,
        string subKey,
        int ulOptions,
        uint samDesired,
        out SafeRegistryHandle hkResult
    );

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint RegEnumKeyEx(
        SafeRegistryHandle hKey,
        uint dwIndex,
        StringBuilder lpName,
        ref uint lpcName,
        IntPtr reserved,
        IntPtr lpClass,
        IntPtr lpcClass,
        out long lpftLastWriteTime
    );
}

[StructLayout(LayoutKind.Sequential)]
public struct FILETIME
{
    public uint dwLowDateTime;
    public uint dwHighDateTime;
}
