using System;
using System.Collections.Generic;
using System.Threading;
using Util;

namespace Peripherals
{
    public class LineStateInfo
    {

        private bool lineRun;
        private bool overflow;
        private bool pusherFail;
        private bool flowABusy;
        private bool flowBBusy;
        private bool waitBoxVerify;
        private bool flowOverflow;
        private byte activeFlow;

        //private bool _PalAplInWorck;
        //private bool _PalScanComplited;
        //private bool _PalGoToWork;
        //private bool _PackScannerFail;
        //private bool _VerifyScanerFail;
        //private bool _AlarmPusherCheckError;
        //private bool _AlarmAplicatorNotReady;
        //private bool _ApplicatorReady;
        //private bool _FailureLineStop;
        //private bool _RemoveCanisterResult;
        //private bool _RemoveCanisterResultActual;
        //private bool _powerOff;
        //private bool _lineBlockedByError;
        //private bool _ControlScanerFail;
        //private bool _ControlScanStopLine;
        //private bool _ControlScanerStreamFail;

        private ushort itemsInStackCounter;
        private ushort itemInBoxCounter;
        //private ushort _BoxBadCounter;
        //private ushort _BoxGoodCounter;
        //private ushort _BoxInPackMachine;
        //private ushort _LineStatus;

        //public string LastErrorBox { get; set; } = "";

        //private List<UncknowBox> _idBoxInPackMachine = new List<UncknowBox>();
        //private static ReaderWriterLockSlim _rwidBoxInPackMachine = new ReaderWriterLockSlim();


        //public List<UncknowBox> idBoxInPackMachine
        //{
        //    //get { return _rwidBoxInPackMachine; }
        //    get
        //    {
        //        if (_rwidBoxInPackMachine.TryEnterWriteLock(200))
        //        {
        //            try
        //            {
        //                return _idBoxInPackMachine;
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Write("PLC", ex.Message,System.Diagnostics.EventLogEntryType.Error, 3102);
        //            }
        //            finally
        //            {
        //                _rwidBoxInPackMachine.ExitWriteLock();
        //            }
        //        }
        //        else
        //        {
        //            Log.Write("PLC" ,"idBoxInPackMachine Превышено время ожидания записи команды",EventLogEntryType.Error, 3102);
        //        }

        //        return null;
        //    }
        //}
        public bool LineRun
        {
            get { return lineRun; }
            set
            {
                if (value != lineRun)
                {
                    lineRun = value;
                    StatusChange = true;
                }
            }
        }
        public bool Overflow
        {
            get { return overflow; }
            set
            {
                if (value != overflow)
                {
                    overflow = value;
                    StatusChange = true;
                }
            }
        }
        public bool PusherFail
        {
            get { return pusherFail; }
            set
            {
                if (value != pusherFail)
                {
                    pusherFail = value;
                    StatusChange = true;
                }
            }
        }     
        public bool FlowABusy
        {
            get { return flowABusy; }
            set
            {
                if (value != flowABusy)
                {
                    flowABusy = value;
                    StatusChange = true;
                }
            }
        }
        public bool FlowBBusy
        {
            get { return flowBBusy; }
            set
            {
                if (value != flowBBusy)
                {
                    flowBBusy = value;
                    StatusChange = true;
                }
            }
        }
        public bool WaitBoxVerify
        {
            get { return waitBoxVerify; }
            set
            {
                if (value != waitBoxVerify)
                {
                    waitBoxVerify = value;
                    StatusChange = true;
                }
            }
        }
        public bool FlowOverflow
        {
            get { return flowOverflow; }
            set
            {
                if (value != flowOverflow)
                {
                    flowOverflow = value;
                    StatusChange = true;
                }
            }
        }

        public ushort ItemsInStackCounter
        {
            get { return itemsInStackCounter; }
            set
            {
                if (value != itemsInStackCounter)
                {
                    itemsInStackCounter = value;
                    StatusChange = true;
                }
            }
        }
        public ushort ItemInBoxCounter
        {
            get { return itemInBoxCounter; }
            set
            {
                if (value != itemInBoxCounter)
                {
                    itemInBoxCounter = value;
                    StatusChange = true;
                }
            }
        }

        public byte ActiveFlow
        {
            get { return activeFlow; }
            set
            {
                if (value != activeFlow)
                {
                    activeFlow = value;
                    StatusChange = true;
                }
            }
        }
        

        #region old
        //public bool WhBrackStackFullOrSensorError
        //{
        //    get { return _WhPushCheck; }
        //    set
        //    {
        //        if (value != _WhPushCheck)
        //        {
        //            _WhPushCheck = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool PalAplInWorck
        //{
        //    get { return _PalAplInWorck; }
        //    set
        //    {
        //        if (value != _PalAplInWorck)
        //        {
        //            _PalAplInWorck = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool PalScanComplited
        //{
        //    get { return _PalScanComplited; }
        //    set
        //    {
        //        if (value != _PalScanComplited)
        //        {
        //            _PalScanComplited = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool PalGoToWork
        //{
        //    get { return _PalGoToWork; }
        //    set
        //    {
        //        if (value != _PalGoToWork)
        //        {
        //            _PalGoToWork = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool PackScannerFail
        //{
        //    get { return _PackScannerFail; }
        //    set
        //    {
        //        if (value != _PackScannerFail)
        //        {
        //            _PackScannerFail = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool VerifyScanerFail
        //{
        //    get { return _VerifyScanerFail; }
        //    set
        //    {
        //        if (value != _VerifyScanerFail)
        //        {
        //            _VerifyScanerFail = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool ControlScanerFail
        //{
        //    get { return _ControlScanerFail; }
        //    set
        //    {
        //        if (value != _ControlScanerFail)
        //        {
        //            _ControlScanerFail = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool ControlScanStopLine
        //{
        //    get { return _ControlScanStopLine; }
        //    set
        //    {
        //        if (value != _ControlScanStopLine)
        //        {
        //            _ControlScanStopLine = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool ControlScanerStreamFail
        //{
        //    get { return _ControlScanerStreamFail; }
        //    set
        //    {
        //        if (value != _ControlScanerStreamFail)
        //        {
        //            _ControlScanerStreamFail = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool AlarmPusherCheckError
        //{
        //    get { return _AlarmPusherCheckError; }
        //    set
        //    {
        //        if (value != _AlarmPusherCheckError)
        //        {
        //            _AlarmPusherCheckError = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool AplicatorNotReady
        //{
        //    get { return _AlarmAplicatorNotReady; }
        //    set
        //    {
        //        if (value != _AlarmAplicatorNotReady)
        //        {
        //            _AlarmAplicatorNotReady = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool ApplicatorReady
        //{
        //    get { return _ApplicatorReady; }
        //    set
        //    {
        //        if (value != _ApplicatorReady)
        //        {
        //            _ApplicatorReady = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool FailureLineStop
        //{
        //    get { return _FailureLineStop; }
        //    set
        //    {
        //        if (value != _FailureLineStop)
        //        {
        //            _FailureLineStop = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool RemoveCanisterResult
        //{
        //    get { return _RemoveCanisterResult; }
        //    set
        //    {
        //        if (value != _RemoveCanisterResult)
        //        {
        //            _RemoveCanisterResult = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool RemoveCanisterResultActual
        //{
        //    get { return _RemoveCanisterResultActual; }
        //    set
        //    {
        //        if (value != _RemoveCanisterResultActual)
        //        {
        //            _RemoveCanisterResultActual = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public bool PowerOff
        //{
        //    get { return _powerOff; }
        //    set
        //    {
        //        if (value != _powerOff)
        //        {
        //            _powerOff = value;
        //            StatusChange = true;
        //        }
        //    }
        //}

        //public bool LineBlockedByError
        //{
        //    get { return _lineBlockedByError; }
        //    set
        //    {
        //        if (value != _lineBlockedByError)
        //        {
        //            _lineBlockedByError = value;
        //            StatusChange = true;
        //        }
        //    }
        //}



        //public ushort BoxBadCounter
        //{
        //    get { return _BoxBadCounter; }
        //    set
        //    {
        //        if (value != _BoxBadCounter)
        //        {
        //            _BoxBadCounter = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public ushort BoxGoodCounter
        //{
        //    get { return _BoxGoodCounter; }
        //    set
        //    {
        //        if (value != _BoxGoodCounter)
        //        {
        //            _BoxGoodCounter = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public ushort BoxInPackMachine
        //{
        //    get { return _BoxInPackMachine; }
        //    set
        //    {
        //        if (value != _BoxInPackMachine)
        //        {
        //            _BoxInPackMachine = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        //public ushort LineStateCode
        //{
        //    get { return _LineStatus; }
        //    set
        //    {
        //        if (value != _LineStatus)
        //        {
        //            _LineStatus = value;
        //            StatusChange = true;
        //        }
        //    }
        //}
        #endregion

        public bool StatusChange { get; set; } = false;

        private  const int CmdBlockSize = 1;
        public bool UpdateValue(byte[] bData)
        {
            try
            {
                if (bData == null)
                    return false;

                
                byte[] data = new byte[CmdBlockSize];

                #region old
                //byte[] data = new byte[16];
                //byte[] ids = new byte[16];
                //byte[] ctrlBox = new byte[18];
                ////
                //if (_rwidBoxInPackMachine.TryEnterWriteLock(200))
                //{
                //    try
                //    {
                //        Array.Copy(bData, 460, ids, 0, 16);
                //        List<UncknowBox> _tmpArray = new List<UncknowBox>();

                //        for (int i = 0; i < 16; i += 2)
                //        {
                //            if(ids[i] > 0)
                //                _tmpArray.Add(new UncknowBox(ids[i],ids[i+1]));
                //        }
                //        //
                //        if (_tmpArray != _idBoxInPackMachine)
                //            _idBoxInPackMachine = _tmpArray;

                //        //StatusChange = true;
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Write("PLC", ex.Message,EventLogEntryType.Error, 3102);
                //    }
                //    finally
                //    {
                //        _rwidBoxInPackMachine.ExitWriteLock();
                //    }
                //}
                //else
                //{
                //    Log.Write("PLC", "idBoxInPackMachine Превышено время ожидания тега _idBoxInPackMachine",EventLogEntryType.Error, 3102);
                //}
                //
                #endregion

                Array.Copy(bData, 0, data, 0, CmdBlockSize);
                System.Collections.BitArray bitArray = new System.Collections.BitArray(data);

                StatusChange = false;

                LineRun = bitArray.Get(0);
                Overflow = bitArray.Get(1);
                PusherFail = bitArray.Get(2);
                FlowABusy = bitArray.Get(3);
                FlowBBusy = bitArray.Get(4);
                WaitBoxVerify = bitArray.Get(5);
                //PowerLoss =  bitArray.Get(6);
                FlowOverflow = bitArray.Get(7);

                ActiveFlow = bData[1];

                #region Counters
                byte[] d = new byte[2] { bData[3], bData[2] };
                ItemsInStackCounter = BitConverter.ToUInt16(d, 0);

                d[0] = bData[7];
                d[1] = bData[6];
                ItemInBoxCounter = BitConverter.ToUInt16(d, 0);
                #endregion

                //                Static
                //Run Bool
                //Overlfow    Bool
                //PusherFail  Bool
                //FlowABusy   Bool
                //FlowBBusy   Bool
                //waitBoxVerify   Bool
                //activeFlow  Byte
                //OnePackerLimit  UInt
                //TwoPakersLimit  UInt
                //ItemInBoxCounter    UInt
                //end Byte


                //WhBrackStackFullOrSensorError = bitArray.Get(5);
                //PalAplInWorck = bitArray.Get(6);
                //PalScanComplited = bitArray.Get(7);
                //PalGoToWork = bitArray.Get(8);
                //PackScannerFail = bitArray.Get(9);
                //VerifyScanerFail = bitArray.Get(10);
                //AlarmPusherCheckError = bitArray.Get(11);
                //AplicatorNotReady = bitArray.Get(12);
                //ApplicatorReady = bitArray.Get(13);
                //FailureLineStop = bitArray.Get(14);
                //RemoveCanisterResult = bitArray.Get(15);
                //RemoveCanisterResultActual = bitArray.Get(16);
                //PowerOff = bitArray.Get(17);
                //LineBlockedByError = bitArray.Get(18);

                //ControlScanerFail = bitArray.Get(20);
                //ControlScanStopLine = bitArray.Get(21);
                //ControlScanerStreamFail = bitArray.Get(22);

                //byte[] d = new byte[2] { data[5], data[4] };
                //PackBadCounter = BitConverter.ToUInt16(d, 0);

                //d[0] = data[7];
                //d[1] = data[6];
                //PackGoodCounter = BitConverter.ToUInt16(d, 0);

                //d[0] = data[9];
                //d[1] = data[8];
                //BoxBadCounter = BitConverter.ToUInt16(d, 0);

                //d[0] = data[11];
                //d[1] = data[10];
                //BoxGoodCounter = BitConverter.ToUInt16(d, 0);

                //d[0] = data[13];
                //d[1] = data[12];
                //BoxInPackMachine = BitConverter.ToUInt16(d, 0);

                //d[0] = data[15];
                //d[1] = data[14];
                //LineStateCode = BitConverter.ToUInt16(d, 0);

                //Array.Copy(bData, 492, ctrlBox, 0, ctrlBox.Length);
                //LastErrorBox = System.Text.Encoding.ASCII.GetString(ctrlBox, 0, ctrlBox.Length);

                if (StatusChange)
                    return true;
            }
            catch
            {
                //Status = -1;
            }
            return false;
        }
    }

    public class UncknowBox
    {
        public byte id;
        public bool full;

        public UncknowBox(byte _i, byte _f) { id = _i; full = (_f == 110); }
    }
}
