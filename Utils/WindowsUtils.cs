namespace Utils
{
    using System.IO;
    using System.Linq;
    using System.Management;

    public static class WindowsUtils
    {
        public static string GetFirstUserName()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
            ManagementObjectCollection collection = searcher.Get();
            return Path.GetFileName((string)collection.Cast<ManagementBaseObject>().First()["UserName"]);
        }
    }
}