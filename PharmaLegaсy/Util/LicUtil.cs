using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Management;

namespace Util
{
    public class LicUtil
    {
        static public string GetDriveSerialNumber(string drive)
        {
            string driveFixed = System.IO.Path.GetPathRoot(drive);
            driveFixed = driveFixed.Replace("\\","");
            try
            {
                using (ManagementObjectSearcher querySearch = new ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk Where Name = '" + driveFixed + "'"))
                {

                    using (ManagementObjectCollection queryCollection = querySearch.Get())
                    {

                        foreach (ManagementObject moItem in queryCollection)
                        {
                            return moItem.GetPropertyValue("VolumeSerialNumber").ToString();
                        }
                    }
                }
            }
            catch (Exception e) { e.ToString(); }
            return "";
        }
    }
}
