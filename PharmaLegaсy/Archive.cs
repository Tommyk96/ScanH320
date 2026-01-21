using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using Util;

namespace Utilite
{
    public static  class Archive
    {
        public static async Task<bool> ArchiveDir(string source, string destanationDir,string fileName, string fileMask = "")
        {
           return await Task.Run( () =>
            {
                if (string.IsNullOrEmpty(source))
                    return false;

                try
                {
                    //запаковать каталог с логами
                    DirectoryInfo directorySelected = new DirectoryInfo(source);

                    using (var ms = new MemoryStream())
                    {
                        using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                        {
                            foreach (FileInfo fileToCompress in directorySelected.GetFiles())
                            {
                                //".txt"
                                if ((File.GetAttributes(fileToCompress.FullName) &
                               FileAttributes.Hidden) != FileAttributes.Hidden & (fileToCompress.Extension == fileMask || string.IsNullOrEmpty(fileMask)))
                                {
                                    var entry = zipArchive.CreateEntry(fileToCompress.Name, CompressionLevel.Fastest);
                                    using (var entryStream = entry.Open())
                                    {
                                        fileToCompress.OpenRead()?.CopyTo(entryStream);
                                    }
                                }
                            }
                        }

                        ms.Position = 0;
                        if (!Directory.Exists(destanationDir))
                        {
                            Directory.CreateDirectory(destanationDir);
                        }

                        using (FileStream stream = File.Create(destanationDir + "\\" + fileName + ".ogz"))
                        {
                           ms.CopyTo(stream);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("ZAR", ex.Message, EventLogEntryType.Error, 1021);
                }

                return false;

            });
        }
    }
}
