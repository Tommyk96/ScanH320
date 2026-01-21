#define X64

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace Util
{
    public delegate void LineStateEventHandler(int state);
    public class ICO300
    {

#if X64
        [DllImport("AxDIO64.dll")]
        private static extern bool AxInit();

        [DllImport("AxDIO64.dll")]
        private static extern bool AxInitGPIO();

        [DllImport("AxDIO64.dll")]
        private static extern bool AxOff();

        [DllImport("AxDIO64.dll")]
        private static extern bool AxSetDO(byte[] tmp);

        [DllImport("AxDIO64.dll")]
        private static extern bool AxSetDIODirection(byte[] tmp);

        [DllImport("AxDIO64.dll")]
        private static extern int AxGetDI(byte[] tmp);
#elif X32
        [DllImport("AxDIO32.dll")]
        private static extern bool AxInit();

        [DllImport("AxDIO32.dll")]
        private static extern bool AxInitGPIO();

        [DllImport("AxDIO32.dll")]
        private static extern bool AxOff();

        [DllImport("AxDIO32.dll")]
        private static extern bool AxSetDO(byte[] tmp);

        [DllImport("AxDIO32.dll")]
        private static extern bool AxSetDIODirection(byte[] tmp);

        [DllImport("AxDIO32.dll")]
        private static extern int AxGetDI(byte[] tmp);
#endif


        private static byte[] statDI = new byte[1];
        private static bool prewState;

        public static Dictionary<int, int> IO = new Dictionary<int, int> {
            { 1, 1 },
            { 2, 2 },
            { 3, 4 },
            { 4, 8 },
            { 5, 16 },
            { 6, 32 },
            { 7, 64 },
            { 8, 128 }
        };

        private System.ComponentModel.BackgroundWorker worker;
        private static ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        private AutoResetEvent waitEvent = new AutoResetEvent(false); //евент ожидания между циклами опроса
        //переменная содержащая команды для заприси в порт ввода-вывода
        private static CommandBlock ioCommandData = new CommandBlock();

        private static ReaderWriterLockSlim _actionsLock = new ReaderWriterLockSlim();
        private List<IoAction> actions = new List<IoAction>();
        //события
        public event LineStateEventHandler LineStateChangeEvent; // событие изменения статуса линии.
        private bool AxInitEx()
        {
            try
            {
                int i = 255;
                byte[] reg = new byte[1];
                byte[] DIO = new byte[1];


                if (!AxInit())
                    return false;

                DIO[0] = (byte)(i - 31);
                return AxSetDIODirection(DIO);
            }
            catch (Exception ex)
            {
                ex.ToString();
                return false;
            }
        }

        public void Run()
        {
          
            var autoEvent = new AutoResetEvent(false);

            worker = new System.ComponentModel.BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += delegate
            {
                Thread.CurrentThread.Name = "ICO300";

                if (!AxInitEx())
                    throw new Exception("Ошибка работы с модулем ввода-вывода");

              

                while (!worker.CancellationPending)
                {
                    //записать команды
                    AxSetDO(ioCommandData.GetCmdData());

                    //опросить состояния входов 
                    int v = AxGetDI(statDI);
                    byte Di = statDI[0];
                    bool power = ((Di & ICO300.IO[7]) != 64);

                    //седьмой
                    if (power && (prewState != power))
                        LineStateChangeEvent?.Invoke(0);
                    else if(!power &&(prewState != power))
                        LineStateChangeEvent?.Invoke(1);

                    prewState = power;

                    
                    waitEvent.WaitOne(1000);
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    //MessageBox.Show("There was an error! " + e.Error.ToString());
                    Log.Write("ICO." + e.Error.ToString());
                }
                else AxOff();
            };

            worker.RunWorkerAsync();

        }
        public void Stop()
        {
            try
            {
                if (worker?.WorkerSupportsCancellation == true)
                    worker.CancelAsync();
            }
            catch (Exception )
            {
                // Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".07:" + "Ошибка отсоединения от" + serverIpAddress + "\n\r" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 3);
            }

            return;
        }

        public bool SetAction(IoAction[] actions)
        {
            if (_rw.TryEnterWriteLock(200))
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
                                ioCommandData.WermaGreen = a.value;
                                break;
                            case 1:
                                ioCommandData.WermaRed = a.value;
                                break;
                            case 2:
                                ioCommandData.WermaSound = a.value;
                                break;
                            case 3:
                                ioCommandData.ClearCounter = a.value;
                                break;
                            case 4:
                                ioCommandData.LineStop = a.value;
                                break;
                          

                        }
                    }
                    waitEvent.Set();
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
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("Критическая ошибка очереди SetAction");
            }
            return false;
        }
        public bool WriteCommandBloc()
        {
            if (_rw.TryEnterWriteLock(200))
            {
                try
                {
                    waitEvent.Set();
                }
                catch (Exception )
                {
                    //Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".11:" + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 6);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            else
            {
                //Log.Write("PLC." + Thread.CurrentThread.ManagedThreadId + ".12:" + "Критическая ошибка записи в " + serverIpAddress + "\n"/*+ex.Message*/,EventLogEntryType.Error, MAIN_ERROR_CODE + 30);
            }
            return false;
        }
    }
    public static class IoActions
    {
        public static IoAction Green = new IoAction(0,true);
        public static IoAction RemoveGreen = new IoAction(0, false);
        public static IoAction FlashGreen = new IoAction(0, true,500);

        public static IoAction Red = new IoAction(1, true);
        public static IoAction RemoveRed = new IoAction(1, false);
        public static IoAction FlashRed = new IoAction(1, true,3000);

        public static IoAction Sound = new IoAction(2, true);  
        public static IoAction RemoveSound = new IoAction(2, false);
        public static IoAction FlashSound = new IoAction(2, true, 1000);

        public static IoAction ClearCounter = new IoAction(3, true);
        public static IoAction RemoveClearCounter = new IoAction(3, false);
        public static IoAction FlashClearCounter = new IoAction(3, true, 1000);

        public static IoAction Stop = new IoAction(4, true);
        public static IoAction RemoveStop = new IoAction(4, false);

        public static IoAction UPS = new IoAction(64, true);

    }

    public class IoAction
    {
        public int Io;
        public bool value;
        public int timeoutToResetInMilisec;
        public DateTime setTimeShtamp;
        public IoAction(int i, bool v,int _t = 0)
        {
            Io = i;
            value = v;
            timeoutToResetInMilisec = _t;
        }
    }

    public class CommandBlock
    {
        public bool WermaGreen { get; set; }      
        public bool WermaRed { get; set; }
        public bool WermaSound { get; set; }    
        public bool LineStop { get; set; }
        public bool ClearCounter { get; set; }
        //public bool LentaIsRuning { get; set; }

        private byte[] cmdData = new byte[1];
        //private Int32 cmdData = 0;
        public byte[] GetCmdData()
        {
            System.Collections.BitArray bitArray = new System.Collections.BitArray(cmdData);
            bitArray.SetAll(false);
            bitArray.Set(0, WermaGreen); //k11
            bitArray.Set(1, WermaRed);//12
            bitArray.Set(2, WermaSound);//13
            bitArray.Set(3, ClearCounter);//14
            bitArray.Set(4, LineStop);//k15
            //входы!
            bitArray.Set(5, false);
            bitArray.Set(6, false);
            bitArray.Set(7, false);
                      
            bitArray.CopyTo(cmdData, 0);
            return cmdData;
        }

       public override bool Equals(object obj)
        {
            if (obj != null)
            {
                CommandBlock newVal = (CommandBlock)obj;

                return (WermaGreen == newVal.WermaGreen) &&
                    (WermaRed == newVal.WermaRed) &&
                    (WermaSound == newVal.WermaSound) &&
                    (ClearCounter == newVal.ClearCounter) &&
                    (LineStop == newVal.LineStop) ;
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 84278560;
            //hashCode = hashCode * -1521134295 + WermaGreen.GetHashCode();
            //hashCode = hashCode * -1521134295 + WermaRed.GetHashCode();
            //hashCode = hashCode * -1521134295 + WermaSound.GetHashCode();
            //hashCode = hashCode * -1521134295 + LineStop.GetHashCode();
            //hashCode = hashCode * -1521134295 + ClearCounter.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(cmdData);
            return hashCode;
        }
    }
}
