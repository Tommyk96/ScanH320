using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace PharmaLegacy
{
    public delegate void DataRecievedEventHandler(object sender, string data);
    public delegate void DisconnectEventHandler(object sender, string data);

    public sealed class ScanerSession
    {
        private TcpClient client;
        private System.Diagnostics.Stopwatch diagCodeTimeFresh;
        private MessageUtilite msgUtil = new MessageUtilite();

        private bool ignoreError;
        private string clientIpAddress;
        private int timeout;// таймаут соединениия в милисекундах
       

        public event DataRecievedEventHandler DataRecieved;
        public event DisconnectEventHandler Disconect;

        public static byte[] codeVerify = new byte[2] { 0, 0xAA };
        public static byte[] noJob = new byte[2] { 0, 0xAB };
        public static byte[] codeBad = new byte[2] { 0, 0xAF };
        public static byte[] codeRepit = new byte[2] { 0, 0xAE };
        public static byte[] wrongGtin = new byte[2] { 0, 0xAD };

        private  long _pId;
        private readonly bool _packetLogEnable;
        public ScanerSession(TcpClient tc,int t, long pId, bool packetLogEnable)
        {
            client = tc;
            diagCodeTimeFresh = System.Diagnostics.Stopwatch.StartNew();
            ignoreError = false;
            timeout = t;
            _pId = pId;
            _packetLogEnable = packetLogEnable;
        }
        public System.ComponentModel.BackgroundWorker Run()
        {
            System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += delegate
            {
                //Program.workerArray.Add(worker);
                mainThread();
            };
            worker.ProgressChanged += delegate
            {
                try
                {
                    if (timeout == 0)
                        return;

                    if (diagCodeTimeFresh.ElapsedMilliseconds < timeout)
                        return;

                    ignoreError = true;
                    client.Close();
                    Log.Write("Сканер " + clientIpAddress + " не шлет диагностику. Соединение закрыто",EventLogEntryType.Information, 21);
                }
                catch
                {

                }

            };
            worker.RunWorkerCompleted += delegate
            {
                try
                {
                    ignoreError = true;
                    //Program.workerArray.Remove(worker);
                    client.Close();
                    Log.Write("Закрыто соединение со сканером " + clientIpAddress,EventLogEntryType.Information, 22);
                }
                catch
                {

                }
            };

            //
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerAsync();
            return worker;
        }
        public void mainThread()
        {
            int errorCounter = 0;
            clientIpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            Thread.CurrentThread.Name = $"TcpScanner{clientIpAddress}";

            try
            {
                Log.Write("Установлено соединение со сканером " + clientIpAddress,EventLogEntryType.Information, 23);
                byte[] rcvDataBuffer = new byte[50000];

                while (client.Connected)
                {
                    //если произошло более 10 ошибок подряд. закрыть соединение
                    if (errorCounter == 10)
                    {
                        client.Close();
                        throw new Exception("Соединение со сканером " + clientIpAddress + " потеряно");
                    }


                    Array.Clear(rcvDataBuffer, 0, rcvDataBuffer.Length);

                    int count = client.GetStream().Read(rcvDataBuffer, 0, rcvDataBuffer.Length);



                    if (count == 0)
                    {
                        errorCounter++;
                        continue;
                    }
                    errorCounter = 0;

                    if (rcvDataBuffer.Length == 0)
                        continue;

                    //получив пакет сохраняем его в лог
                    if (_pId >= 10000000)
                        _pId = 0;
                    else
                        _pId++;

                    //отключить покачто возможность логирования
                    if (_packetLogEnable)
                        PacketLog(_pId, rcvDataBuffer, count);

                    LabelParcePacket:
                    //если данные получены проверить стартовый и стоповый символы.
                    //string message = msgUtil.GetMessAtStart(rcvDataBuffer, count,2,3); Encoding.ASCII.GetString(rcvDataBuffer, 0, rcvDataBuffer.Length);
                    string message = msgUtil.GetMessAtStart(rcvDataBuffer, count, 0, 0x0d);

                    if (message.Length == 0)
                        continue;

                    //сбросить буфер после получения корректной посылки.
                    //тем самым мы можем получать только 1 код за такт. хз как но както инфа в нем накапливается.....
                    //msgUtil.ResetBufer();

                    //Log.Write(">1");
                    DataRecieved?.Invoke(this,message);
                    //очистить входной буфер и посмотреть есть ли еще пакет в ожидании обработки
                    Array.Clear(rcvDataBuffer);
                    count = 0;
                    goto LabelParcePacket;



                    //Log.Write("<1");

                }
                client.Close();
            }
            catch (Exception ex)
            {
                if (!ignoreError)
                    Log.Write(ex.Message,EventLogEntryType.Error, 25);
            }
            finally
            {
                //Program.ScanersState[scanNum] = false;
                //Program.systemState.SetScanerState(scanNum, false);
                Disconect?.Invoke(this, "");
            }
        }
        public static void PacketLog(long id, byte[] msgSource, int msgSourceSize)
        {
            DateTime d = DateTime.Now;

            try
            {
                byte[] header = Encoding.UTF8.GetBytes($"\nid:{id} src=");
                byte[] row = new byte[header.Length + msgSourceSize];
                Array.Copy(header, row, header.Length);
                Array.Copy(msgSource, 0, row, header.Length, msgSourceSize);
                Task.Factory.StartNew((_row) =>
                {
                    string errpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
                    try
                    {
                        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Logs";
                       
                    //DateTime time = (DateTime)_time;
                    //создать директорию если надо
                    if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        path += "\\Packet" + DateTime.Now.ToString("dd.MM.yyyy") + ".txt";

                        byte[] __row = (byte[])_row;
                        using (FileStream sw = new FileStream(path, FileMode.Append | FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            sw?.Write(__row, 0, __row.Length);
                            sw?.Flush();
                        }

                    //Console.WriteLine(s);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.IO.TextWriter logFile = new System.IO.StreamWriter(errpath + "\\errorLog.txt", true);
                        logFile.WriteLine("UnauthorizedAccessException " + ex.Message + "\n ");
                        logFile.Close();
                    }
                    catch (SecurityException ex)
                    {
                        System.IO.TextWriter logFile = new System.IO.StreamWriter(errpath + "\\errorLog.txt", true);
                        logFile.WriteLine("SecurityException " + ex.Message + "\n ");
                        logFile.Close();
                    }
                    catch (InvalidOperationException ex)
                    {
                        System.IO.TextWriter logFile = new System.IO.StreamWriter(errpath + "\\errorLog.txt", true);
                        logFile.WriteLine("InvalidOperationException " + ex.Message + "\n ");
                        logFile.Close();
                    }
                    catch (ArgumentException ex)
                    {
                        System.IO.TextWriter logFile = new System.IO.StreamWriter(errpath + "\\errorLog.txt", true);
                        logFile.WriteLine("ArgumentException " + ex.Message + "\n ");
                        logFile.Close();
                    }
                    catch (Exception ex)
                    {
                        System.IO.TextWriter logFile = new System.IO.StreamWriter(errpath + "\\errorLog.txt", true);
                        logFile.WriteLine("Exception " + ex.Message + "\n ");
                        logFile.Close();
                    }
                }, row);

            }
            catch { }


        }
    }
}
