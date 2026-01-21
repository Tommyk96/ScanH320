using PharmaLegaсy.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;

namespace BoxAgr.Configure
{
#pragma warning disable CA1031 // Не перехватывать исключения общих типов
    [DataContract]
    public class Config : INotifyPropertyChanged, IConfig
    {
        [DefaultValue(4)]
        [DataMember]
        public int LogSizeInMonth { get; set; }

        [DefaultValue(51232)]
        [DataMember]
        public int scannerServerPort { get; set; }

        [DefaultValue("172.16.10.141")]
        [DataMember]
        public string PlcIp { get; set; }

        [DataMember]
        public uint LicKey { get; set; }

        [DataMember]
        public bool AuthorizationEnable { get; set; } = true;

        [DataMember]
        public bool PalletAggrgationEnable { get; set; } = true;

        [DataMember]
        public uint Mode { get; set; } = 0;
        //0 - для работы без разделителя.
        //2 - для конвейра с двумя толкателями
        //3 - для включения возможности ручной упаковки

        

        #region COM
        [DefaultValue("COM1")]
        [DataMember]
        public string SerialPort232Name { get; set; }

        [DefaultValue(115200)]
        [DataMember]
        public int SerialPort232BaudRate { get; set; }

        [DefaultValue(System.IO.Ports.Parity.None)]
        [DataMember]
        public global::System.IO.Ports.Parity SerialPort232Parity { get; set; }

        [DefaultValue(8)]
        [DataMember]
        public int SerialPort232DataBits { get; set; }

        [DefaultValue(System.IO.Ports.StopBits.One)]
        [DataMember]
        public global::System.IO.Ports.StopBits SerialPort232StopBits { get; set; }

        [DefaultValue(System.IO.Ports.Handshake.None)]
        [DataMember]
        public global::System.IO.Ports.Handshake SerialPort232Handshake { get; set; }

        [DefaultValue("COM2")]
        [DataMember]
        public string HandSerialPort232Name { get; set; }

        [DefaultValue(115200)]
        [DataMember]
        public int HandSerialPort232BaudRate { get; set; }

        [DefaultValue(System.IO.Ports.Parity.None)]
        [DataMember]
        public global::System.IO.Ports.Parity HandSerialPort232Parity { get; set; }

        [DefaultValue(8)]
        [DataMember]
        public int HandSerialPort232DataBits { get; set; }

        [DefaultValue(System.IO.Ports.StopBits.One)]
        [DataMember]
        public global::System.IO.Ports.StopBits HandSerialPort232StopBits { get; set; }

        [DefaultValue(System.IO.Ports.Handshake.None)]
        [DataMember]
        public global::System.IO.Ports.Handshake HandSerialPort232Handshake { get; set; }
#endregion

        [DefaultValue(31)]
        [DataMember]
        public int MaxPackInOneScanMode { get; set; }


        [DefaultValue(90)]
        [DataMember]
        public int BoxFullPercentToStop { get; set; }

        [DefaultValue(false)]
        [DataMember]
        public bool AutoVerifyBox { get; set; } = false;

        [DefaultValue(false)]
        [DataMember]
        public bool PrintLabelBox { get; set; } = false;

        [DefaultValue(false)]
        [DataMember]
        public bool IgnoreRepitNumbers { get; set; } = false;

        [DefaultValue(100)]
        [DataMember]
        public int WindowTimeOut { get; set; }

        [DefaultValue(2)]
        [DataMember]
        public int MinBoxNumberBeforeWarning { get; set; }


        [DefaultValue(20)]
        [DataMember]
        public int GetMoreBoxNumberCount { get; set; }
        

       

        [DefaultValue(4012)]
        [DataMember]
        public int VerifyScannerServerPort { get; set; } = 4012;

        [DefaultValue(1)]
        [DataMember]
        public int LineNum { get; set; }

        #region MES
        [DefaultValue("http://*:7081")]
        [DataMember]
        public string LocalWebSrvUrl { get; set; }

        [DefaultValue("http://172.16.10.33:7090/report")]
        [DataMember]
        public string Srv1CUrl { get; set; }

        [DefaultValue("http://172.16.10.33:7090/jobs/Authorization")]
        [DataMember]
        public string Srv1CUrlAuthorize { get; set; }

        [DefaultValue("l2")]
        [DataMember]
        public string Srv1CLogin { get; set; }

        [DefaultValue("111")]
        [DataMember]
        public string Srv1CPass { get; set; }

        [DefaultValue(10000)]
        [Description("Таймаут ожидания соединения и ответа при запросах к серверу EMS")]
        [DataMember]
        public int Srv1CRequestTimeout { get; set; }
        #endregion

        #region UNC
        [Category("Unc")]
        [Description("Unc путь к сервису L3 для передачи данных")]
        [DefaultValue(@"\UncReports")]
        [DataMember]
        public string ReportUncDir { get; set; }
        
        [Category("Unc")]
        [DefaultValue(false)]
        [Description("Использовать Unc передачу вместо Web или FTP")]
        [DataMember]
        public bool UseReportUncDir { get; set; }

        #endregion

        #region FTP
        [Category("Ftp")]
        [Description("Ftp путь к сервису L3 для передачи данных")]
        [DefaultValue(@"ftp.host.local")]
        [DataMember]
        public string ReportServerFtp { get; set; }
        [Category("Ftp")]
        [DefaultValue("/report/")]
        [Description("Порт сервера ftp L3")]
        [DataMember]
        public string ReportServerFtpDir { get; set; }
        [Category("Сервер L3")]
        [DefaultValue(false)]
        [Description("Использовать Ftp передачу вместо Web")]
        [DataMember]
        public bool UseReportServerFtp { get; set; }
        #endregion

        [DefaultValue("172.16.10.198")]
        [DataMember]
        public string BoxPrinterIp { get; set; }

        [DefaultValue(false)]
        [DataMember]
        public bool UsePalletPrinter { get; set; } = false;

        [DefaultValue("TSC")]
        [DataMember]
        public string BoxPrinterType { get; set; } = string.Empty;


        [DefaultValue(9100)]
        [DataMember]
        public int BoxPrinterPort { get; set; }


        [DefaultValue("172.16.10.33")]
        [DataMember]
        public string ModBusSlaveIp { get; set; }

        [DefaultValue("172.16.10.33")]
        [DataMember]
        public string PrinterPalleteIp { get; set; }

        [DefaultValue(9100)]
        [DataMember]
        public int PrinterPalletPort { get; set; }

        [DefaultValue(false)]
        [DataMember]
        public bool PacketLogEnable { get; set; }

        [DefaultValue("\\scImg")]
        [DataMember]
        public string ScanImgPath { get; set; } = string.Empty;

        [DefaultValue(false)]
        [DataMember]
        public bool ScanImgShow { get; set; }

        [DefaultValue(4000)]
        [DataMember]
        public int ScanImgTimeout { get; set; }

        [DefaultValue(0)]
        [DataMember]
        public int Angle { get; set; }
        #region Параметры определяемые с окна settings
        int pusherTimeLatensy;
        [Description("Время задержки срабатывания толкателя относительно времени прохода объектом датчика толкателя. Задается в милисекундах"), Category("Настройки толкателя")]
        [DefaultValue(500)]
        [DataMember]
        public int PusherTimeLatensy
        {
            get { return pusherTimeLatensy; }
            set { SetProperty(ref pusherTimeLatensy, value); }
        }

        int pusherImpulseTime;
        [Description("Длинна импульса толкателя в милисекундах."), Category("Настройки толкателя")]
        [DefaultValue(300)]
        [DataMember]
        public int PusherImpulseTime
        {
            get { return pusherImpulseTime; }
            set { SetProperty(ref pusherImpulseTime, value); }
        }


        bool sendRquestNewBoxNum ;
        [DefaultValue(false)]
        [DataMember]
        public bool SendRquestNewBoxNum
        {
            get { return sendRquestNewBoxNum; }
            set { SetProperty(ref sendRquestNewBoxNum, value); }
        }

        bool aggregateOn;
        [DefaultValue(false)]
        [DataMember]
        public bool AggregateOn
        {
            get { return aggregateOn; }
            set { SetProperty(ref aggregateOn, value); }
        }

        int numItemInPack;
        [DefaultValue(6)]
        [DataMember]
        public int NumItemInPack
        {
            get { return numItemInPack; }
            set { SetProperty(ref numItemInPack, value); }
        }


        int pusherQueueSize;
        public int PusherQueueSize
        {
            get { return pusherQueueSize; }
            set { SetProperty(ref pusherQueueSize, value); }
        }

        int manufactureDateShiftInHours;
        [DefaultValue(24)]
        [DataMember]
        public int ManufactureDateShiftInHours
        {
            get { return manufactureDateShiftInHours; }
            set { SetProperty(ref manufactureDateShiftInHours, value); }
        }

        bool palletAutoCreate;
        [DataMember]
        public bool PalletAutoCreate
        {
            get { return palletAutoCreate; }
            set { SetProperty(ref palletAutoCreate, value); }
        }

        bool stopLineAfterPalletFull;
        [DataMember]
        public bool StopLineAfterPalletFull
        {
            get { return stopLineAfterPalletFull; }
            set { SetProperty(ref stopLineAfterPalletFull, value); }
        }

        int numRows;
        [DefaultValue(2)]
        [DataMember]
        public int NumRows
        {
            get { return numRows; }
            set { SetProperty(ref numRows, value); }
        }

        int numColumns;
        [DefaultValue(2)]
        [DataMember]
        public int NumColumns
        {
            get { return numColumns; }
            set { SetProperty(ref numColumns, value); }
        }

        int boxHeight;
        [DefaultValue(1080)]
        [DataMember]
        public int BoxHeight
        {
            get { return boxHeight; }
            set { SetProperty(ref boxHeight, value); }
        }

        int boxWidth;
        [DefaultValue(1920)]
        [DataMember]
        public int BoxWidth
        {
            get { return boxWidth; }
            set { SetProperty(ref boxWidth, value); }
        }

        bool boxGrid;
        [DataMember]
        public bool BoxGrid
        {
            get { return boxGrid; }
            set { SetProperty(ref boxGrid, value); }
        }

        

        #endregion

        #region Func
        private static readonly string cfgFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + @"\H320.cfg";
        public static Config Load()
        {
            Config r = new();
            r.Reset();

            try
            {
                if (!System.IO.File.Exists(cfgFileName))
                {
                    r.Save();
                    return r;
                }

                using System.IO.TextReader tmpFile = new System.IO.StreamReader(cfgFileName);
                string s = tmpFile.ReadToEnd();
                tmpFile.Close();
                tmpFile.Dispose();

                Config bj = DeserializeJSON<Config>(s);
                if (bj != null)
                    return bj;
            }
            catch (SecurityException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("SecurityException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (InvalidOperationException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("InvalidOperationException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (ArgumentException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("ArgumentException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (Exception ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("Exception " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            return r;

        }
        public static string PrettyJson(string unPrettyJson)
        {
            var options = new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(unPrettyJson);

            return System.Text.Json.JsonSerializer.Serialize(jsonElement, options);
        }
        public void Save()
        {
            try
            {
                using System.IO.TextWriter tmpFile = new System.IO.StreamWriter(cfgFileName, false);
                using System.IO.MemoryStream stream = new();
                DataContractJsonSerializer ds = new(typeof(Config));
                DataContractJsonSerializerSettings s = new();
                ds.WriteObject(stream, this);
                string jsonString = Encoding.UTF8.GetString(stream.ToArray());
                jsonString = PrettyJson(jsonString);
                tmpFile.Write(jsonString);
                tmpFile.Close();
                tmpFile.Dispose();
            }
            catch (SecurityException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("SecurityException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (InvalidOperationException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("InvalidOperationException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (ArgumentException ex)
            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("ArgumentException " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
            catch (Exception ex)

            {
                System.IO.TextWriter logFile = new System.IO.StreamWriter("errorLog.txt", true);
                logFile.WriteLine("Exception " + ex.Message + "\n " + cfgFileName);
                logFile.Close();
            }
        }
        public Config()
        {

        }
        public void Reset()
        {
            //установить значения по умолчанию
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this))
            {
                DefaultValueAttribute myAttribute = (DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)];

                if (myAttribute != null)
                {
                    property.SetValue(this, myAttribute.Value);
                }
            }
        }
        public static T DeserializeJSON<T>(string json)
        {
            var instance = typeof(T);

            using var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(json));
            var deserializer = new DataContractJsonSerializer(instance);
            return (T)deserializer.ReadObject(ms);
        }
        #endregion;

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
          [CallerMemberName] string propertyName = "",
          Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            //InvokePropertyChanged(new PropertyChangedEventArgs(propertyName));
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

    }
#pragma warning restore CA1031 // Не перехватывать исключения общих типов
    public enum Mode
    {
        Default = 0,
        Selector = 1,
        HandAndAutoAgr=2
    }
}
