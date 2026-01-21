using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Util
{
    public class Log
    {
        //private static int lastEventId;
        private static ReaderWriterLockSlim logWr = new ReaderWriterLockSlim();
        private static readonly string _logDir; 
        static Log()
        {
            _logDir = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName)}\\Logs";
        }

        public static void ClearLogs(int month)
        {
            Task.Factory.StartNew(() =>
            {
                string errpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
                //Stopwatch swt = new Stopwatch();
                // swt.Start();
                try
                {
                    string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Logs";
                    DateTime horizont = DateTime.Today.AddMonths(-month);


                    string[] fileEntries = Directory.GetFiles(path);
                    foreach (string fileName in fileEntries)
                    {
                        DateTime lw = File.GetLastWriteTime(fileName);
                        if (lw < horizont)
                            File.Delete(fileName);
                    }
                }
                catch (Exception ) { }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(string id, string data, EventLogEntryType errType = EventLogEntryType.Information, int eventId = 0, [CallerMemberName] string fname = "")
        {
            string s = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}|{5}\n",
                DateTime.Now.ToString("HH:mm:ss.fff"),Thread.CurrentThread.ManagedThreadId, id, fname,  eventId, data);
            Write(s);
        }
        public static Task Write(string data, EventLogEntryType errType = EventLogEntryType.Information, int eventId = 0, [CallerMemberName] string fname = "")
        {
            string s = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}|{5}\n",
               DateTime.Now.ToString("HH:mm:ss.fff"), Thread.CurrentThread.ManagedThreadId, "xxx", fname, eventId, data);
           return Write(s);
        }
        private static Task Write(string data)
        {
            if (logWr.TryEnterWriteLock(200))
            {
                try
                {

                    //создать директорию если надо
                    if (!Directory.Exists(_logDir))
                        Directory.CreateDirectory(_logDir);

                    string filePath = _logDir + "\\sLog" + DateTime.Now.ToString("dd.MM.yyyy") + ".txt";
                    Console.WriteLine(data);
                
                    return File.AppendAllTextAsync(filePath, data);
                }
                catch (UnauthorizedAccessException ex)
                {
                    TextWriter logFile = new System.IO.StreamWriter(_logDir + "\\errorLog.txt", true);
                    logFile.WriteLine("UnauthorizedAccessException " + ex.Message + "\n ");
                    logFile.Close();
                }
                catch (SecurityException ex)
                {
                    System.IO.TextWriter logFile = new System.IO.StreamWriter(_logDir + "\\errorLog.txt", true);
                    logFile.WriteLine("SecurityException " + ex.Message + "\n ");
                    logFile.Close();
                }
                catch (InvalidOperationException ex)
                {
                    System.IO.TextWriter logFile = new System.IO.StreamWriter(_logDir + "\\errorLog.txt", true);
                    logFile.WriteLine("InvalidOperationException " + ex.Message + "\n ");
                    logFile.Close();
                }
                catch (ArgumentException ex)
                {
                    System.IO.TextWriter logFile = new System.IO.StreamWriter(_logDir + "\\errorLog.txt", true);
                    logFile.WriteLine("ArgumentException " + ex.Message + "\n ");
                    logFile.Close();
                }
                catch (Exception ex)
                {
                    System.IO.TextWriter logFile = new System.IO.StreamWriter(_logDir + "\\errorLog.txt", true);
                    logFile.WriteLine("Exception " + ex.Message + "\n ");
                    logFile.Close();
                }
                finally
                {
                    logWr.ExitWriteLock();
                }
            }
            else
            {
                try {
                    Console.WriteLine("ОСНОВНОЙ ЛОГ ЗАБЛОКИРОВАН!"+data);
                    return File.AppendAllTextAsync(_logDir + "\\blockLog.txt", $"{DateTime.Now.ToString("HH:mm:ss.fff")
                        };{data}"); } catch { }
            }

            return Task.CompletedTask;
        }
        public static void WriteConsole(string s) { Console.Write(s); }
    }

    public enum EventLogEntryType
    {
        //
        // Сводка:
        //     An error event. This indicates a significant problem the user should know about;
        //     usually a loss of functionality or data.
        Error = 1,
        //
        // Сводка:
        //     A warning event. This indicates a problem that is not immediately significant,
        //     but that may signify conditions that could cause future problems.
        Warning = 2,
        //
        // Сводка:
        //     An information event. This indicates a significant, successful operation.
        Information = 4,
        //
        // Сводка:
        //     A success audit event. This indicates a security event that occurs when an audited
        //     access attempt is successful; for example, logging on successfully.
        SuccessAudit = 8,
        //
        // Сводка:
        //     A failure audit event. This indicates a security event that occurs when an audited
        //     access attempt fails; for example, a failed attempt to open a file.
        FailureAudit = 16
    }
}