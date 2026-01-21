using System.IO.Ports;
using Util;


namespace Peripherals
{
    public class ScannerRs232
    {
        public static readonly byte[] ScanResultGood = new byte[29] { 0x1B, 0x5B, 0x36, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x33, 0x71, 0x1B, 0x5B, 0x37, 0x71, 0x0D };
        public static readonly byte[] ScanResultBad = new byte[29] { 0x1B, 0x5B, 0x37, 0x71, 0x1B, 0x5B, 0x38, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x35, 0x71, 0x1B, 0x5B, 0x34, 0x71, 0x1B, 0x5B, 0x39, 0x71, 0x0D };
        public static readonly byte[] Ask = new byte[1] { 0x06 };

        public bool ScannerFound { get; set; }
        public readonly SerialPort SerialPort = new SerialPort(); //ком порт для работы со сканером
        private const int MAIN_ERROR_CODE = 8000;

        /// <summary>
        /// применяет текущие настройки и
        /// запускает чтение компорта
        /// </summary> 
        public bool StartReadScannerPort(
            string SerialPort232Name,
            int SerialPort232BaudRate,
            Parity SerialPort232Parity,
            int SerialPort232DataBits,
            StopBits SerialPort232StopBits,
            Handshake SerialPort232Handshake
            )
        {
            try
            {
                if (SerialPort.IsOpen)
                    return false;

                SerialPort.PortName = SerialPort232Name;
                SerialPort.BaudRate = SerialPort232BaudRate;
                SerialPort.Parity = SerialPort232Parity;
                SerialPort.DataBits = SerialPort232DataBits;
                SerialPort.StopBits = SerialPort232StopBits;
                SerialPort.Handshake = SerialPort232Handshake;
                SerialPort.RtsEnable = true;
                SerialPort.ReceivedBytesThreshold = 4;
                

                SerialPort.Open();
                ScannerFound = false;

                if (!SerialPort.IsOpen)
                    throw new Exception("Критический сбой компонентов.Работа с COM портом невозможна!");

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                return false;
            }
            return true;
        }
    }
}
