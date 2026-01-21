using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace BoxAgr.License
{
    public class DiskInfo
    {
        // Импорт функции из kernel32.dll
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string rootPathName,
            StringBuilder volumeNameBuffer,
            int volumeNameSize,
            out uint volumeSerialNumber,
            out uint maximumComponentLength,
            out FileSystemFlags fileSystemFlags,
            StringBuilder fileSystemNameBuffer,
            int fileSystemNameSize);

        [Flags]
        public enum FileSystemFlags : uint
        {
            CaseSensitiveSearch = 1,
            CasePreservedNames = 2,
            UnicodeOnDisk = 4,
            PersistentAcls = 8,
            FileCompression = 16,
            VolumeQuotas = 32,
            SupportsSparseFiles = 64,
            SupportsReparsePoints = 128,
            VolumeIsCompressed = 32768,
            SupportsObjectIDs = 65536,
            SupportsEncryption = 131072,
            NamedStreams = 262144,
            ReadOnlyVolume = 524288,
            SequentialWriteOnce = 1048576,
            SupportsTransactions = 2097152,
            SupportsHardLinks = 4194304,
            SupportsExtendedAttributes = 8388608,
            SupportsOpenByFileId = 16777216,
            SupportsUsnJournal = 33554432
        }

        public static uint GetVolumeSerialNumber(string drivePath)
        {
            uint serialNumber;
            uint maxComponentLength;
            FileSystemFlags flags;

            StringBuilder volumeName = new StringBuilder(256);
            StringBuilder fileSystemName = new StringBuilder(256);

            bool success = GetVolumeInformation(
                drivePath,
                volumeName,
                volumeName.Capacity,
                out serialNumber,
                out maxComponentLength,
                out flags,
                fileSystemName,
                fileSystemName.Capacity);

            if (!success)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return serialNumber;
        }

        public static string GetVolumeSerialNumberString(string drivePath)
        {
            uint serialNumber = GetVolumeSerialNumber(drivePath);
            return serialNumber.ToString("X8"); // Возвращает в HEX-формате (как в `vol` в cmd)
        }
    }

    internal static class License
    {
        //private static readonly string licFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
            //+ @"\License.key";

        public static bool IsLicenseValid(out string machineId)
        {
            try
            {
                string licFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
                string systemDrive = Path.GetPathRoot(Environment.SystemDirectory); 
                uint serialNumber = DiskInfo.GetVolumeSerialNumber(systemDrive);
                string serialHex = DiskInfo.GetVolumeSerialNumberString(systemDrive);
                machineId = serialHex;
                licFileName = licFileName + @"\" + serialHex + ".key";

                if (File.Exists(licFileName))
                {
                    using (TextReader reader = new StreamReader(licFileName))
                    {
                        string key = reader.ReadLine();
                        reader.Close();

                        if (Calc1cpassHash(machineId, "seed_zero_ten") == key)
                           return true;
                    }
                }
            }
            catch (Exception ex)
            {
                machineId = "UNKNOWN";
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            return false;
        }


        private static string Calc1cpassHash(string login, string pass)
        {
            byte[] data = Encoding.Default.GetBytes(pass.ToLower(CultureInfo.InvariantCulture));
            byte[] result;

            using (System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                // This is one implementation of the abstract class SHA1.
                result = sha.ComputeHash(data);
                string passSha1 = ByteArrayToString(result);

                string stdData = login.ToLower(CultureInfo.InvariantCulture) + passSha1;

                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(stdData);

                    byte[] md5Hash = md5.ComputeHash(inputBytes);
                    string resu = ByteArrayToString(md5Hash);

                    return resu;
                }
            }
        }
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            return hex.ToString();
        }
        private static string GetMachineId()
        {
            //// Пример: берем ID системного диска
            //var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name == Path.GetPathRoot(Environment.SystemDirectory));
            //if (drive != null)
            //{
            //    return drive.VolumeSerialNumber.ToString();
            //}

            // Или MAC-адрес
            var macAddr = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up)?
                .GetPhysicalAddress().ToString();

            return macAddr ?? "UNKNOWN";
        }
    }
}
