using System;
using System.IO;
using System.Runtime.Versioning;

namespace BoxAgr
{
    public static class Constants
    {
        public const string DatabaseFilename = "BoxUnit.db3";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                string basePath = "";
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) ?? "c:\\";
                }
                else
                {
                    basePath = "";
                    //basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    //basePath = "storage/emulated/0/Download";
                }

                return Path.Combine(basePath, DatabaseFilename);
            }
        }
        public static string TmpDir
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";
            }
        }



    }
    public static class TextConstants
    {
        public static string WorkType
        {
            get
            {
                return "сериализация";
            }
        }

        public static string WorkTypeUp
        {
            get
            {
                return "Cериализация";
            }
        }

        public static string WorkTypePadeg
        {
            get { return "сериализации"; }
        }
    }
}
