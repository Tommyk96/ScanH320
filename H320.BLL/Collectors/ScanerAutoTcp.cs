using BoxAgr.BLL.Events;
using Peripherals;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace BoxAgr.BLL.Collectors
{
    public sealed class ScanerAutoTcp
    {
        private readonly TcpClient Client;
        private readonly MessageUtilite MsgUtil = new(10240);

        private bool IgnoreCloseDisconnectError;
        public string ClientIpAddress { get; set; } = "";
        public int Id { get; set; } = -1;
        public bool RawPacketLogEnable { get; set; }
        private readonly int Timeout;// таймаут соединениия в милисекундах


        public event MessageRecievedEvent? MessageRecieved;
        public event SessionStateEvent? StatusChange; // событие изменение статуса.

        public ScanerAutoTcp(TcpClient tc, int t)
        {
            if (tc is null)
                throw new ArgumentNullException(nameof(tc), "Объект соединения не может быть null");

            Client = tc;
            IgnoreCloseDisconnectError = false;
            Timeout = t;
        }
        public BackgroundWorker Run()
        {
            BackgroundWorker worker = new();
            worker.DoWork += delegate
            {
                MainThread();
            };
            worker.RunWorkerCompleted += delegate (object? sender, RunWorkerCompletedEventArgs e)
            {
                try
                {
                    IgnoreCloseDisconnectError = true;
                    Client.Close();

                    if (e.Error != null)
                        Log.Write("SCT", $"Критический сбой соединение со сканером {ClientIpAddress}.\nОшибка {e.Error}", EventLogEntryType.Information, 21);
                    else
                        Log.Write("SCT", "Закрыто соединение со сканером " + ClientIpAddress, EventLogEntryType.Information, 22);
                }
                catch (Exception ex)
                {
                    Log.Write("SCT", $"Сбой закрытия соединения со сканером {ClientIpAddress}\n {ex}", EventLogEntryType.Information, 22);
                }
            };

            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerAsync();
            return worker;
        }
        private void MainThread()
        {
            if (Client is null)
                return;

            int errorCounter = 0;
            if (Client.Client.RemoteEndPoint is IPEndPoint endPoint)
                ClientIpAddress = endPoint.Address.ToString();

            int scanerId = -1;
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            Thread.CurrentThread.Name = $"TcpScanner{ClientIpAddress}";

            try
            {
                Log.Write("SCT", "Установлено соединение со сканером " + ClientIpAddress, EventLogEntryType.Information, 23);
                byte[] rcvDataBuffer = new byte[5000];
                Client.ReceiveTimeout = Timeout;

                //генерим событие сканер в онлайне
                StatusChange?.Invoke(Id, PeripheralsType.Scanner, SessionStates.OnLine);

                while (Client.Connected)
                {
                    //если произошло более 10 ошибок подряд. закрыть соединение
                    if (errorCounter == 10)
                    {
                        Client.Close();
                        //WSAEOPNOTSUPP 10045 Операция не поддерживается.
                        throw new SocketException(10045);
                    }

                    Array.Clear(rcvDataBuffer, 0, rcvDataBuffer.Length);
                    int count = Client.GetStream().Read(rcvDataBuffer, 0, rcvDataBuffer.Length);

                    if (count == 0)
                    {
                        errorCounter++;
                        continue;
                    }
                    errorCounter = 0;

                    if (rcvDataBuffer.Length == 0)
                        continue;

                    //получив пакет генерим событие с RAW данными
                    if (RawPacketLogEnable)
                        PacketLog(rcvDataBuffer, count);// RawDataRecieved?.Invoke(this, rcvDataBuffer, count);


                    LabelParcePacket:
                    //если данные получены проверить стартовый и стоповый символы.
                    string messageLine = MsgUtil.GetMessAtStart(rcvDataBuffer, count, 0x00, 0x03);

                    if (messageLine.Length == 0)
                        continue;

                    //scanerId = ParcePacketDL(messageLine);
                    scanerId = ParcePacketHIK(messageLine);

                    //очистить входной буфер и посмотреть есть ли еще пакет в ожидании обработки
                    Array.Clear(rcvDataBuffer);
                    count = 0;
                    goto LabelParcePacket;
                }
                Client.Close();
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10060)
                    Log.Write("SCN", $"Сканер {ClientIpAddress} не шлет диагностику! Соединение закрыто!", EventLogEntryType.Error, 22);
                else
                    Log.Write("SCN", $"Ошибка канала TCP сканера {ClientIpAddress}  {ex}", EventLogEntryType.Error, 22);
            }
            catch (Exception ex)
            {
                if (!IgnoreCloseDisconnectError)
                    Log.Write("SCT", ex.ToString(), EventLogEntryType.Error, 25);
            }
            finally
            {
                StatusChange?.Invoke(Id, PeripheralsType.Scanner, SessionStates.Disconnect);
            }
        }

        private int ParcePacketDL(string messageLine)
        {
            int scanerId;
            if (int.TryParse(messageLine[0].ToString(), out scanerId))
            {
                //выделить id сканера и сгенеить событие соеденения
                if (Id == -1)
                {
                    Id = scanerId;
                    StatusChange?.Invoke(Id, PeripheralsType.Scanner, SessionStates.OnLine);
                }

                switch (messageLine[2])
                {
                    case 'D':
                        //получен пакет диагностики пока не делать ничего
                        break;
                    case 'B':
                        if (messageLine[3] == 'D')
                            //получен пакет диагностики BD пока не делать ничего
                            break;

                        string msg = messageLine[4..];
                        MessageRecieved?.Invoke(this, scanerId, msg);
                        break;
                }
            }
            else
                throw new ArgumentNullException(nameof(messageLine), $"Пакет от сканера {ClientIpAddress} не распознан. Data={messageLine}");
            return scanerId;
        }
        private int ParcePacketHIK(string messageLine)
        {
            int scanerId = 1;

            //выделить id сканера и сгенеить событие соеденения
            if (Id == -1)
            {
                Id = scanerId;
                StatusChange?.Invoke(Id, PeripheralsType.Scanner, SessionStates.OnLine);
            }

            switch (messageLine[0])
            {
                //heartbeat
                case 'D':
                    //получен пакет диагностики пока не делать ничего
                    break;

                default: 
                    MessageRecieved?.Invoke(this, scanerId, messageLine);
                    break;
                //default:
                //    throw new ArgumentNullException(nameof(messageLine), $"Пакет от сканера {ClientIpAddress} не распознан. Data={messageLine}");

            }

            return scanerId;
        }

        private static void PacketLog(byte[] msgSource, int msgSourceSize)
        {
            DateTime d = DateTime.Now;

            try
            {
                byte[] header = Encoding.UTF8.GetBytes($"{d.ToString("HH:mm.ss.fff", CultureInfo.InvariantCulture)} src=");
                byte[] row = new byte[header.Length + msgSourceSize];
                Array.Copy(header, row, header.Length);
                Array.Copy(msgSource, 0, row, header.Length, msgSourceSize);
                Task.Factory.StartNew((_row) =>
                {
                    string? errpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
                    try
                    {
                        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Logs";
                        //создать директорию если надо
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        path += "\\Packet" + d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ".txt";

                        if (_row is not byte[] buffer)
                            return;

                        using FileStream sw = new(path, FileMode.Append | FileMode.OpenOrCreate, FileAccess.Write);
                        sw?.Write(buffer, 0, buffer.Length);
                        sw?.Flush();
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
