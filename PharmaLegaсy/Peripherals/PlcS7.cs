using PharmaLegacy;
using PharmaLegaсy.Models;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace Peripherals
{
    //public struct testStruct
    //{
    //    public bool varBool0;
    //    public bool varBool1;
    //    public bool varBool2;
    //    public bool varBool3;
    //    //  public bool varBool4;
    //    //  public bool varBool5;
    //    // public bool varBool6;
    //    // public bool varBool7;
    //    //  public bool varBool8;
    //    //  public bool varBool9;
    //    // public bool varBool10;
    //    // public bool varBool11;
    //    // public bool varBool12;
    //    // public bool varBool13;
    //    // public bool varBool14;
    //    //  public bool varBool15;
    //    // public bool varBool16;
    //    // public bool varBool17;
    //    // public bool varBool18;
    //    // public bool varBool19;
    //    // public bool varBool20;
    //    // public bool varBool21;
    //}

    public delegate void PlcLineStateEventHandler(LineStateInfo newState);
    public delegate void ReceivedDataFromLineEventHandler(byte[] data);


    public enum SystemParam
    {
        LineStart = 1,
        LineStop = 2,
        PusherFail = 3,
        PusherFailAlarm = 4,
        ObjLenFail = 5,
        ObjLenFailAlarm = 6
    }
    public class PlcCommandBlock
    {
        public bool BoxFlowARelease { get; set; }
        public bool BoxFlowBRelease { get; set; }
        public bool OrderIsSet { get; set; }
        public bool FlowAEnable { get; set; }
        public bool FlowBEnable { get; set; }
        public bool ClearCounter { get; set; }  
        public bool LineRunEnable { get; set; }// признак работы линии 
        public bool BoxRelease { get; set; }
        public bool WermaGreen { get; set; }
        public bool WermaRed { get; set; }
        public bool WermaSound { get; set; }
        public bool a7 { get; set; }
        public bool a8 { get; set; }
        public bool a9 { get; set; }
        public bool a10 { get; set; }
        

        private byte[] verifyMap = new byte[5];

        //public bool PusherOn { get; set; }
        //public bool BrackAllItem { get; set; }
        //public bool LentaIsRuning { get; set; }

        public string removeCanisterCode { get; set; }
        public byte RemoveBoxId { get; set; }

        private byte[] cmdData = new byte[2];
        //private Int32 cmdData = 0;
        public byte[] GetCmdData()
        {
            List<byte> values = new List<byte>();
            System.Collections.BitArray bitArray = new System.Collections.BitArray(cmdData);
            bitArray.SetAll(false);
            bitArray.Set(0, BoxFlowARelease);
            bitArray.Set(1, BoxFlowBRelease);
            bitArray.Set(2, OrderIsSet);
            bitArray.Set(3, FlowAEnable);
            bitArray.Set(4, FlowBEnable);
            bitArray.Set(5, ClearCounter);
            bitArray.Set(6, LineRunEnable);
            bitArray.Set(7, BoxRelease);
            bitArray.Set(8, WermaGreen);
            bitArray.Set(9, WermaRed);
            bitArray.Set(10, WermaSound);
            bitArray.Set(11, a7);
            bitArray.Set(12, a8);
            bitArray.Set(13, a9);
            bitArray.Set(14, a10);
            
            bitArray.CopyTo(cmdData, 0);
            values.AddRange(cmdData);
            //values.AddRange(verifyMap);
          

            //if (RemoveBox)
            //{
            //    byte[] prefData = S7.Net.Types.String.ToByteArray(removeCanisterCode);
            //    values.AddRange(prefData);
            //}

            return values.ToArray();
        }
      
        public void EndSendToPlc(byte[] cmdSend)
        {
            try
            {
                //перед сбросом проверяем не были ли измененые параметры во время записи. если были то не сбрасываем оставляем для записи на следующем цыкле
                System.Collections.BitArray bitArray = new System.Collections.BitArray(cmdSend);
                //сбросить сигналы событий
                if (bitArray.Get(0) == BoxFlowARelease)
                    BoxFlowARelease = false;
                if (bitArray.Get(1) == BoxFlowBRelease)
                    BoxFlowBRelease = false;
                if (bitArray.Get(5) == ClearCounter)
                    ClearCounter = false;
                if (bitArray.Get(7) == BoxRelease)
                    BoxRelease = false;
                

                //if (bitArray.Get(8) == answerAccept)
                //    answerAccept = false;
                //if (bitArray.Get(12) == a8)
                //    a8 = false;
                //if (bitArray.Get(13) == a9)
                //    a9 = false;
                //if (bitArray.Get(14) == a10)
                //    a10 = false;
                //if (bitArray.Get(9) == a5)
                //    a5 = false;
                //cmdBlock.RemoveBoxId = 0;

                //return true;
            }
            catch { }
        }
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                PlcCommandBlock newVal = (PlcCommandBlock)obj;

                return (BoxFlowARelease == newVal.BoxFlowARelease) &&
                    (BoxFlowBRelease == newVal.BoxFlowBRelease) &&
                    (OrderIsSet == newVal.OrderIsSet) &&
                    (FlowAEnable == newVal.FlowAEnable) &&
                    (FlowBEnable == newVal.FlowBEnable) &&
                    (ClearCounter == newVal.ClearCounter) &&
                    (LineRunEnable == newVal.LineRunEnable) &&
                    (BoxRelease == newVal.BoxRelease) &&
                    (WermaGreen == newVal.WermaGreen) &&
                    (WermaSound == newVal.WermaSound) &&
                    (WermaRed == newVal.WermaRed);
            }
            else
                return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    public enum PeripheralsType
    {
        Scanner = 0,
        PCL = 1,
        SQL = 2
    }

    public enum SessionStates
    {
        Uncknow,
        OnLine,
        Disconnect,
        DeviceError,
        DeviceWarning,
        DevicePaused
    }

    public delegate void SessionStateEventHandler(object sender, PeripheralsType type, SessionStates data);

    public class PlcS7
    {
        private Plc plc = null;
        private string serverIpAddress;
        private System.ComponentModel.BackgroundWorker worker;
        private static ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        private static ReaderWriterLockSlim _startNewThread = new ReaderWriterLockSlim();

        private const int MAIN_ERROR_CODE = 4400;
        private bool clientReconnect = false;
        private AutoResetEvent waitEvent = new AutoResetEvent(false); //евент ожидания между циклами опроса
        private static bool WriteLineInfo;

        //события
        public event PlcLineStateEventHandler LineStateChangeEvent; // событие изменения статуса линии.
        public event ReceivedDataFromLineEventHandler ReceivedDataFromLineEvent; // событие данные с линии получены.
        public event SessionStateEventHandler StatusChangeEvent; // событие изменение статуса контроллера.
        

        //блоки данных
        private LineStateInfo _lineInfo = new LineStateInfo();
        private PlcCommandBlock cmdBlock = new PlcCommandBlock();

        public PlcCommandBlock Command
        {
            get { return cmdBlock; }
            set
            {
                if (value != cmdBlock)
                {
                    if (_rw.TryEnterWriteLock(200))
                    {
                        try
                        {
                            cmdBlock = value;
                            waitEvent.Set();
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                        }
                        finally
                        {
                            _rw.ExitWriteLock();
                        }
                    }
                    else
                    {
                        Log.Write("PLC." + Environment.CurrentManagedThreadId + ".01:" + "Превышено время ожидания записи команды" + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
                    }
                }
            }
        }
        public PlcCommandBlock DelayedCommand
        {
            get { return cmdBlock; }
            set
            {
                if (value != cmdBlock)
                {
                    if (_rw.TryEnterWriteLock(200))
                    {
                        try
                        {
                            cmdBlock = value;
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                        }
                        finally
                        {
                            _rw.ExitWriteLock();
                        }
                    }
                    else
                    {
                        Log.Write("PLC." + Environment.CurrentManagedThreadId + ".01:" + "Превышено время ожидания записи команды" + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
                    }
                }
            }
        }
        public PlcS7(string ip, int t)
        {
            serverIpAddress = ip;
        }

        //информация о задании
        private string PartNum = "";
        private string Product = "";
        private int ItemInBox;
        private int packInCurBox;

        private bool Connect()
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    if (plc != null)
                    {
                        if (plc.IsConnected)
                            return true;
                    }

                    if (plc == null)
                    {
                        plc = new Plc(CpuType.S71200, serverIpAddress, 0, 1);
                        plc.Open();
                        //client.SendTimeout = 500;
                        //diagCodeTimeFresh.Restart();
                        if (plc.LastErrorCode == 0)
                            StatusChangeEvent?.Invoke(this,PeripheralsType.PCL,SessionStates.OnLine);
                        else
                            StatusChangeEvent?.Invoke(this, PeripheralsType.PCL, SessionStates.Uncknow);
                    }
                    else
                        plc.Open();

                    if (plc.IsConnected)
                        Log.Write("PLC." + Environment.CurrentManagedThreadId + ".02:" + "Установлено соединение с " + serverIpAddress,EventLogEntryType.Information, MAIN_ERROR_CODE + 1);
                    return true;

                }
                catch (Exception ex)
                {
                    Disconnect();
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".03:" + "Ошибка соединения с " + serverIpAddress + "\n\r" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".04:" + "Connect превышена очередь");
            }
            return false;
        }
        /// <summary>
        /// Вызывается разныши внутренными функциями для уничтожения объекта client
        /// </summary>
        private void Disconnect()
        {
            try
            {
                if (plc == null)
                    return;

                if (plc?.IsConnected == true)
                {
                    //client.GetStream().Close();
                    plc?.Close();
                }

            }
            catch (ObjectDisposedException ex)
            {
                //if (ex.ObjectName == "System.Net.Sockets.TcpClient")
                //    client = null;
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".05:" + ex.ToString());
            }
            catch (NullReferenceException ex)
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".06:" + ex.ToString());
            }
            catch { }
            finally
            {
                plc = null;
                StatusChangeEvent?.Invoke(this, PeripheralsType.PCL, SessionStates.Disconnect);
            }
        }

        public void Stop()
        {
            try
            {

                if (worker != null)
                    worker.CancelAsync();

                if (plc == null)
                    return;

                Disconnect();
                //Log.Write("Закрывается соединение с принтером X1Jet" + serverIpAddress + " Порт " + serverPort.ToString() + "\n\r",EventLogEntryType.Error, MAIN_ERROR_CODE + 1);

            }
            catch (Exception ex)
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".07:" + "Ошибка отсоединения от" + serverIpAddress + "\n\r" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 3);
            }

            return;
        }
        public bool Run()
        {
            var autoEvent = new AutoResetEvent(false);

            worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += delegate
            {
                //задержка запуска связанныя с тем чтоб форма успела создатся если запускают с конструктора форм
                Thread.Sleep(1000);

                while (!worker.CancellationPending)
                {
                    //если соединение успешно установлено
                    if (Connect())
                    {
                        //Program.systemState.Printer = (int)DeviceState.good;
                        ReciveThread(null);
                    }
                    //
                    autoEvent.WaitOne(1000);
                }
            };
            //контроль состояния потока
            worker.ProgressChanged += delegate
            {
                if (_rw.TryEnterWriteLock(500))
                {
                    try
                    {
                        if (plc == null)
                            return;

                        //если клиент не соединен ничего не делать
                        if (!plc.IsConnected)
                            return;
                    }
                    catch
                    {

                    }
                    finally
                    {
                        _rw.ExitWriteLock();
                    }
                }
                else
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".08:" + "превышено время ожидания отклика от функции диагностики");
                }

            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                try
                {
                    Disconnect();
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".09:" + "Закрыто соединение с " + serverIpAddress,EventLogEntryType.Information, MAIN_ERROR_CODE + 4);
                }
                catch
                {

                }
                //перекинуть ошибку дальше 
                if (e.Error != null)
                {
                    //сохранить ошибку для обработки в меморидампе
                    Exception ex = e.Error.InnerException;
                    ex?.Message.ToString();

                    throw new Exception("PLC." + Environment.CurrentManagedThreadId + ".10:" + "Сбой в потоке ", e.Error);
                }
            };

            //
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            
            worker.RunWorkerAsync();
            return true;
        }

        public bool WriteCommandBloc()
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    waitEvent.Set();
                }
                catch (Exception ex)
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".11:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".12:" + "Критическая ошибка записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }
        public void WriteOrderInfo(string pr, string pn, int ib,int pIncBox)
        {
            PartNum = pn;
            Product = pr;
            ItemInBox = ib;
            packInCurBox = pIncBox;
            WriteLineInfo = true;

        }
        public bool SetAction(IoAction[] actions)
        {

            try
            {
                if (actions == null)
                    return false;
                // if (ioReadyToUse)
                //  {
                foreach (IoAction a in actions)
                {
                    switch (a.Io)
                    {
                        case 0:
                            //ioCommandData.WermaGreen = a.value;
                            DelayedCommand.WermaGreen = a.value;
                            break;
                        case 1:
                            //ioCommandData.WermaRed = a.value;
                            DelayedCommand.WermaRed = a.value;
                            break;
                        case 2:
                            // ioCommandData.WermaSound = a.value;
                            DelayedCommand.WermaSound = a.value;
                            break;
                        case 3:
                            ;
                            //ioCommandData.ClearCounter = a.value;
                            //DelayedCommand.ClearCounter = a.value;
                            break;
                        case 4:
                            ;
                            //ioCommandData.LineStop = a.value;
                            //DelayedCommand.LineStop = a.value;
                            break;


                    }
                }
                //waitEvent.Set();
                return true;
                // }
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233052)//0x8007007E)
                {
                    //systemState.StatusText = "Критическая ошибка\n" + ex.Message;
                    //systemState.StatusBackground = Brushes.Red;
                }



                Log.Write(ex.Message);
            }


            return false;
        }

        private bool WriteOrderInfoToPLC()
        {
            // string prefix = ""; string sufix = ""; int maxNoread; int lineMode; int curNoRead; ushort packSizeInPulse;
            // ushort scanPointInPulse; ushort pushPointInPulse; ushort pushFailPointInPulse;

            //if (_rw.TryEnterWriteLock(200))
            // {
            try
            {
                if (plc == null)
                    return false;

                if (!plc.IsConnected)
                    return false;

                List<byte> values = new List<byte>();
                plc.ClearLastError();
                ErrorCode err = plc.Write("DB2.DBW2", (ushort)ItemInBox);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                plc.ClearLastError();
                err = plc.Write("DB2.DBW4", (ushort)packInCurBox);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


                //**************************
                if (!string.IsNullOrEmpty(Product))
                {

                    byte[] utf8bytes = Encoding.UTF8.GetBytes(Product);
                    byte[] win1252Bytes = Encoding.Convert(
                                    Encoding.UTF8, Encoding.ASCII, utf8bytes);// Encoding.GetEncoding("windows-1252"), utf8bytes);


                    byte[] productData = S7.Net.Types.String.ToByteArray(Product);
                    values.Clear();
                    values.Add((byte)productData.Length);//добавить максимальную длинну строки
                    values.Add((byte)productData.Length);//добавить текущую длинну строки
                    values.AddRange(productData);

                    plc.ClearLastError();
                    err = plc.WriteBytes(DataType.DataBlock, 2, 6, values.ToArray());
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);
                }
                //************************** 
                if (!string.IsNullOrEmpty(PartNum))
                {
                    byte[] PartNumData = S7.Net.Types.String.ToByteArray(PartNum);
                    values.Clear();
                    values.Add((byte)PartNumData.Length);//добавить максимальную длинну строки
                    values.Add((byte)PartNumData.Length);//добавить текущую длинну строки
                    values.AddRange(PartNumData);

                    plc.ClearLastError();
                    err = plc.WriteBytes(DataType.DataBlock, 2, 68, values.ToArray());
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);
                }


                //if (RemoveBox)
                //{
                //    byte[] prefData = S7.Net.Types.String.ToByteArray(removeCanisterCode);
                //    values.AddRange(prefData);
                //}


                //plc.ClearLastError();
                //ErrorCode err = plc.Write("DB17.DBW0", (ushort)CanisterInBox);
                //if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                //    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


                //byte[] part = System.Text.Encoding.UTF8.GetBytes(PartNum);
                //plc.ClearLastError();
                //err = plc.WriteBytes(DataType.DataBlock, 17, 2, part);
                //if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                //    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                #region old
                /*
                byte[] prefData = S7.Net.Types.String.ToByteArray(prefix);
                List<byte> values = new List<byte>();
                values.Add((byte)prefix.Length);//добавить максимальную длинну строки
                values.Add((byte)prefix.Length);//добавить текущую длинну строки
                values.AddRange(prefData);

                plc.ClearLastError();
                ErrorCode err = plc.WriteBytes(DataType.DataBlock, 2, 0, values.ToArray());
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


                //**************************
                byte[] suffData = S7.Net.Types.String.ToByteArray(sufix);
                values.Clear();
                values.Add((byte)prefix.Length);//добавить максимальную длинну строки
                values.Add((byte)prefix.Length);//добавить текущую длинну строки
                values.AddRange(suffData);

                plc.ClearLastError();
                err = plc.WriteBytes(DataType.DataBlock, 2, 256, values.ToArray());
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


                //********************
                plc.ClearLastError();
                err = plc.Write("DB2.DBW512", (ushort)maxNoread);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


                plc.ClearLastError();
                err = plc.Write("DB1.DBW4", (ushort)curNoRead);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                //записать  максимальный и минимальный размер объекта в пульсах
                ushort MaxObjLenInPulse =0, MinObjLenInPulse=0;
                //MaxObjLenInPulse = Convert.ToUInt16(packSizeInPulse * 1.15);
                //MinObjLenInPulse = Convert.ToUInt16(packSizeInPulse * 0.85);

               // MaxObjLenInPulse = Convert.ToUInt16(packSizeInPulse + (10 / L2DataService.Properties.Settings.Default.PulseInMilimetr));
                decimal minPackSize = 0;// packSizeInPulse - (10 / L2DataService.Properties.Settings.Default.PulseInMilimetr);

                if (minPackSize > 0)
                    MinObjLenInPulse = Convert.ToUInt16(minPackSize);
                else
                    MinObjLenInPulse = 0;

                plc.ClearLastError();
                err = plc.Write("DB2.DBW516", (ushort)MaxObjLenInPulse);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                plc.ClearLastError();
                err = plc.Write("DB2.DBW518", (ushort)MinObjLenInPulse);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                //записать положение сканера в импульсах
                plc.ClearLastError();
                err = plc.Write("DB2.DBW522", (ushort)scanPointInPulse);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                //записать положение пушера в импульсах
                plc.ClearLastError();
                err = plc.Write("DB2.DBW520", (ushort)pushPointInPulse);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                //записать положение метки ошибки отбраковки
                plc.ClearLastError();
                err = plc.Write("DB2.DBW524", (ushort)pushFailPointInPulse);
                if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                    throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);
                    */
                #endregion
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".13:" + ex.ToString(),EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
            }
            finally
            {
                //  _rw.ExitWriteLock();
            }
            //}
            //else
            //{
            //Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".14:" + "Критическая ошибка записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            //}

            return false;
        }
        public bool RecreatePAckMAchineQueue(byte NewPackNum)
        {

            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    if (plc == null)
                        return false;

                    if (!plc.IsConnected)
                        return false;


                    List<byte> values = new List<byte>();
                    values.Add(NewPackNum);



                    plc.ClearLastError();
                    ErrorCode err = plc.WriteBytes(DataType.DataBlock, 15, 26, values.ToArray());
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                    //задать команду и сгенерить событие

                    waitEvent.Set();

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".17:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".18:" + "Критическая ошибка отправки записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }
        public bool ClearLine()
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    if (plc == null)
                        return false;

                    if (!plc.IsConnected)
                        return false;

                    //очистить данные со старого задания
                    plc.ClearLastError();
                    ErrorCode err = plc.Write("DB2.DBW514", (ushort)1);
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".15:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".16:" + "Критическая ошибка записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }
        public void ClearCounter()
        {
            DelayedCommand.ClearCounter = true;
        }
        public void StartLine(bool val)
        {
            DelayedCommand.LineRunEnable = val;
        }

        public void BoxRelease(WorckMode scannerSide)
        {
            if (scannerSide == WorckMode.Left)
            {
                DelayedCommand.BoxFlowARelease = true;
            }
            else if (scannerSide == WorckMode.Right)
            {
                DelayedCommand.BoxFlowBRelease = true;
            }
        }

        public bool StartReleaseItem(ushort releaseCount)
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    if (plc == null)
                        return false;

                    if (!plc.IsConnected)
                        return false;

                    
                    List<byte> values = new List<byte>();
                    byte[] idData = BitConverter.GetBytes(releaseCount);
                    //values.Add(0x00);//пустой байт для выравнивания
                    values.Add(idData[1]);
                    values.Add(idData[0]);
                    //values.Add(0x01);
                    

                    plc.ClearLastError();
                    ErrorCode err = plc.WriteBytes(DataType.DataBlock, 15, 22, values.ToArray());
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                    //задать команду и сгенерить событие
                    cmdBlock.WermaSound = true;
                    waitEvent.Set();

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".17:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".18:" + "Критическая ошибка отправки записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }
        public bool RemoveBoxInPackMachineById(byte releaseCount)
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    if (plc == null)
                        return false;

                    if (!plc.IsConnected)
                        return false;


                    List<byte> values = new List<byte>();
                    values.Add(releaseCount);
                    


                    plc.ClearLastError();
                    ErrorCode err = plc.WriteBytes(DataType.DataBlock, 15, 24, values.ToArray());
                    if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                        throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);

                    //задать команду и сгенерить событие
                  
                    waitEvent.Set();

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("PLC." + Environment.CurrentManagedThreadId + ".17:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".18:" + "Критическая ошибка отправки записи в " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }

        private List<byte> ReadMultipleBytes(int numBytes, int db, int startByteAdr = 0)
        {
            List<byte> resultBytes = new List<byte>();
            int index = startByteAdr;
            while (numBytes > 0)
            {
                var maxToRead = (int)Math.Min(numBytes, 200);
                byte[] bytes = plc.ReadBytes(DataType.DataBlock, db, index, (int)maxToRead);
                if (bytes == null)
                    return new List<byte>();
                resultBytes.AddRange(bytes);
                numBytes -= maxToRead;
                index += maxToRead;
            }
            return resultBytes;
        }
        private byte[] ReadMultipleBytes2(int db, int startByteAdr,int numBytes)
        {
            byte[] resultBytes = new byte[numBytes];
            int index = startByteAdr;
            while (numBytes > 0)
            {
                var maxToRead = (int)Math.Min(numBytes, 200);
                byte[] bytes = plc.ReadBytes(DataType.DataBlock, db, index, (int)maxToRead);
                if (bytes == null)
                    return new byte[0];
                Array.Copy(bytes, 0, resultBytes, index, bytes.Length);
                //resultBytes.AddRange(bytes);
                numBytes -= maxToRead;
                index += maxToRead;
            }
            return resultBytes;
        }
#pragma warning disable SYSLIB0011 // Тип или член устарел
        public static object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();

            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);


            object obj = binForm.Deserialize(memStream);


            return obj;
        }
        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
                return default(T);
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }
#pragma warning restore SYSLIB0011 // Тип или член устарел
        private void ReciveThread(object o)
        {
            System.Diagnostics.Stopwatch sw = new ();
            long step1 = 0, step2 = 0, step3 = 0;
            //byte[] data = new byte[130];
            //data[0] = 3;
            //data[129] = 4;
            try
            {
                while (plc.IsConnected)
                {
                    if (_rw.TryEnterWriteLock(1000))
                    {
                        sw.Restart();
                        try
                        {
                            //скопировать данные для записи
                            byte[] cmdSend = cmdBlock.GetCmdData();
                            //записать блок команд
                            plc.ClearLastError();
                            ErrorCode err = ErrorCode.NoError;
                            err = plc.WriteBytes(DataType.DataBlock, 2, 0, cmdSend);
                        
                            if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
                                throw new Exception("Ошибка обмена данными с :" + plc.LastErrorString);
                            //возврат некоторых значений на дефолт после запси в плк
                            cmdBlock.EndSendToPlc(cmdSend);
                            
                            //
                            step1 = sw.ElapsedMilliseconds;
                            //считать массив данных
                            plc.ClearLastError();
                            byte[] bytes2 = plc.ReadBytes(DataType.DataBlock, 7, 0, 8);
                            //byte[] bytes2 = new byte[6];
                            //byte[] bytes2 = ReadMultipleBytes2( 5, 0, 510); 
                            if (plc.LastErrorCode != ErrorCode.NoError)
                                throw new Exception("Ошибка обмена данными с :" + plc.LastErrorString);

                            //
                            step2 = sw.ElapsedMilliseconds - step1;

                            //обработать карту верифицированных номеров  
                            Task.Factory.StartNew(() =>
                              {
                                  if (_startNewThread.TryEnterWriteLock(100))
                                  {
                                      try
                                      {
                                          if (_lineInfo.UpdateValue(bytes2))
                                              LineStateChangeEvent?.Invoke(_lineInfo);

                                          ReceivedDataFromLineEvent?.Invoke(bytes2);
                                      }
                                      finally
                                      {
                                          _startNewThread.ExitWriteLock();
                                      }
                                  }
                                  else
                                  {
                                      Log.Write("PLC." + Environment.CurrentManagedThreadId + ".01:" + "Цикл обновления пропущен. Не завершился предыдущий!",EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
                                  }
                              }).ConfigureAwait(false);   
                            
                            step3 = sw.ElapsedMilliseconds - (step2+step1);

                            if (WriteLineInfo)
                            {
                                WriteLineInfo = false;
                                WriteOrderInfoToPLC();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write("PLC." + Environment.CurrentManagedThreadId + ".19:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                        }
                        finally
                        {
                            _rw.ExitWriteLock();
                        }
                    }
                    else
                    {
                        Log.Write("PLC." + Environment.CurrentManagedThreadId + ".20:" + "Критическая обмена данными с  " + serverIpAddress /*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
                    }

                    if (sw.ElapsedMilliseconds > 500)
                        Log.Write("PLC." + Environment.CurrentManagedThreadId + ".21:" + "Время обмена выше нормы :" + sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) 
                            + " step1:" + step1.ToString(CultureInfo.InvariantCulture) + " step2:" + step2.ToString(CultureInfo.InvariantCulture) +" step3:"+step3.ToString(CultureInfo.InvariantCulture));

                    waitEvent.WaitOne(300);

                }
            }
            catch (NullReferenceException ex)
            {
                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".22:" + ex.ToString());
            }
            catch (Exception ex)
            {
                if (worker.CancellationPending || clientReconnect)
                    return;

                Log.Write("PLC." + Environment.CurrentManagedThreadId + ".23:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 5);
            }
            finally
            {
                Disconnect();
            }
        }
        //public bool RemoveCanister(string _canCode)
        //{
        //    if (_rw.TryEnterWriteLock(1000))
        //    {
        //        try
        //        {
        //            if (plc == null)
        //                return false;

        //            if (!plc.IsConnected)
        //                return false;

        //            cmdBlock.removeCanisterCode = _canCode;
        //            cmdBlock.RemoveBox = true;
                   
        //            waitEvent.Set();

        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".13:" + ex.ToString(),EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
        //        }
        //        finally
        //        {
        //            _rw.ExitWriteLock();
        //        }
        //    }
        //    else
        //    {
        //        Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".14:" + "Критическая ошибка записи в " + serverIpAddress + "\n"/*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
        //    }
        //    return false;
        //}
        //public bool RemoveCanisterOld(string _canCode)
        //{
        //    if (_rw.TryEnterWriteLock(1000))
        //    {
        //        try
        //        {
        //            if (plc == null)
        //                return false;

        //            if (!plc.IsConnected)
        //                return false;

        //            byte[] prefData = S7.Net.Types.String.ToByteArray(_canCode);
        //            List<byte> values = new List<byte>();
        //          //  values.Add((byte)_canCode.Length);//добавить максимальную длинну строки
        //          //  values.Add((byte)_canCode.Length);//добавить текущую длинну строки
        //            values.AddRange(prefData);

        //            byte[] d1 = new byte[1];
        //            System.Collections.BitArray bitArray = new System.Collections.BitArray(d1);
        //            bitArray.SetAll(false);
        //            bitArray.Set(0, true);             
        //            bitArray.CopyTo(d1, 0);

        //            values.AddRange(d1);

        //            //
        //            plc.ClearLastError();
        //            ErrorCode err = plc.WriteBytes(DataType.DataBlock, 15, 0, values.ToArray());
        //            if ((plc.LastErrorCode != ErrorCode.NoError) || (err != ErrorCode.NoError))
        //                throw new Exception("Ошибка обмена данными с PLC:" + plc.LastErrorString);


          

        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".13:" + ex.ToString(),EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
        //        }
        //        finally
        //        {
        //            _rw.ExitWriteLock();
        //        }
        //    }
        //    else
        //    {
        //        Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".14:" + "Критическая ошибка записи в " + serverIpAddress + "\n"/*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
        //    }
        //    return false;
        //}
    }
}
