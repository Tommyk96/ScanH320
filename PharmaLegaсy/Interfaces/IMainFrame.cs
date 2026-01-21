using PharmaLegacy.data;
using PharmaLegaсy.Models;
using Util;

namespace PharmaLegaсy.Interfaces
{
    public delegate void CodeAddEventHandler(AddCodeType typeRead);
    public delegate void ScanDataEventHandler(string data);
    public delegate void EnterUserEventHandler(string data);

    public interface IMainFrame
    {
        //public AggregateJob Job { get; }
        public bool ProcessCode(GsLabelData ld, string data, out bool dropSequence);
        //собития сканера
        public event CodeAddEventHandler codeAddEventHandler;

        public void Print();
        public bool ClearBox(bool ShowQuestionDialog = true, bool ClearCurentBox = false, bool suspendMsg = false);
        public void BtnBrack();
        public void BtnSample();
        public void ReprintEvent();
        public void SelectDrobBoxOrDropCode();
        public void BtnHelp();
        public void btnCloseBox(bool stop);
        public bool StartLine(bool showWarninDialog = true);
        public void StopLine();
        public Task CreatePallete();
        public Task PrintPallete();
        public void CloseFullBox(bool autoVerify);
        public void ShowMessage(string msg, EventLogEntryType level, int msgId = 0, bool safeToLog = true);
        public bool ProssedData(string indata, WorckMode scannerSide);
        
        public Task TestSetGreenLight(bool on);
        public Task TestSetRedLight(bool on);

        public Task TestFunc();
    }
}
