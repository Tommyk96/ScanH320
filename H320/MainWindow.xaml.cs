
using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Controllers.Interfaces;
using BoxAgr.BLL.Http;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.BLL.Models.Matrix;
using BoxAgr.Configure;
using BoxAgr.Pages;
using FSerialization;
using H320.BLL.Factories;
using H320.BLL.Interfaces;
using H320.BLL.Utilites;
using Peripherals;
using PharmaLegacy;
using PharmaLegacy.data;
using PharmaLegaсy.Interfaces;
using PharmaLegaсy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Util;

namespace BoxAgr
{
    public partial class MainWindow : Window, IMainFrame
    {
        //собития сканера
        public event CodeAddEventHandler? codeAddEventHandler;
        internal event EnterUserEventHandler? enterUserEventHandler;


        private bool topMost = true;
        public bool noPrint = false;
        private bool allPort = false;
        public bool Dbg { get; }
        private string msg2 = "";//если значение больше "" то выводится второе окно подтверждения



        public Autorization.User Master { get; set; } = new Autorization.User();
        public HandScannerMode HandScannerMode { get; set; } = HandScannerMode.Default; // 0= чтение коробок, 1= чтение пачки брака
        public string OperatorId { get; set; } = "";
        private string criticalMsg = "";


        private static Config Settings => App.Settings;
        private static LocalSystemState systemState => App.SystemState;
        private static JobController Job => App.Job;


        private ScannerRs232 leftScanner;
        private ScannerRs232 rightScanner;

        private readonly IBoxAssemblyController boxAssembly;
        private readonly IBusControl modBus; //IModBusController
        //private readonly MatrixController matrix;


        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer windowTimeOut = new System.Windows.Threading.DispatcherTimer();
        private int shutdownTimer = 70;

        private MainPageLuninec mainPageLun;
        private MainPageMatrix mainPageMatrix;
        private ImageBoxPage mainPageImageBoxPage;
        private BoxArgegateImagePage boxArgegateImagePage;

        private IMainPage mainPage;

        private SettingsPage settingsPage;


        private Aardwolf.HttpAsyncHost hs;
        private System.ComponentModel.BackgroundWorker webServerWorker = new System.ComponentModel.BackgroundWorker();


        public bool IncLayer = false;
        private bool closeAfterVerifyLastBox = false;//выставляется функцией BtnOrderClose_Click при завершении задания 


        private DateTime lastRedTime = DateTime.MinValue;
        private DateTime lastSoundTime = DateTime.MinValue;

        private readonly IPrinterDataStrategy _prnDataPrepare;

        #region Кисти для пузырьков сканеров
        private readonly
            LinearGradientBrush green1 = new LinearGradientBrush() {
                GradientStops = new GradientStopCollection(new GradientStop[2] {
                    new GradientStop((Color) ColorConverter.ConvertFromString("#FF0F6900"), 0),
                    new GradientStop((Color) ColorConverter.ConvertFromString("#FFA3FF87"), 1)}),
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(23, 43),
                EndPoint = new Point(23, 44),
                Transform = new MatrixTransform(30.2835, 0, 0, 30.2835, -680.90622, -1286.1606)
            };

        private readonly
             LinearGradientBrush green2 = new LinearGradientBrush()
             {
                 GradientStops = new GradientStopCollection(new GradientStop[2] {
                    new GradientStop((Color) System.Windows.Media.ColorConverter.ConvertFromString("#00FFFFFF"), 0),
                    new GradientStop((Color) System.Windows.Media.ColorConverter.ConvertFromString("#DFFFFFFF"), 1) }),
                 Transform = new MatrixTransform(24.162375, 0, 0, 18.685562, -538.24647, -790.03875),
                 MappingMode = BrushMappingMode.Absolute,
                 StartPoint = new Point(23, 44),
                 EndPoint = new Point(23, 43)
             };

        private readonly
              LinearGradientBrush red1 = new LinearGradientBrush()
              {

                  GradientStops = new GradientStopCollection(new GradientStop[2] {
            new GradientStop((Color) System.Windows.Media.ColorConverter.ConvertFromString("#FFCF0000"), 0),
            new GradientStop((Color) System.Windows.Media.ColorConverter.ConvertFromString("#FFFF8BA4"), 1)}),

                  MappingMode = BrushMappingMode.Absolute,
                  StartPoint = new Point(23, 43),
                  EndPoint = new Point(23, 44),
                  Transform = new MatrixTransform(30.2835, 0, 0, 30.2835, -680.9062, -1286.1606)
              };
        #endregion
        public MainWindow()
        {
            System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            System.Reflection.AssemblyName assemName = thisExe.GetName();
            systemState.ver = $"v {assemName.Version}. Mode: {Settings.Mode} ";

            Log.Write($"{TextConstants.WorkTypeUp} версия:" + assemName.Version?.ToString() + " запускается", EventLogEntryType.Information, 0);

            InitializeComponent();

            WindowState = WindowState.Maximized;

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

            ///
            string[] args = Environment.GetCommandLineArgs();
            //проверить параметры командной строки
            foreach (string s in args)
            {
                switch (s)
                {
                    case "-noPrint":
                        noPrint = true;
                        break;
                    case "-dbg":
                        WindowState = WindowState.Normal;
                        Dbg = true;
                        break;
                    case "-AllPorts":
                        allPort = true;
                        break;
                    case "-noTop":
                        topMost = false;
                        break;
                    case "-ShowImage":
                        Settings.BoxGrid = true;
                        break;
                    case "-HideImage":
                        Settings.BoxGrid = false;
                        break;
                }
            }

            Topmost = topMost;

            #region Кисти для пузырьков сканеров
            //LinearGradientBrush green1 = new LinearGradientBrush();

            //green1.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF0F6900"), 0));
            //green1.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFA3FF87"), 1));

            //green1.MappingMode = BrushMappingMode.Absolute;
            //green1.StartPoint = new Point(23, 43);
            //green1.EndPoint = new Point(23, 44);
            //green1.Transform = new MatrixTransform(30.2835, 0, 0, 30.2835, -680.90622, -1286.1606);


            //LinearGradientBrush green2 = new LinearGradientBrush();
            //green2.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FFFFFF"), 0));
            //green2.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#DFFFFFFF"), 1));
            //green2.Transform = new MatrixTransform(24.162375, 0, 0, 18.685562, -538.24647, -790.03875);
            //green2.MappingMode = BrushMappingMode.Absolute;
            //green2.StartPoint = new Point(23, 44);
            //green2.EndPoint = new Point(23, 43);


            //LinearGradientBrush red1 = new LinearGradientBrush();

            //red1.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCF0000"), 0));
            //red1.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFF8BA4"), 1));

            //red1.MappingMode = BrushMappingMode.Absolute;
            //red1.StartPoint = new Point(23, 43);
            //red1.EndPoint = new Point(23, 44);
            //red1.Transform = new MatrixTransform(30.2835, 0, 0, 30.2835, -680.9062, -1286.1606);


            LinearGradientBrush red2 = new LinearGradientBrush();

            red2.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FFFFFF"), 0));
            red2.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#DFFFFFFF"), 1));

            red2.MappingMode = BrushMappingMode.Absolute;
            red2.StartPoint = new Point(23, 44);
            red2.EndPoint = new Point(23, 43);
            red2.Transform = new MatrixTransform(24.162375, 0, 0, 18.685562, -538.24645, -790.03875);

            #endregion

            //создать стратегию преобразования полей 
            _prnDataPrepare = PrinterStrategiesFactory.CreatePrintDataStrategy(Settings.BoxPrinterType);

            //
            Log.ClearLogs(Settings.LogSizeInMonth);

            //запуск соединения с сканером на СОМ  порту
            leftScanner = new Peripherals.ScannerRs232();
            leftScanner.SerialPort.DataReceived += SerialPort_DataReceived;
            //если не удалось запустить сканер выставить аварийный стоп
            if (string.IsNullOrEmpty(Settings.SerialPort232Name) || (!leftScanner.StartReadScannerPort(
                Settings.SerialPort232Name,
                Settings.SerialPort232BaudRate,
                Settings.SerialPort232Parity,
                Settings.SerialPort232DataBits,
                Settings.SerialPort232StopBits,
                Settings.SerialPort232Handshake) && !allPort))
            {
                if (!string.IsNullOrEmpty(Settings.SerialPort232Name))
                {
                    systemState.StatusText = "Системная ошибка! " + Settings.SerialPort232Name;
                    systemState.StatusBackground = Brushes.Red;
                }
                //
                systemState.LeftScanLampColor1 = red1;
                systemState.LeftScanLampColor2 = red2;

                systemState.LeftScannerLamp = Brushes.Red;
            }
            else
            {
                systemState.LeftScanLampColor1 = green1;
                systemState.LeftScanLampColor2 = green2;
                systemState.LeftScannerLamp = Brushes.Green;
            }


            //запуск соединения с сканером на СОМ  порту
            rightScanner = new Peripherals.ScannerRs232();
            rightScanner.SerialPort.DataReceived += HandSerialPort_DataReceived;
            //если не удалось запустить сканер выставить аварийный стоп
            if (string.IsNullOrEmpty(Settings.HandSerialPort232Name) || (!rightScanner.StartReadScannerPort(
                Settings.HandSerialPort232Name,
                Settings.HandSerialPort232BaudRate,
                Settings.HandSerialPort232Parity,
                Settings.HandSerialPort232DataBits,
                Settings.HandSerialPort232StopBits,
                Settings.HandSerialPort232Handshake) && !allPort))
            {
                if (!string.IsNullOrEmpty(Settings.HandSerialPort232Name))
                {
                    systemState.StatusText = "Ручной сканер. Системная ошибка !" + Settings.HandSerialPort232Name;
                    systemState.StatusBackground = Brushes.Red;
                    systemState.RightScannerLamp = Brushes.Red;
                }
                systemState.RightScanLampColor1 = red1;
                systemState.RightScanLampColor2 = red2;

            }
            else
            {
                systemState.RightScannerLamp = Brushes.Green;

                systemState.RightScanLampColor1 = green1;
                systemState.RightScanLampColor2 = green2;
            }


            mainPageMatrix = new(this);
            mainPageImageBoxPage = new(this);
            mainPageLun = new(this);
            boxArgegateImagePage = new(this);

            mainPage = GetMainPage(!Settings.BoxGrid);


            //сервер модбаса
            //modBus = new ModBusController(systemState);


            if (Settings.AggregateOn)
            {
                //агрегация в короб

                //dio
                modBus = new SlaveModBusController(systemState, Settings.ModBusSlaveIp, 1, 51, 470);

                //bll
                boxAssembly = new BoxAssemblyModBusController(App.JsonDbContext, modBus, Job, systemState,
                    App.Current as App, Settings.scannerServerPort, 3,  !Settings.BoxGrid);
            }
            else {
                //****сериализация в коробке

                //dio
                modBus = new SlaveModBusKerchController(systemState, Settings.ModBusSlaveIp, 1, 51, 470);

                //bll
                boxAssembly = new BoxAssemblySerializeDioController(App.JsonDbContext, modBus, Job, systemState,
                   App.Current as App, Settings.scannerServerPort, 3, false/*!Settings.BoxGrid*/, Settings.PacketLogEnable);
            }

            modBus.PowerLoss += ModBus_PowerLoss;
            modBus.StatusChange += Device_StatusChange;
            modBus.Start(CancellationToken.None);


            boxAssembly.StatusChange += Device_StatusChange;
            boxAssembly.AddLayer += mainPage.AddLayer;
            boxAssembly.Start();

           
            //
            DeskTop.Visibility = Visibility.Visible;
            MainFrame.Visibility = Visibility.Visible;
           

            DataContext = systemState;
            
          
           
            

            settingsPage = new SettingsPage(this);
            settingsPage.BackClick += SettingsPage_BackClick;

            
            if (Job.JobState == JobStates.InWork)
                Job.JobState = JobStates.Paused;

            if (Job.boxQueue == null)
                Job.boxQueue = new Queue<BoxWithLayers>();

            Job.OrderAcceptedEvent += Job_OrderAcceptedEvent;


            systemState.LayerCount = 0;
            systemState.StopLine = true;

            UpdateSystemState();


            switch (Job.JobState)
            {
                case JobStates.Empty:
                    BtnOrderStart.IsEnabled = false;
                    BtnOrderClose.IsEnabled = false;
                    break;
                case JobStates.Paused:
                case JobStates.InWork:
                case JobStates.New:
                    BtnOrderStart.IsEnabled = true;
                    BtnOrderClose.IsEnabled = false;
                    break;
                default:
                    BtnOrderStart.IsEnabled = false;
                    BtnOrderClose.IsEnabled = true;
                    break;
            }



            //проверить каталог пользователей
            if (!Autorization.UsersCatalog.CheckCatalog())
            {
                Log.Write("Не возможно создать каталог пользователей. Не возможно продолжить работу программы.");
                return;
            }

            systemState.Packer = "Мастер:";
            ShowRightBoxAwaitVerify(false);
            MainFrame.Navigate(mainPage);

            StartWebService();
           
           


          
            systemState.PropertyChanged += SystemState_PropertyChanged;

            //Properties.Settings.Default.Save();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            systemState.CurTime = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

            //определяем режим для совсместимости
            systemState.CurentMode = WorckMode.Left;

            //настроить таймер бездействия в окнах
            windowTimeOut.Tick +=  WindowTimeOut_Tick;
            windowTimeOut.Interval = new TimeSpan(0, 0, Settings.WindowTimeOut);

          

            //если есть задание то вывести список коробов
            if (Job?.readyBoxes.Count > 0)
            {

                //обновить дланные по коробам
                mainPage.UpdateBoxView();
            }

            if (Job?.order1C != null)
            {
                systemState.PackInBox = Job.numРacksInBox;
                systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //

                systemState.ReadyPalleteCount = Job.readyPallets.Count(x => x.state == NumberState.Верифицирован);
                systemState.BoxNotInPallete = Job.readyBoxes.Count(x => x.state == NumberState.Верифицирован);

            }
            
            Log.Write($"{TextConstants.WorkTypeUp} запущена",EventLogEntryType.Information, 0);
            #region Dbg
            /*
            */
            #endregion
        }

        //возвращает главное окно в зависимости от выбранного режима
        private IMainPage GetMainPage(bool showImage)
        {
            //если включена агрегация 
            if (Settings.AggregateOn && showImage)
                return boxArgegateImagePage;
            else if (Settings.AggregateOn)
                return mainPageMatrix;

            if (showImage)
                return mainPageImageBoxPage;
            else
                return mainPageLun;
        }

        public void SelectMainPage(bool showImage)
        {
            if (showImage == Settings.BoxGrid)
                return;

            //если есть задание запретить смену режима
            if (Job.JobState != JobStates.Empty && Job.JobState != JobStates.New)
            {
                ShowMessage("Невозможно изменить режим работы пока есть задание в работе.\nЗавершите задание и повторите попытку",
                    EventLogEntryType.Error);
                return;
            }

           

            boxAssembly.AddLayer -= mainPage.AddLayer;
            mainPage = GetMainPage(showImage);

            boxAssembly.AddLayer += mainPage.AddLayer;
            MainFrame.Navigate(mainPage);

            Settings.BoxGrid = showImage;
            Settings.Save();
        }
        private void ModBus_PowerLoss(object sender, bool state)
        {
            Dispatcher.Invoke(() =>
            {
                if (state)
                    systemState.PowerOff = true;
                else
                {
                    systemState.PowerOff = false;
                    shutdownTimer = 70;
                    if (systemState.StatusText.Contains("Потеря питания!"))
                    {
                        systemState.StatusText = "";
                        systemState.StatusBackground = Brushes.Transparent;
                    }

                }
            });

            
        }

        private readonly object updateDeviceStatus = new();
        private void Device_StatusChange(int id, PeripheralsType type, SessionStates data)
        {

            switch (id)
            {
                case 1:
                    //systemState.CamConnect = false;
                   // systemState.CamOnline = false;
                    systemState.CamConnect = data == SessionStates.OnLine ? true : false;
                    //обновить состояние в панеле управления
                    Dispatcher.Invoke(() => { settingsPage.CameraStatus = systemState.CamConnect ? "Подключён" : "Не подключён"; });
                    break;
                case 2:
                    systemState.ClientConnected = data == SessionStates.OnLine ? true : false;
                    //обновить состояние в панеле управления
                    Dispatcher.Invoke(() => { settingsPage.PlcStatus = systemState.ClientConnected ? "Подключён" : "Отключен"; });
                    break;
                default:

                    break;
            }

            //если оба канала подключены рисуем зеленый
            if (systemState.CamConnect )//&& systemState.ClientConnected)
            {
                //обновить состояние в панеле управления
                Dispatcher.Invoke(() => { 
                    systemState.LeftScannerLamp = Brushes.Green;
                    systemState.LeftScanLampColor1 = green1;
                    systemState.LeftScanLampColor2 = green2;
                });

            }else
                //обновить состояние в панеле управления
                Dispatcher.Invoke(() => { 
                    systemState.LeftScannerLamp = Brushes.Red;
                    systemState.LeftScanLampColor1 = red1;
                    systemState.LeftScanLampColor2 = green2;
                });

        }

        #region other func
        //таймерная процедура бездействия окон
        private void WindowTimeOut_Tick(object? sender, EventArgs e)
        {
            codeAddEventHandler?.Invoke(AddCodeType.Brack);
            windowTimeOut.Stop();
        }

        //часы а также проверка DI
        private void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            systemState.CurTime = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

            try
            {
                if (systemState.PowerOff)
                {
                    if (shutdownTimer < 61)
                    {
                        systemState.StatusText = "Потеря питания!\nВсе не завершенные операции не будут сохранены. Комплекс будет отключен через " + shutdownTimer + " сек.";

                        if (systemState.StatusBackground != Brushes.Red)
                            systemState.StatusBackground = Brushes.Red;
                    }
                    shutdownTimer--;

                    if (shutdownTimer == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Close();
                            System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233052)//0x8007007E)
                {
                    systemState.StatusText = "Критическая ошибка\n" + ex.Message;
                    systemState.StatusBackground = Brushes.Red;

                    //CriticalError()
                    Log.Write(ex.Message);
                }
            }
            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion

        /// <summary>
        /// Вот ето вот надо переделать!!!!!!!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "StatusBackground")
                {
                    if ((systemState.StatusBackground == Brushes.Red) || (systemState.StatusBackground == Brushes.DarkOrange))
                    {
                        //WermaLightAndSound(true, false, true, true, true, true);
                        SetTimesAction(IoActions.Red, IoActions.RemoveRed);
                        SetTimesAction(IoActions.Sound, IoActions.RemoveSound, 1000);
                    }
                    else if ((systemState.StatusBackground == Brushes.Green) || (systemState.StatusBackground == Brushes.Blue))
                    {
                        // WermaLightAndSound(false, true, false, true, true, true);
                    }

                    if (systemState.StatusBackground == Brushes.Blue)
                    {
                        if(Settings.PrintLabelBox)
                            Print();
                        //если стоит автоприемка . сразу принять короб
                        if (Settings.AutoVerifyBox)
                        {
                            
                            //проверить запущено ли задание
                            BoxWithLayers cBox = Job.boxQueue.Peek();
                            if (!ReleaseBox(WorckMode.None, cBox))
                                CriticalError($"Ошибка автоматической верификации короба {cBox.Number}\n Примите короб вручную!", false);
                        }

                    }


                }
                //обработка смены режима работы
                if (e.PropertyName == "CurentMode")
                {
                    if (systemState.CurentMode == WorckMode.None)
                    {
                        StopLine();
                        systemState.StatusText = "Линия была остановлена!";
                        if (boxAssembly.cBox != null)
                        {
                            if (boxAssembly.cBox.Numbers.Count > 0)
                                systemState.StatusText = "Линия была остановлена! Текущий короб обнулён!";
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Unknow;
                            boxAssembly.cBox.Numbers.Clear();
                            SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);

                            //обнулить окно
                            systemState.ListCurrentSerials?.Clear();
                            systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //
                        }

                        systemState.StatusBackground = Brushes.DarkOrange;
                    }
                    else if (systemState.CurentMode == WorckMode.Left)
                    {
                        if (boxAssembly.cBox?.Place == BoxWithLayersPlace.Unknow)
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Left;
                        else if (boxAssembly.cBox?.Place == BoxWithLayersPlace.Right)
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Left;
                    }
                    else if (systemState.CurentMode == WorckMode.Right)
                    {
                        if (boxAssembly.cBox?.Place == BoxWithLayersPlace.Unknow)
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Right;
                        else if (boxAssembly.cBox?.Place == BoxWithLayersPlace.Left)
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Right;
                    }
                    else if (systemState.CurentMode == WorckMode.Both)
                    {
                        if (boxAssembly.cBox?.Place == BoxWithLayersPlace.Unknow)
                            boxAssembly.cBox.Place = BoxWithLayersPlace.Right;
                    }
                }
            }
            catch { }
        }
        private static readonly SolidColorBrush BeltBachgroundColor = new(Color.FromRgb(101, 120, 134));
        public void ShowMessage(string msg, EventLogEntryType level, int msgId = 0, bool safeToLog = true)
        {
            try
            {
                if (string.IsNullOrEmpty(msg))
                    return;

                if (msgId == 0)
                    msgId = 101;

#pragma warning disable IDE0066 // Преобразовать оператор switch в выражение                   
                if (Dispatcher.CheckAccess())
                {
                    systemState.StatusText = msg;

                    switch (level)
                    {
                        case EventLogEntryType.Error:
                            systemState.StatusBackground = Brushes.Red;
                            break;
                        case EventLogEntryType.Warning:
                            systemState.StatusBackground = Brushes.Gold;
                            break;
                        default:
                            systemState.StatusBackground = BeltBachgroundColor;// Brushes. Brushes.Transparent;
                            break;
                    }

                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        systemState.StatusText = msg;
                        switch (level)
                        {
                            case EventLogEntryType.Error:
                                systemState.StatusBackground = Brushes.Red;
                                break;
                            case EventLogEntryType.Warning:
                                systemState.StatusBackground = Brushes.Gold;
                                break;
                            default:
                                systemState.StatusBackground = BeltBachgroundColor;// Brushes. Brushes.Transparent;
                                break;
                        }
                    });
#pragma warning restore IDE0066 // Преобразовать оператор switch в выражение
                }

                if (safeToLog)
                    Log.Write("MW", msg, level, msgId);
            }
            catch (Exception ex)
            {
                Log.Write("MW", $"Сбой отображения сообщения:{msg}\n Ошибка:{ex.Message}", level, msgId);
            }
        }
        public void ClearMsgBelt()
        {
            this.Dispatcher.Invoke(() =>
            {
                systemState.StatusText = "";
                systemState.StatusBackground = BeltBachgroundColor;
            });
        }
        private void ShowCriticalWarning()
        {
            PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Подтвердите что Вы удалили продукцию из текущего короба!", 
                PharmaLegacy.Windows.MessageBoxExButton.OK);
            if (!string.IsNullOrEmpty(msg2))
            {
                PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, msg2, PharmaLegacy.Windows.MessageBoxExButton.OK);
                msg2 = "";
            }
            systemState.CriticalError = false;
        }
        public void CriticalError(string msg, bool clearCurentBox, string _msg2 = "")
        {
            try
            {

                if (clearCurentBox)
                    systemState.CriticalError = true;
                else
                    systemState.CriticalError = false;

                systemState.StatusText = msg;
                systemState.StatusBackground = Brushes.Red;
                // });

                msg2 = _msg2;


                //отключить все окна если они были
                codeAddEventHandler?.Invoke(AddCodeType.CriticalError);
                //
                StopLine();

                //
                if (clearCurentBox)
                    ClearBox(false, true, true);

                criticalMsg = msg;

                Log.Write("MW",msg);
            }
            catch (Exception ex)
            {
                Log.Write("MW.",ex.Message, EventLogEntryType.Error, 10);
            }

          
        }

        #region Камера омрон
        private readonly object Sc_DataRecievedLock = new();
    
        public bool ProcessCode(GsLabelData ld, string data, out bool dropSequence)
        {
            dropSequence = false;
            try
            {
                lock (Sc_DataRecievedLock)
                {

                    if (systemState.CriticalError)
                    {

                        // Log.Write("Sc_DataRecieved. Программа заблокирована данные проигнорированы : " + data);
                        //!!!!!!!!!!!!!!!!!!!!!!
                        //после первого вызова функции SetTimesAction внутри блока  if (autoScannerSync.TryEnterWriteLock(500))
                        //все последующие вызовы autoScannerSync.TryEnterWriteLock генерят исключение о попытке рекурсивного установления лока на autoScannerSync
                        //как?!?! почему?!?! непонятно
                        SetTimesAction(IoActions.Red, IoActions.RemoveRed);
                        SetTimesAction(IoActions.Sound, IoActions.RemoveSound, 1000);
                        return false;
                    }

                    try
                    {

                        //неделать ничего если нет задания
                        if (Job.JobState == JobStates.Complited)
                        {
                            systemState.StatusText = "Нет задания для работы!";
                            systemState.StatusBackground = Brushes.Red;
                            return false;
                        }

                        //проверить если ли корректное задание?
                        if (Job.numРacksInBox < 1)
                        {
                            systemState.StatusText = "Нет задания для работы!";
                            systemState.StatusBackground = Brushes.Red;
                            return false;
                        }
                        //проверить запущено ли задание
                        if (Job.JobState != JobStates.InWork)
                        {
                            systemState.StatusText = $"{TextConstants.WorkTypeUp} не начата !";
                            systemState.StatusBackground = Brushes.Red;
                            return false;
                        }

                        //!!!!!!!!!!!!!!!!!
                        //проверить запушена ли линия c учетом счетчика останова
                        if (systemState.StopLine != false && systemState.StopCounter > 2)
                        {
                            //return;

                            if (boxAssembly.cBox.Numbers.Count > 0)
                                CriticalError("Получен код продукта при состоянии \"Стоп\"! Текущий короб №:" + boxAssembly.cBox.Number + " будет очишен принудительно!\nУберите из текущего короба всю продукцию", true);
                            else
                                CriticalError("Получен код продукта при состоянии \"Стоп\"! \nУберите  из текущего короба №:" + boxAssembly.cBox.Number + " всю продукцию", true);

                            return false;
                        }
                        else if (systemState.StopLine != false)
                            systemState.StopCounter++;


                        //проверить полон ли короб если он полон ничего не делать пока не закроют короб
                        if (boxAssembly.cBox?.NumbersCount == Job.numРacksInBox)
                        {
                            systemState.StatusText = "Верифицируйте предыдущий короб!";
                            systemState.StatusBackground = Brushes.DarkOrange;
                            return false;
                        }

                        //проверить соответствует ли присланный код текущему заданию 
                        //проверить GTIN итд

                        string result = Job.VerifyProductNum(ld);
                        if (!string.IsNullOrEmpty(result))// != "")
                        {
                            CriticalError($"Продукт не соответствует заданию! {result}", false);
                            // systemState.StatusText = $"Продукт не соответствует заданию! {result}";
                            // systemState.StatusBackground = Brushes.DarkOrange;
                            return false;
                        }
                        //пачка прошла проверку 
                        //проверить не последняя ли это пачка в коробе
                        //если последняя остановить линию

                        //проверить в текущих
                        //проверить в текущей коробке
                       
                        var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
                        string boxNum = r.boxNum;
                        if (r.IsExist)
                        {
                            //если включено игнорирование повторов ничего не делаем просто не обрабатываем код
                            if (Settings.IgnoreRepitNumbers)
                                return false;


                            #region Отключить сброс короба при повторе
                            //if (boxNum == boxAssembly.cBox.Number)
                            //    CriticalError($"Повтор серийного номера пачки {ld.SerialNumber}! Короб №: {boxAssembly.cBox.Number} удален!\nПодтвердите удаление короба с линии.", true);
                            //else
                            //    CriticalError($"Повтор серийного номера пачки {ld.SerialNumber}, она числится в коробе № {boxNum} ожидающего выпуска с линии. Удалите с линии текущий и собранный короба.", true, "Подтвердите, что вы удалили собранный короб № " + boxNum + "\nс линии.");

                            ////очистить найденный в очереди короб и объявить его браком
                            //job.WasteBox(boxNum);
                            //this.Dispatcher.Invoke(() =>
                            //{
                            //    if (job.boxQueue.Count > 0)
                            //    {
                            //        ShowRightBoxAwaitVerify(true);
                            //    }
                            //    else
                            //    {
                            //        ShowRightBoxAwaitVerify(false);
                            //    }
                            //});
                            #endregion

                            //CriticalError($"Повтор серийного номера  {ld.SerialNumber}!", false);
                            if (r.IsCurrentLayer)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    systemState.StatusText = $"Номер {ld.SerialNumber} уже присутствут в собираемом слое короба!";
                                    systemState.StatusBackground = Brushes.DarkOrange;
                                });
                            }
                            else if(r.IsAwaitVerify)
                                CriticalError($"Номер {ld.SerialNumber} уже присутствует в собранном коробе {r.boxNum} ожидающем верификации.", false);
                            else
                                CriticalError($"Номер {ld.SerialNumber} уже присутствует в слое {r.LayerNum} собираемого короба!", false);

                            return false;
                        }

                        //проверить в отбракованных
                        if (Job.IsAlreadyInBrack(ld.SerialNumber))
                        {
                            CriticalError($"Внимание  считан продукт с номером {ld.SerialNumber} который числится в браке!\nПодтвердите удаление продукта из короба.", false);
                            //systemState.StatusText = $"Cерийный номер {ld.SerialNumber} числится в браке!!";
                            //systemState.StatusBackground = Brushes.DarkOrange;
                            return false;
                        }

                        //проверить в уже верифицированных номерах
                        if (Job.IsAlreadyInProcessedBox(data, out boxNum))
                        {
                            #region Отключить сброс короба при повторе
                            //CriticalError("Повтор серийного номера пачки, она числиться в коробе № " + boxNum + " выпущенного с линии. Удалите с линии текущий короб и выпущенный короб со склада.", true, "Подтвердите, что вы удалили выпущенный короб № " + boxNum + "\nсо склада.");

                            //job.WasteBox(boxNum);
                            ////обновить список выпущенных коробов
                            //this.Dispatcher.Invoke(() =>
                            //{
                            //    //обновить дланные по коробам
                            //    systemState.ProcessedBoxes.Clear();

                            //    ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                            //    foreach (PartAggSrvBoxNumber itm in job.readyBoxes)
                            //    {
                            //        if (itm.state == NumberState.Верифицирован || itm.state == NumberState.VerifyAndPlaceToReport)
                            //            pbView.Add(new SerialCode(itm.boxNumber));
                            //    }

                            //    //получить количество обработанных коробов для вывода на екран
                            //    systemState.ReadyBoxCount = pbView.Count;
                            //    systemState.ProcessedBoxes = pbView;
                            //    mainPage.lvBox.ScrollIntoView(pbView.LastOrDefault());
                            //});

                            //job.SaveOrder();
                            #endregion

                            CriticalError($"Повтор серийного номера продукта {ld.SerialNumber}, он числиться в коробе № " + boxNum + " выпущенном с линии.", false);
                            //systemState.StatusText = $"Повтор серийного номера продукта {ld.SerialNumber}, он числиться в коробе № " + boxNum + " выпущенном с линии.";
                            //systemState.StatusBackground = Brushes.DarkOrange;
                            return false;
                            ;
                        }

                        //проверить по списку разрешенных в серии
                        if (Job.order1C?.productNumbers?.Count > 0)
                        {
                            if (!Job.order1C.productNumbers.Exists(x => x == ld.SerialNumber))
                            {
                                 systemState.StatusText = $"Отбракован  продукт с номером " + ld.SerialNumber + " который не числится в серии!";
                                systemState.StatusBackground = Brushes.DarkOrange;
                                return false;
                            }
                        }

                        if (Settings.BoxGrid)
                        {
                            boxAssembly.AddSingleCodeToLayer(data);
                        }
                        else
                        {
                            //boxAssembly.cBox.Numbers.Add(new Unit() { Number = ld.SerialNumber, Barcode = data });

                            if(!boxAssembly.AddSingleCodeToLayer(data))
                                return false;

                            return true;
                            #region Old
                            this.Dispatcher.Invoke(() =>
                            {    //
                                if (systemState.ListCurrentSerials != null)
                                {

                                    //systemState.ListCurrentSerials.Add(new SerialCode(ld.SerialNumber));

                                    systemState.ListCurrentSerials.Insert(0, new SerialCode(ld.SerialNumber));
                                    while (systemState.ListCurrentSerials.Count > 10)
                                        systemState.ListCurrentSerials.RemoveAt(systemState.ListCurrentSerials.Count - 1);


                                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //

                                }
                                else
                                {
                                    ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                                    foreach (Unit itm in boxAssembly.cBox.Numbers)
                                    {
                                        pbView.Add(new SerialCode(itm.Number));
                                    }
                                    systemState.ListCurrentSerials = pbView;
                                }
                            });

                            //проверка на верификацию предыдущего короба
                            if ((Job.boxQueue.Count > 0) && (boxAssembly.cBox.Numbers.Count >= (Job.numРacksInBox - Settings.BoxFullPercentToStop)) && (systemState.CurentMode == WorckMode.Both))
                            {
                                BoxWithLayers b = Job.boxQueue.Peek();
                                //
                                systemState.StatusText = "Работа остановлена, считайте код выпускаемого короба №: " + b.Number;//"Линия остановлена. Верифицируйте или откажитесь от короба №:" + job.boxQueue.Peek().Number;
                                systemState.StatusBackground = Brushes.Red;
                                StopLine();
                            }
                            else if ((Job.boxQueue.Count > 0) && (boxAssembly.cBox.Numbers.Count >= Settings.MaxPackInOneScanMode)
                               && ((systemState.CurentMode == WorckMode.Left) || (systemState.CurentMode == WorckMode.Right)))
                            {
                                BoxWithLayers b = Job.boxQueue.Peek();
                                systemState.StatusText = "Работа остановлена, считайте код выпускаемого короба №: " + b.Number;//"Линия остановлена. Верифицируйте или откажитесь от короба №:" + job.boxQueue.Peek().Number;
                                systemState.StatusBackground = Brushes.Red;
                                StopLine();
                            }

                            //если короб набран закрыть его
                            if (boxAssembly.cBox.NumbersCount == Job.numРacksInBox)
                            {
                                //скрыть все окна если они на экране 
                                //codeAddEventHandler?.Invoke(AddCodeType.Uncknow);

                                //если определен ключ игнорировать повторы то передаем сигнал сбросить пакет
                                //если включено игнорирование повторов ничего не делаем просто не обрабатываем код
                                if (Settings.IgnoreRepitNumbers)
                                    dropSequence = true;

                                //сохранить номер короба для последующего вывода в сообщении
                                string safeBoxNum = boxAssembly.cBox.Number;
                                var lastPackNum = boxAssembly.cBox.Numbers.Last();
                                //job.boxQueue.Enqueue(boxAssembly.cBox.Clone());
                                AddBoxToQueue(boxAssembly.cBox.Clone(),false);
                                boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
                                Job.SaveOrder();

                                //запросить новый номер короба
                                int ost;
                                boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
                                //проверить если достигнута уставка запроса номеров запустить окно запроса
                                GetMoreBoxNumber(ost, true);

                                systemState.LayerCount = 1;
                                IncLayer = false;


                                systemState.ReadCodeCount = 0;
                                this.Dispatcher.Invoke(() =>
                                {

                                    if (boxAssembly.cBox != null)
                                        systemState.BoxNumber = boxAssembly.cBox?.Number;
                                    else
                                        systemState.BoxNumber = "";


                                    ShowRightBoxAwaitVerify(true);
                                    //если все коды прошли проверку добавить слой в отработанные
                                    systemState.StatusText = "Короб №: " + safeBoxNum + " успешно агрегирован. Верифицируйте короб.";
                                    systemState.StatusBackground = Brushes.Blue;

                                    systemState.ListCurrentSerials?.Clear();
                                    if (boxAssembly.cBox == null)
                                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", "0", systemState.PackInBox);
                                    else
                                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление";
                                                                                                                                                                                                 //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;

                                });


                            }
                            #endregion
                        }
                        return true;
                        
                    }
                    catch (Exception ex) { Log.Write(ex.ToString()); }
                    finally
                    {
                    }
                }
            }
            catch (LockRecursionException lr)
            {
                Log.Write(lr.Message);
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
            }

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        public void CloseFullBox(bool autoVerify)
        {
            
            //сохранить номер короба для последующего вывода в сообщении
            string safeBoxNum = boxAssembly.cBox.Number;
            var lastPackNum = boxAssembly.cBox.Numbers.Last();

            AddBoxToQueue(boxAssembly.cBox.Clone(), Settings.AutoVerifyBox);
            boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
            Job.SaveOrder();

            //запросить новый номер короба
            int ost;
            boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
            

            //проверить если достигнута уставка запроса номеров запустить окно запроса
            GetMoreBoxNumber(ost, true);

            systemState.LayerCount = 1;
            IncLayer = false;


            systemState.ReadCodeCount = 0;
            this.Dispatcher.Invoke(() =>
            {

                if (boxAssembly.cBox != null)
                    systemState.BoxNumber = boxAssembly.cBox?.Number;
                else
                    systemState.BoxNumber = "";

                //запросить верификацию этикетки если надо
                //if (!autoVerify)
                //{
                    ShowRightBoxAwaitVerify(true);
                    //если все коды прошли проверку добавить слой в отработанные
                    systemState.StatusText = "Короб №: " + safeBoxNum + " успешно агрегирован. Верифицируйте короб.";
                    systemState.StatusBackground = Brushes.Blue;
                


                systemState.ListCurrentSerials?.Clear();
                if (boxAssembly.cBox == null)
                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", "0", systemState.PackInBox);
                else
                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление";
                                                                                                                                                                             //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;

            });
        }
        public void ShowRightBoxAwaitVerify(bool show)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (show)
                        {
                            if (systemState.RightBoxAwaiVerifyVisiblility == Visibility.Visible)
                                return;

                            systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;
                            systemState.LastCodeInLastBox = Job.boxQueue?.Last().Numbers?.Last().Number;
                        }
                        else
                        {
                            if (systemState.RightBoxAwaiVerifyVisiblility == Visibility.Hidden)
                                return;

                            systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
                            systemState.LastCodeInLastBox = "";
                        }
                    }
                    catch (Exception ex) { Log.Write(ex.ToString()); }
                });
            }
            catch { }
        }
        #endregion
        public bool GetMoreBoxNumber(int ost, bool showWindow = false)
        {
            #region  Генерация номеров по маске 01{GTIN}11{дата производства}10{номер партии}37{кол-во едениц в коробе}21{сквозной счетчик в рамках партии 5 символов}

            if (Job.generateBoxNum)
            {
                //если номера еще есть игнорировать вызов 
                if (ost > 0)
                    return true;


                List<PartAggSrvBoxNumber> newNum = Job.GenerateNewBoxNum(20);
                Job.readyBoxes.AddRange(newNum);
                //если текущий короб пуст создать текущий короб
                if (boxAssembly.cBox == null)
                {
                    boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
                    //запросить новый номер короба
                    int ost1;
                    boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost1);
                }

                Job.SaveOrder();
                return true;
            }
            #endregion
            #region Запрос новых номеров коробов
            if (!App.Settings.SendRquestNewBoxNum)
                return false;

            System.ComponentModel.BackgroundWorker tmp1 = new System.ComponentModel.BackgroundWorker();
            tmp1.DoWork += delegate
            {
                try
                {
                    bool getNumberLoop = false;
                    this.Dispatcher.Invoke(() =>
                    {
                    Loop1:
                        if ((ost == Settings.MinBoxNumberBeforeWarning) || (getNumberLoop))
                        {
                            showWindow = false;
                            getNumberLoop = false;
                            //отключить таймаут для этого окна
                            windowTimeOut.Stop();
                            PharmaLegacy.Windows.GetMoreNumberWindow autWin = new PharmaLegacy.Windows.GetMoreNumberWindow(
                                Settings.Srv1CLogin,
                                Settings.Srv1CPass,
                                Settings.GetMoreBoxNumberCount,
                                Settings.Srv1CUrl + "/" + Job.id
                                       + "/AdditionalBoxNumbers/", ost,Settings.WindowTimeOut);

                            this.enterUserEventHandler += autWin.MainWindow_enterUserEventHandler;
                            autWin.Owner = this;
                            autWin.RequestComplitedEvent += AutWin_RequestComplitedEvent;
                            //запустить таймер контроля бездействия окна
                            //windowTimeOut.Start();

                            autWin.ShowDialog();

                            this.enterUserEventHandler -= autWin.MainWindow_enterUserEventHandler;
                            autWin.RequestComplitedEvent -= AutWin_RequestComplitedEvent;

                            windowTimeOut.Start();
                            //if (autWin.result == MessageBoxResult.Cancel)
                            //    return false;
                        }

                        if (ost == -1)//кончились номера
                        {
                            /*
                            string msg = "Номера закончились.Продолжение работы не возможно.";
                            if (job.boxQueue.Count > 0)
                                msg += "\nВерифицируйте собранный короб.";
                            msg += " Запросите еще номера или завершите задание.";
                            */
                            StopLine();

                            if (showWindow)
                            {
                                getNumberLoop = true;
                                goto Loop1;
                            }
                            //CriticalError(msg, false);
                        }


                    });
                }
                catch (Exception ex)
                {
                    Log.Write("получения доп номеров " + ex.Message, EventLogEntryType.Error, 53);
                }
            };
            tmp1.WorkerSupportsCancellation = true;
            tmp1.RunWorkerAsync();
            #endregion
            return false;
        }
        //сгенерировать 
        //вызывается если уудалось получить доп номера на короба
        private void AutWin_RequestComplitedEvent(BoxAdditionalNumbers state)
        {
            if (state == null)
            {
                systemState.StatusText = "Ошибка при расознавании ответа сервера.\nНе удалось получить доп номера коробов! Обратитесь к наладчику.";
                systemState.StatusBackground = Brushes.Red;
                return;
            }

            //добавить номера коробов
            foreach (string s in state.boxNumbers)
                Job.readyBoxes.Add(new FSerialization.PartAggSrvBoxNumber(s));

            //если текущий короб пуст создать текущий короб
            if (boxAssembly.cBox == null)
            {
                boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
                //запросить новый номер короба
                int ost;
                boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
            }

            Job.SaveOrder();

            systemState.StatusText = "В задание добавлено " + state.boxNumbers.Count.ToString(CultureInfo.InvariantCulture) + " " + Util.Helper.GetDeclension(state.boxNumbers.Count, "номер", "номера", "номеров") + " коробов.";
            systemState.StatusBackground = Brushes.Transparent;
        }

        public void AddBoxToQueue(BoxWithLayers box, bool autoVerify)
        {

            try
            {
                //обновить состояние в панеле управления
                Dispatcher.Invoke(() => {
                    Job.boxQueue.Enqueue(box);// boxAssembly.cBox.Clone());
                                              //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;

                    if (!autoVerify)
                        ShowRightBoxAwaitVerify(true);
                });

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, EventLogEntryType.Error, 509);
            }
        }

        #region Управление линией
        public bool StartLine(bool showWarninDialog = true)
        {
            try
            {
                Log.Write("D.MW." + Environment.CurrentManagedThreadId + ".10:Запушена процедура StartLine",EventLogEntryType.Error, 10);

                if ((systemState.CriticalError) && (showWarninDialog))
                    ShowCriticalWarning();// "", msg2);

                if (boxAssembly.cBox == null)
                {


                    //запросить новый номер короба
                    int ost;
                    boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
                    //проверить если достигнута уставка запроса номеров запустить окно запроса
                    GetMoreBoxNumber(ost, true);

                    // //проверить нет ли в задании короба ожидающего подтверждения
                    if (Job.boxQueue.Count > 0)
                    {
                        BoxWithLayers cBox1 = Job.boxQueue.Peek();
                        systemState.StatusText += "\nКороб №: " + cBox1.Number + " ожидает верификации.";
                    }

                    //если номер короба получен запустится
                    if (boxAssembly.cBox == null)
                    {
                        systemState.StatusText = $"{TextConstants.WorkTypeUp} не возможна! Все номера коробов в задании выработаны!\nЗапросите еще номера или завершите задание";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }
                }

                if (Job.JobState == JobStates.Empty)
                {
                    systemState.StatusText = "Работа не возможна! Нет задания на агрегацию !";
                    systemState.StatusBackground = Brushes.Red;
                    return false;
                }

                if (Job.JobState == JobStates.Closes)
                {
                    systemState.StatusText = "Работа не возможна!\nЗадание завершено и ожидает отправки! Нажмите кнопку \"Завершить агрегацию\"";
                    systemState.StatusBackground = Brushes.Red;
                    return false;
                }

                if (string.IsNullOrEmpty(boxAssembly.cBox?.Number))
                {
                    systemState.StatusText = $"Ошибка оператора: {TextConstants.WorkType} короба не начата !";
                    systemState.StatusBackground = Brushes.Red;
                    return false;
                }

                if (systemState.CurentMode == WorckMode.None)
                {
                    systemState.StatusText = "Не выбран режим работы !";
                    systemState.StatusBackground = Brushes.DarkOrange;
                    return false;
                }

                //проверка на верификацию предыдущего короба
                if ((Job.boxQueue.Count > 0) && (boxAssembly.cBox.Numbers.Count >= (Job.order1C.numPacksInBox * Settings.BoxFullPercentToStop)) && (systemState.CurentMode == WorckMode.Both))
                {
                    BoxWithLayers b = Job.boxQueue.Peek();
                    //
                    systemState.StatusText = "Работа остановлена, считайте код выпускаемого короба №: " + b.Number;//"Линия остановлена. Верифицируйте или откажитесь от короба №:" + job.boxQueue.Peek().Number;
                    systemState.StatusBackground = Brushes.Red;
                    StopLine();
                    return false;
                }
                else if ((Job.boxQueue.Count > 0) && (boxAssembly.cBox.Numbers.Count >= Settings.MaxPackInOneScanMode)
                   && ((systemState.CurentMode == WorckMode.Left) || (systemState.CurentMode == WorckMode.Right)))
                {
                    BoxWithLayers b = Job.boxQueue.Peek();
                    systemState.StatusText = "Работа остановлена, считайте код выпускаемого короба №: " + b.Number;//"Линия остановлена. Верифицируйте или откажитесь от короба №:" + job.boxQueue.Peek().Number;
                    systemState.StatusBackground = Brushes.Red;
                    StopLine();
                    return false;
                }
               
               
                //отпустить кнопку стоп
                mainPage.SetStop(false);

                ClearMsgPane();
                systemState.StopLine = false;
                //разрешить чтение сканера
                boxAssembly.ScanEnable = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("MW." + Environment.CurrentManagedThreadId + ".15:" + ex.Message,EventLogEntryType.Error, 10);
            }
            finally
            {
                Log.Write("D.MW." + Environment.CurrentManagedThreadId + ".10:Завершена процедура StartLine",EventLogEntryType.Error, 10);
            }
            return false;

        }
        public void StopLine()
        {

            this.Dispatcher.Invoke(() =>
            {
                systemState.StopLine = true;
                //выдать дискретный сигнал стопа
                SetAction(new IoAction[1] { IoActions.Stop });


                //вдавить кнопку стоп
                mainPage.SetStop(true);
                //остановить чтение сканера
                boxAssembly.ScanEnable = false;
                boxAssembly.StopCycle();
            });

        }

        #endregion

        #region управление дисктерными выходами на пк 
        public bool SetAction(IoAction[] actions)
        {

            return false;// ico300.SetAction(actions);


        }
        public bool SetTimesAction(IoAction a, IoAction b, int time = 3000)
        {
          

            // return true;
            #region Предотвращение множественного накопления звука и красной лампы. срабатывать не чаше чем time+200
            if (a == IoActions.Red && b == IoActions.RemoveRed && ((DateTime.Now - lastRedTime).TotalMilliseconds > time + 200)) lastRedTime = DateTime.Now;
            else if (a == IoActions.Sound && b == IoActions.RemoveSound && ((DateTime.Now - lastSoundTime).TotalMilliseconds > time + 200)) lastSoundTime = DateTime.Now;
            else if ((a == IoActions.Red && b == IoActions.RemoveRed) || (a == IoActions.Sound && b == IoActions.RemoveSound)) return true;
            #endregion


            using (System.ComponentModel.BackgroundWorker tmp1 = new System.ComponentModel.BackgroundWorker())
            {
                tmp1.DoWork += delegate
                {
                    Thread.CurrentThread.Name = "SetTimesAction";
                    try
                    {
                        Thread.Sleep(time);
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2146233052)
                        {
                            systemState.StatusText = "Критическая ошибка\n" + ex.Message;
                            systemState.StatusBackground = Brushes.Red;
                        }

                        Log.Write(ex.Message);
                    }
                };
                tmp1.WorkerSupportsCancellation = true;
                tmp1.RunWorkerAsync();
            }
            return true;
        }
        #endregion

        private object HandScannerLock = new();
        //обработка данных с ручного сканера ПРАВЫЙ СКАНЕР!!!!
        private void HandSerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;
            try
            {
                string indata = "";
                indata = sp.ReadTo("\r");
                //# для дебага не забыть убрать!!!!!!!!!
                //indata = sp.ReadTo("#");
               


                if (indata == null)
                    throw new Exception("Критический сбой компонентов COM.");

                indata = indata.Trim();
              
                 //во избежании застревания в буфере строк очистить его после чтения.
                sp.DiscardInBuffer();
                //handScanner.serialPort.DiscardOutBuffer();

                if (ProssedData(indata, WorckMode.Right))
                    ;// sp.Write(ScannerRs232.ScanResultGood, 0, ScannerRs232.ScanResultGood.Length);
                else
                {
                    // sp.Write(ScannerRs232.ScanResultBad, 0, ScannerRs232.ScanResultBad.Length);
                    this.Dispatcher.Invoke(() =>
                    {
                        modBus.StartRedBlink().ConfigureAwait(false);
                    } );
                }
               
                sp?.Write(ScannerRs232.Ask, 0, ScannerRs232.Ask.Length);

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
            }
        }
        //обработка данных с сканерного модуля грифон 
        //ЛЕВЫЙ СКАНЕР!
        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;
            try
            {
                string indata = "";
                indata = sp.ReadTo("\r");
                //# для дебага не забыть убрать!!!!!!!!!
                //indata = sp.ReadTo("#");

               
                if (indata == null)
                    throw new Exception("Критический сбой компонентов COM.");

                indata = indata.Trim();

                ProssedData(indata, WorckMode.Left);//, 1);

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
            }
        }
        //алгоритм обработки данных с ручных сканеров
        //работа в многопоточном режиме здесь нужна!!!
        //вызывается после чтения кодом любым из сканеров 
        public bool ProssedData(string indata, WorckMode scannerSide)
        {
            lock (HandScannerLock)
            {  
                bool ResetState = false;
                //работа в многопоточном режиме здесь нужна!!!
                try
                {
                    if (Dbg)
                        Log.Write($"HandScan {scannerSide}. Mode: {HandScannerMode}  Data: {indata}");

                    //*********************************
                    //проверить считанала ли карта пользователя
                    // если считана то выйти
                    if (ParseAuthCode(indata))
                        return false;

                  
                    Util.GsLabelData ld = new Util.GsLabelData(indata);

                    //
                    if (systemState.CriticalError)
                    {
                        systemState.StatusText = "Программа заблокировала ввод данных до подтверждения удаления всех едениц продукта из короба. Удалите все продукты и нажмите кнопку Старт для подтверждения!";
                        systemState.StatusBackground = Brushes.DarkOrange;

                        Log.Write("SerialPortRight_DataReceived. Программа заблокирована данные проигнорированы : " + indata);
                        SetTimesAction(IoActions.Red, IoActions.RemoveRed);
                        SetTimesAction(IoActions.Sound, IoActions.RemoveSound, 1000);
                        return false;
                    }


                    //верификация кода короба для номеров не SSCC18
                    if (Job.boxQueue.Count > 0 && Job.boxQueue.Peek() is BoxWithLayers cBox && cBox?.Number == indata)
                        HandScannerMode = HandScannerMode.VerifyBox;


                    //если состояние сканера не указано верифицировыть код
                    //и определить операцию
                    if (HandScannerMode == HandScannerMode.Default)// || (handScannerMode == HandScannerMode.Help))
                    {
                        if (ld.CryptoHash != null)
                        {
                            //////
                            //если выбран режим работы с ручным сканером
                            if (App.Settings.Mode == 3 &&
                                (systemState.CurentMode == WorckMode.Right || systemState.CurentMode == WorckMode.Both))
                            {
                                HandScannerMode = HandScannerMode.AddCodeToCurrentBox;
                            }
                            else
                            {
                                HandScannerMode = HandScannerMode.Help;
                            }
                            ResetState = true;
                        }
                        else if (ld.SerialShippingContainerCode00 != null || !string.IsNullOrEmpty(ld.SerialNumber))
                        {
                            //если ждет палета отдать приоритет ей
                            if (Job?.readyPallets.Exists(x => x.state == NumberState.Собирается) == true)
                                HandScannerMode = HandScannerMode.VerifyPallet;
                            else
                                HandScannerMode = HandScannerMode.VerifyBox;
                        }
                    }

                    //проверить запущено ли задание
                    if (HandScannerMode == HandScannerMode.VerifyBox && string.IsNullOrEmpty(boxAssembly.cBox?.Number)
                        && (Job?.boxQueue.Count == 0))
                    {
                        systemState.StatusText = "Нет собранного короба для верификации";// "$"{TextConstants.WorkTypeUp} короба не начата !";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }


                    switch (HandScannerMode)
                    {
                        case HandScannerMode.AddCodeToCurrentBox:
                            bool dropSequence;
                            return ProcessCode(ld, indata, out dropSequence);

                        case HandScannerMode.AddPack://добавить пачку в рабочий короб вместо отбракованной
                                                     //если в этом режиме прилетает номер короба попытатся его верифицировать
                            if (ld.SerialShippingContainerCode00 != null)
                            {
                                if (!VerifyBoxCode(indata, scannerSide))
                                    return false;
                                //на всяк вернуть режим
                                HandScannerMode = HandScannerMode.AddPack;
                                return true;
                            }

                            if (AddPack(indata))
                            {
                                HandScannerMode = HandScannerMode.Default;
                                return true;
                            }

                            break;
                        case HandScannerMode.VerifyBox:
                            ResetState = true;
                            return VerifyBoxCode(indata, scannerSide);
                        case HandScannerMode.VerifyPallet:
                            ResetState = true;
                            return VerifyPalletCode(indata);
                        case HandScannerMode.Brack:
                            if ((ld.SerialShippingContainerCode00 != null) ||
                                    (!string.IsNullOrEmpty(ld.SerialNumber) && string.IsNullOrEmpty(ld.CryptoHash)))
                            {
                                //если есть ведуший признак типа кода отрезать его
                                string bn = indata;
                                if (indata.Length > 3)
                                {
                                    if (indata.Substring(0, 3)?.Equals("]C1", StringComparison.Ordinal) == true)
                                        bn = bn.Remove(0, 3);
                                }
                                return BrackBox(bn);
                                // handScannerMode = HandScannerMode.Default;
                            }
                            else
                                return Brack(indata);
                            //if(Brack(indata))
                            //    handScannerMode = HandScannerMode.Default;
                            break;
                        case HandScannerMode.DropCodeFromCurrentBox:
                            return DeleteCodeFromCurrentBox(indata);
                            
                        case HandScannerMode.Sample:
                            ResetState = true;
                            return AddSample(indata);
                           
                        case HandScannerMode.Help:
                            return HelpFrom1c(indata);

                        case HandScannerMode.Reprint:
                           return ReprintAnyReleasedBox(indata);
                          
                    }

                  

                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message);
                }
                finally
                {
                    if (ResetState)
                        HandScannerMode = HandScannerMode.Default;
                }
                return false;
            }
        }
        private bool BrackBox(string boxNum)
        {
            //Dispatcher.Invoke(() =>
            //{

            //проверить в уже верифицированных номерах
            if (Job.IsProcessedBox(boxNum))
            {
                // CriticalError("Повтор серийного номера пачки, она числиться в коробе № " + boxNum + " выпущенного с линии. Удалите с линии текущий короб и выпущенный короб со склада.", true, "Подтвердите, что вы удалили выпущенный короб № " + boxNum + "\nсо склада.");

                Job.WasteBox(boxNum);

                systemState.StatusText = "Короб №: " + boxNum + "  удален из результата.\nВсе номера продукта из него снова доступны для агрегации.";
                systemState.StatusBackground = Brushes.Transparent;

                //обновить список выпущенных коробов
                this.Dispatcher.Invoke(() =>
                {
                    //обновить дланные по коробам
                    systemState.ProcessedBoxes?.Clear();

                    //ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                    //foreach (PartAggSrvBoxNumber itm in job.readyBoxes)
                    //{
                    //    if (itm.state == NumberState.Верифицирован || itm.state == NumberState.VerifyAndPlaceToReport)
                    //        pbView.Add(new SerialCode(itm.GS1SerialOrSSCC18)); //(itm.boxNumber));
                    //}

                    ////получить количество обработанных коробов для вывода на екран
                    //systemState.ReadyBoxCount = pbView.Count;
                    //systemState.ProcessedBoxes = pbView;
                    //mainPage.lvBox.ScrollIntoView(pbView.LastOrDefault());

                    //получить количество обработанных коробов для вывода на екран
                    systemState.ReadyBoxCount = Job.GetVerifyBoxCount();

                    systemState.ReadyProductCount = Job.GetVerifyProductCount();

                    //обновить список готовых коробов
                    mainPage.UpdateBoxView();
                });

                Job.SaveOrder();
                return true;
            }
            else
            {
                systemState.StatusText = "Невозможно отбраковать!\nКороб №: " + boxNum + " отсутствует в результатах";
                systemState.StatusBackground = Brushes.Red;
            }
            return false;
        }
        //вызов справки
        private bool HelpFrom1c(string fullNumber)
        {
            //codeAddEventHandler?.Invoke(AddCodeType.Help);

            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);
            //проверить на посторонний код
            if (ld.SerialShippingContainerCode00 == null && ld.SerialNumber == null)
            {
                systemState.StatusText = "Посторонний код";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить может это номер короба?
            if (ld.SerialShippingContainerCode00 != null)
            {
                string msg = Job.GetBoxInfo(fullNumber);

                systemState.StatusText = msg;
                systemState.StatusBackground = Brushes.Transparent;
                return false;
            }
            //проверить есть ли такой код в текущих коробах
            var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
            string boxNum = r.boxNum;
            if (r.IsExist)
            {
                //проверить в текущем боксе
                if (boxNum == boxAssembly.cBox.Number)
                    systemState.StatusText = $"Код {ld.SerialNumber} присутствует в слое {r.LayerNum} собираемого короба";// Номер: " + ld.SerialNumber + " доступен для агрегации";
                else
                    systemState.StatusText = $"Код {ld.SerialNumber} присутствует в собранном коробе";// Номер: " + ld.SerialNumber + " доступен для агрегации";

                systemState.StatusBackground = Brushes.Transparent;
                return true;
            }

            //проверить если ли такой код в задании
            CodeState cs = Job.CheckCodeWinthFullNum(fullNumber, out boxNum, false);
            if (cs == CodeState.New)
            {

                systemState.StatusText = "Код: " + ld.SerialNumber + $" доступен для {TextConstants.WorkTypePadeg}";
                systemState.StatusBackground = Brushes.Transparent;
                return true;
            }
            else
            {

                systemState.StatusText = "Неверный номер пачки!";

                if (cs == CodeState.Bad)
                    systemState.StatusText = "Код  " + ld.SerialNumber + " числится отбракованным!";

                if (cs == CodeState.Sample)
                    systemState.StatusText = "Код " + ld.SerialNumber + " числится отобранным как образец!";

                if (cs == CodeState.Verify)
                    systemState.StatusText = "Код " + ld.SerialNumber +" присутствует в выпущенном коробе №: " + boxNum;// "Номер присутствует в верифицированном коробе!";

                if (cs == CodeState.Missing)
                {
                    string msg = Job.VerifyProductNum(ld);
                    systemState.StatusText = "Код " + ld.SerialNumber +" отсутствует в задании!\n" + msg;
                }

                if (cs == CodeState.WrongLot)
                {
                    string msg = Job.VerifyProductNum(ld);
                    systemState.StatusText = "Продукт другой серии!";
                }

                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //scanDataEventHandler?.Invoke(data);
            /*
            Thread.Sleep(100);
            handScannerMode = MainWindow.HandScannerMode.AddPack;
            Dispatcher.Invoke(() =>
            {
                Windows.MessageBoxEx.ShowEx(this, "Номер: " + ld.SerialNumber + "  добавлен в брак.\nСчитайте ручным сканером код пачки для замены отбракованной", Windows.MessageBoxExButton.Cancel);
            });
            */

            //return false;
        }
        //обработка кода брака
        private bool Brack(string fullNumber)
        {
            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

            //проверить если ли корректное задание?
            if (Job.numРacksInBox < 1)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить запущено ли задание
            if (Job.JobState != JobStates.InWork)
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //проверить на сооответствие задание
            string msg = Job.VerifyProductNum(ld);
            if (!string.IsNullOrEmpty(msg))// != "")
            {
                systemState.StatusText = "Невозможно отбраковать!\n" + msg;
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            // bool ApendAfterRemove = false;
           
            CodeState cs = CodeState.Missing;
            //проверить есть ли такой номер в текущем слое
            //и запретить добавление если он не в текушем слое а например в предыдущем
            var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
            string boxNum = r.boxNum;
            if (r.IsExist)
            {
                //если код стоит в текущем слое то просто удалить его без процедуры замены
                if (r.IsCurrentLayer)
                {
                    boxAssembly.RemoveUnitFromLayer(fullNumber);
                    cs = CodeState.New;
                }
                else if (!r.IsAwaitVerify && !Settings.BoxGrid)
                {
                    //подготовка замены кода в верифицированном слое в режиме  БЕЗ сетки
                    cs = CodeState.New;
                    boxAssembly.RemoveUnitFromBox(fullNumber);
                }
                else
                {
                    //замена пачки в коробе ожидающем верификации этикетки
                    cs = CodeState.Verify;
                }
              
                Job.AppendBoxNum = boxNum;
                Job.DeletedPackNum = fullNumber;

                //boxAssembly.cBox.RemoveItem(ld.SerialNumber);
            }
            else
            {
                //если код не найден в рабочих коробах то искать его в выпущенных и предлагать заменить

                if (Job.IsAlreadyInProcessedBox(fullNumber, out boxNum))
                {
                    //если код найден то в режиме сериализации списать его
                    //удалить бракованный номер из короба
                    
                    if(!boxAssembly.cBox.RemoveItem(Job.DeletedPackNum))
                        if(Job.RemoveProduct(boxAssembly,fullNumber))
                        {
                            Job.AddDefectCode(ld.SerialNumber, OperatorId);
                            Job.SaveOrder();

                            systemState.StatusText = "Номер: " + ld.SerialNumber + "  удален из результата.";
                            systemState.StatusBackground = Brushes.Transparent;
                        }


                    Dispatcher.Invoke(() =>
                    {
                        UpdateSystemState();
                    });
                    //Job.AppendBoxNum = boxNum;
                    //Job.DeletedPackNum = fullNumber;

                    //cs = CodeState.Verify;

                    return true;
                }
                else
                {

                    //systemState.StatusText = "Невозможно отбраковать!\nПродукт №: " + ld.SerialNumber+" отсутствует в рабочих коробах!";
                    systemState.StatusText = "Невозможно отбраковать!\nПродукт №: " + ld.SerialNumber + " не выпускался!";
                    systemState.StatusBackground = Brushes.Red;
                    return false;
                }
            }


            if ((cs == CodeState.New) || (cs == CodeState.Verify))//if(ApendAfterRemove)
            {

                Job.AddDefectCode(ld.SerialNumber, OperatorId);


                // systemState.StatusText = "Номер: " + ld.SerialNumber + " помечен на списание в брак.\nОн будет списан после того как вы считаете сканером добавленный вместо него номер";
                systemState.StatusText = "Номер: " + ld.SerialNumber + "  удален из короба №: " + Job.AppendBoxNum + ".";
                systemState.StatusBackground = Brushes.Transparent;
                //job.SaveOrder();
                //SetTimesAction(IoActions.Green, IoActions.RemoveGreen);

                codeAddEventHandler?.Invoke(AddCodeType.Brack);

                if (cs == CodeState.Verify)
                {
                    //Thread.Sleep(100);
                    HandScannerMode = HandScannerMode.AddPack;

                    ////////////////
                    System.ComponentModel.BackgroundWorker _helpWindowWorker = new System.ComponentModel.BackgroundWorker();
                    _helpWindowWorker.DoWork += delegate
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                //переставить таймер
                                windowTimeOut.Stop();
                                //windowTimeOut.Start();
                                MessageBoxResult res;

                            Loop1:
                                HandScannerMode = HandScannerMode.AddPack;
                                res = MessageBoxResult.Cancel;
                                //"Номер: " + ld.SerialNumber + " помечен на удаление из короба №" + job.AppendBoxNum + ".\nОн будет списан после того как вы считаете сканером добавленный вместо него номер.\n\nСчитайте ручным сканером код пачки для замены отбракованной"
                                //"Номер: " + ld.SerialNumber + "  удален из короба №: " + job.AppendBoxNum + ".

                                //если номер текущего короба обновить окно
                                // if (job.AppendBoxNum != boxAssembly.cBox.Number)
                                res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Считайте сканером код пачки для замены отбракованной.",
                                    PharmaLegacy.Windows.MessageBoxExButton.LastBox, Dbg ,
                                    () => 
                                    {
                                        ProssedData(Helper.RandomMilkPackNum(Job.GTIN), WorckMode.Right);
                                    });
                                /* else
                                 {
                                     ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                                     foreach (LayerItem itm in boxAssembly.cBox.Numbers)
                                     {
                                         pbView.Add(new SerialCode(itm.number));
                                     }

                                     systemState.ListCurrentSerials.Clear();
                                     systemState.ListCurrentSerials = pbView;

                                     systemState.SerialInBoxCounter = "запустить обновление"; //

                                     res = Windows.MessageBoxEx.ShowEx(this, "Считайте сканером код пачки для замены отбракованной.", Windows.MessageBoxExButton.LastBox);
                                     //проверить количество номеров в коробе


                                 }*/

                             

                                //если выбрано закрытитие не полного короба
                                if (res == MessageBoxResult.Yes)
                                {
                                    //очистить панель сообшений
                                    ClearMsgPane();

                                    //останавливаем линию при нажатии кнопки последний короб
                                    StopLine();
                                    //проверяем что нет коробов в очереди на верификацию
                                    if (Job.boxQueue.Count > 0)
                                    {
                                        systemState.StatusText = "Перед созданием последнего короба\nнеобходимо верифицировать предыдущий короб!";
                                        systemState.StatusBackground = Brushes.Red;
                                        goto Loop1;
                                    }
                                    //проверить что в коробе после удаления будет хотябы одна пачка
                                    if (Job.GetPackCountInBox( boxAssembly, Job.AppendBoxNum) < 2)
                                    {
                                        //systemState.StatusText = "Нельзя завершить короб. Так как в нем больше нет пачек!";
                                        //systemState.StatusBackground = Brushes.Red;
                                        if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this,
                                            "Нельзя завершить короб. Так как в нем больше нет продуктов!\nХотите расформировать его?",
                                           PharmaLegacy.Windows.MessageBoxExButton.OKCancel) == MessageBoxResult.Cancel)
                                            goto Loop1;

                                        //расформировываем короб и пареходим в основное окно
                                        HandScannerMode = HandScannerMode.Default;
                                        ClearBoxAndMarkAsBrak(Job.AppendBoxNum, false);
                                        return;
                                    }

                                    Autorization.User user = new Autorization.User();
                                    if (UserAuth(true, false, false, true, user) == null)
                                        goto Loop1;

                                    HandScannerMode = HandScannerMode.Default;

                                    //удалить бракованный номер из короба
                                    boxAssembly.cBox.RemoveItem(Job.DeletedPackNum);

                                    CloseBox(true);
                                    Job.JobState = JobStates.Closes;
                                }

                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex.Message,EventLogEntryType.Error, 50);
                        }
                    };
                    _helpWindowWorker.WorkerSupportsCancellation = true;
                    _helpWindowWorker.RunWorkerAsync();
                }

                return true;
            }
            else
            {

                systemState.StatusText = "Неверный номер пачки!";

                if (cs == CodeState.Bad)
                    systemState.StatusText = "Номер уже добавлен в брак!";

                if (cs == CodeState.Sample)
                    systemState.StatusText = "Номер уже добавлен в образцы!";

                //if (cs == CodeState.Verify)
                //    systemState.StatusText = "Номер присутствует в верифицированном коробе!";

                if (cs == CodeState.Missing)
                    systemState.StatusText = "Номер отсутствует в задании! Не совпадает один или несколько параметров! ";

                systemState.StatusBackground = Brushes.Red;
                return false;
            }

        }
        private void ClearMsgPane()
        {
            Dispatcher.Invoke(() =>
            {
                systemState.StatusText = "";
                systemState.StatusBackground = Brushes.Transparent;
            });
        }
        //добавить сод в масив семплов
        private bool AddSample(string fullNumber)
        {

            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

            //проверить если ли корректное задание?
            if (Job.numРacksInBox < 1)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить запущено ли задание
            if (Job.JobState != JobStates.InWork)
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить есть ли такой номер в текущем слое
            //и запретить добавление если он есть
            if (boxAssembly.cBox.IsAlreadyInBox(ld.SerialNumber))
            {
                systemState.StatusText = "Нельзя взять образец из текущего слоя!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //проверить если ли такой код в задании
            CodeState cs = Job.CheckCode(fullNumber);
            if (cs == CodeState.New)
            {

                Job.AddSampleCode(ld.SerialNumber, OperatorId);

                systemState.StatusText = "Номер: " + ld.SerialNumber + "  добавлен в образцы";
                systemState.StatusBackground = Brushes.Transparent;
                Job.SaveOrder();
                codeAddEventHandler?.Invoke(AddCodeType.Sample);
                return true;
            }
            else
            {

                systemState.StatusText = "Неверный номер пачки!";

                if (cs == CodeState.Bad)
                    systemState.StatusText = "Номер уже добавлен в брак!";

                if (cs == CodeState.Sample)
                    systemState.StatusText = "Номер уже добавлен в образцы!";

                if (cs == CodeState.Verify)
                    systemState.StatusText = "Номер присутствует в верифицированном коробе!";

                systemState.StatusBackground = Brushes.Red;
                return false;
            }

        }
        //добавить пачку в  текущий короб взамен отбракованной
        private bool AddPack(string fullNumber)
        {
            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);
            string boxNum = "";

            //проверить если ли корректное задание?
            if (Job.numРacksInBox < 1)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить запущено ли задание
            if (Job.JobState != JobStates.InWork)
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить что это не тот же самый номер
            //проверить запущено ли задание
            if (Job.DeletedPackNum == fullNumber)
            {
                systemState.StatusText = "Нельзя заменить номер сам на себя!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //проверить номер пачки на повторение в текущем коробе
            #region ww
            /*   string boxNum = "";
               if (job.IsAlreadyInCurrentBoxes(ld.SerialNumber, out boxNum))
               {
                   if (boxNum == boxAssembly.cBox.Number)
                       CriticalError("Повтор серийного номера пачки! Короб №: " + boxAssembly.cBox.Number + " удален!\nПодтвердите удаление короба с линии.", true);
                   else
                       CriticalError("Повтор серийного номера пачки! Пачка числится в собраном коробе №: " + boxNum + "!\nТекуший короб будет очищен автоматически! Короб  №: " + boxNum + " удалите вручную!.", true);
                   //очистить найденный в очереди короб и объявить его браком
                   job.WasteBox(boxNum);
                   this.Dispatcher.Invoke(() =>
                   {
                       if (job.boxQueue.Count > 0)
                           systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;
                       else
                           systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
                   });
                   return;
               }

               //проверить в отбракованных
               if (job.IsAlreadyInBrack(ld.SerialNumber))
               {
                    return;
               }
            "Srv1CLogin": "hs_user",
     "Srv1CPass": "123456",
     "Srv1CRequestTimeout": 10000,
     "Srv1CUrl": "http://192.168.1.165/markirovka/hs/aglineex/jobs",
     "Srv1CUrlAuthorize": "",

               //проверить в уже верифицированных номерах
               if (job.IsAlreadyInProcessedBox(ld.SerialNumber, out boxNum))
               { }*/
            #endregion

            //проверить если ли такой код в задании
            CodeState cs = Job.CheckCode(fullNumber);

            var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
            boxNum = r.boxNum;
            if (r.IsExist)
                cs = CodeState.InWorck;

            if (cs == CodeState.New)
            {
                //убрать окно
                codeAddEventHandler?.Invoke(AddCodeType.Uncknow);

                //добавить номер в короб
                //job.AddNuberToBox(ld.SerialNumber, job.AppendBoxNum);
                bool boxAlreadyVerify;
                //удалить старый номер из короба
                if (!Job.ReplaceNumInBox(boxAssembly, Job.AppendBoxNum, Job.DeletedPackNum, ld.SerialNumber, fullNumber, out boxAlreadyVerify))
                {
                    systemState.StatusText = "Не удалось поместить в короб №: " + Job.AppendBoxNum + "\nпачку с номером " + ld.SerialNumber + " вместо пачки" + ld.SerialNumber;
                    systemState.StatusBackground = Brushes.Red;
                    return false;
                }
                HandScannerMode = HandScannerMode.Default;

                if (boxAlreadyVerify)
                {
                    systemState.StatusText = "Продукт №: " + ld.SerialNumber + " успешно добавлен в короб №: " + boxAssembly.cBox.Number;
                    systemState.StatusBackground = Brushes.Transparent;
                    Job.SaveOrder();
                    return true;
                }


                
                //SetTimesAction(IoActions.Green, IoActions.RemoveGreen);

                //job.AppendBoxNum = boxNum;
                //job.DeletedPackNum = ld.SerialNumber;

                //если это текущий короб обновить окно
                if (Job.AppendBoxNum == boxAssembly.cBox.Number)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();

                        //выбрать до 10 последних номеров в обратной последовательности
                        List<Unit> last10 = new();
                        last10.AddRange(boxAssembly.cBox.Numbers.Skip(Math.Max(0, boxAssembly.cBox.Numbers.Count - 10)));

                        foreach (Unit itm in last10)
                        {
                            pbView.Add(new SerialCode(itm.Number));
                        }

                        pbView.Reverse();

                        systemState.ListCurrentSerials?.Clear();
                        systemState.ListCurrentSerials = pbView;

                        //
                        // systemState.ListCurrentSerials.Insert(0, new SerialCode(ld.SerialNumber));
                        //while (systemState.ListCurrentSerials.Count > 10)
                        //    systemState.ListCurrentSerials.RemoveAt(systemState.ListCurrentSerials.Count - 1);


                        //systemState.SerialInBoxCounter = string.Format("Пачек {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox);
                        //

                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //

                        systemState.StatusText = "Продукт №: " + ld.SerialNumber + " успешно добавлен в короб №: " + boxAssembly.cBox.Number;
                        systemState.StatusBackground = Brushes.Transparent;

                    });
                    return true;
                }
                else
                {
                    ShowRightBoxAwaitVerify(true);
                }

                //job.AddDefectCode(ld.SerialNumber, OperatorId);
                //boxAssembly.cBox.productNumbers.Add(ld.SerialNumber);
                //ParceCodeArray(new List<string>() { ld.SerialNumber}, true);
                // if (AddSingleCodeToLayer(ld.SerialNumber, true))
                //{
                // systemState.StatusText = "Номер: " + ld.SerialNumber + "  добавлен в короб";
                // systemState.StatusBackground = Brushes.Transparent;
                //   job.SaveOrder();

                //   return true;
                //}
            }
            else
            {

                systemState.StatusText = "Неверный номер пачки!";

                if (cs == CodeState.Bad)
                    systemState.StatusText = "Номер уже добавлен в брак!";

                if (cs == CodeState.Sample)
                    systemState.StatusText = "Номер уже добавлен в образцы!";

                if (cs == CodeState.Verify)
                    systemState.StatusText = "Номер присутствует в верифицированном коробе!";

                if (cs == CodeState.InWorck)
                    systemState.StatusText = "Номер уже присутствует в собираемом коробе №: " + boxNum + "!";

                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            return false;

        }
        //добавить пачку в  текущий короб
        private bool VerifyBoxCode(string fullNumber, WorckMode scannerSide)
        {
            //  Util.GsLabelData ld = new Util.GsLabelData(fullNumber);
            if (Job.boxQueue.Count < 1)
            {
                systemState.StatusText = "нет короба для верификации !";
                systemState.StatusBackground = Brushes.Red;
                return true;
            }

            //проверить запущено ли задание
            BoxWithLayers cBox = Job.boxQueue.Peek();


            if (string.IsNullOrEmpty(cBox.Number))// == "")
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.DarkOrange;
                return false;
            }

            //выделить номер коробаудалив ведущиен ]C1
            string bn = fullNumber.Replace("]C1","");// Remove(0, 3);
            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

            //определить номер короба
            string inBoxNum = ld.SerialShippingContainerCode00 ?? fullNumber;
            //верифицировать номер коробки 
            if (cBox.Number != bn && cBox.Number != inBoxNum)
            {
                systemState.StatusText = "Неверный номер короба! Ожидается №:" + cBox.Number;
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //проверить количесвто продукта если ето не последний короб
            if ((cBox.NumbersCount != Job.numРacksInBox) && (!closeAfterVerifyLastBox) && (!cBox.CloseNotFull))
            {
                systemState.StatusText = "Короб не полный! Доложите упаковки или завершите агрегацию";
                systemState.StatusBackground = Brushes.DarkOrange;
                return false;
            }

            return ReleaseBox(scannerSide, cBox);
        }
        //Выпустить короб 
        private bool ReleaseBox(WorckMode scannerSide, BoxWithLayers cBox)
        {
            try
            {
 


                //если верифицируется не полный короб закрыть задание
                if ((cBox.NumbersCount != Job.order1C?.numPacksInBox) && (closeAfterVerifyLastBox))
                {
                    Job.JobState = JobStates.Complited;
                    closeAfterVerifyLastBox = false;

                    //добавить короб в обработанные
                    if (Master != null)
                        Job.AddVerifyBoxWithFullNum(cBox, Master.ID);
                    else
                        Job.AddVerifyBoxWithFullNum(cBox, "");

                    //удалить короб из очереди
                    Job.boxQueue.Dequeue();
                    Job.SaveOrder();

                    //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
                    ShowRightBoxAwaitVerify(false);
                    
                    systemState.StatusText = "Короб №:" + cBox.Number + " верифицирован! Задание закрыто!\nНажмите кнопку \"Завершить агрегацию\" для отправки отчета";
                    systemState.StatusBackground = Brushes.DarkGreen;


                    systemState.LayerCount = 1;
                    IncLayer = false;
                    systemState.BoxNumber = "";
                    systemState.ReadCodeCount = 0;


                    //если установлен режим матрици отрисовать пустую
                    if (Settings.BoxGrid)
                    {
                        this.Dispatcher.Invoke(async () =>
                        {
                            mainPage?.ShowEmptyMatrix();
                        });
                    }

                    //закрыть задание
                    Job.JobState = JobStates.CloseAndAwaitSend;

                    //bool b = CloseOrder().Result;
                    return true;
                }

                systemState.StatusText = "Короб №:" + cBox.Number + " успешно верифицирован.";
                systemState.StatusBackground = Brushes.Transparent;

                SetTimesAction(IoActions.Green, IoActions.RemoveGreen);
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //временно отключить добавление верифицированных кодов
                //для показа. 
                //job.BoxCancel(boxAssembly.cBox);

                //добавить короб в обработанные
                if (Master != null)
                    Job.AddVerifyBoxWithFullNum(cBox, Master.ID);
                else
                    Job.AddVerifyBoxWithFullNum(cBox, "");

                //получить количество обработанных коробов для вывода на екран
                systemState.ReadyBoxCount = Job.GetVerifyBoxCount();
                systemState.ReadyProductCount = Job.GetVerifyProductCount();
                //удалить короб из очереди
                Job.boxQueue.Dequeue();

                //обновить счечит ожидающих агрегации коробов
                systemState.BoxNotInPallete = Job.readyBoxes.Count(x => x.state == NumberState.Верифицирован);

                Job.SaveOrder();


                this.Dispatcher.Invoke(async () =>
                {
                    //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
                    ShowRightBoxAwaitVerify(false);
                    //обновить дланные по коробам
                    systemState.ProcessedBoxes?.Clear();
                    mainPage.UpdateBoxView();

                    //если установлен режим матрици отрисовать пустую
                    if (Settings.BoxGrid)
                        mainPage?.ShowEmptyMatrix();

                    //если стоит формировать палетную этикетку автоматически то выдать ее если палета полна
                    //если количество коробов включая этот достигло полной палеты то остановить линию если надо
                    if (Settings.PalletAutoCreate)
                    {
                        if (systemState.BoxNotInPallete >= Job.order1C.numBoxInPallet)
                            await CreatePallete();
                    }

                });
                return true;
            }
            catch (Exception)
            {

            }
            return false;
        }

        private void UpdateSystemState()
        {
            //обновить данные считано\осталось
            foreach (LabelField lf in Job.boxLabelFields)
            {
                if (lf.FieldName == "#productName#")
                    systemState.ProductName = lf.FieldData;
            }
            systemState.PartNumber = Job.lotNo;
            systemState.GTIN = Job.GTIN;
            systemState.LayersCount = Job.numLayersInBox;


            if (Job.numLayersInBox > 0)
                systemState.LayerCodeCount = Job.numРacksInBox / Job.numLayersInBox;
            else
            {
                systemState.LayerCodeCount = Job.numРacksInBox;
            }


            if (boxAssembly.cBox != null)
                systemState.BoxNumber = boxAssembly.cBox.Number;

            //systemState.BoxInOrders = job.
            if (Job?.numРacksInBox > 0)
            {
                if (Job.order1C?.numPacksInSeries > 0)
                    systemState.BoxInOrders = (Job.order1C.numPacksInSeries / Job.numРacksInBox);
            }
            systemState.ReadyBoxCount = Job.GetVerifyBoxCount();
            systemState.ReadyProductCount = Job.GetVerifyProductCount();

            //  this.Dispatcher.Invoke(() =>
            // {
            if (Job.boxQueue == null)
                Job.boxQueue = new Queue<BoxWithLayers>();

            if (Job.boxQueue.Count > 0)
                ShowRightBoxAwaitVerify(true); //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Visible;
            else
                ShowRightBoxAwaitVerify(false); //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
            //});
            //systemState.StatusText = "";
            //systemState.StatusBackground = Brushes.Transparent;

        }
        private void Job_OrderAcceptedEvent(object sender)
        {
            boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
           
            systemState.LayerCount = 0;
            UpdateSystemState();
            BtnOrderStart.Dispatcher.Invoke(() =>
            { 
                BtnOrderStart.IsEnabled = true;

                BoxMatrixCatalog boxMatrixCatalog = BoxMatrixCatalog.Load();
                if(boxMatrixCatalog.Catalog.FirstOrDefault(x => x.GTIN.Equals(Job.GTIN, StringComparison.OrdinalIgnoreCase)) is BoxMatrix bm)
                    SelectMainPage(bm.BoxGrid);

                Job.JobState = JobStates.Paused;
            });
           
            systemState.StatusText = "Задание получено. В работу не принято.";
            systemState.StatusBackground = Brushes.Transparent;
        }
        public void ClearBoxAndMarkAsBrak(string boxNum, bool suspendMsg = false)
        {


            try
            {
                if ((boxAssembly.cBox == null) && (Job.boxQueue.Count == 0))
                    throw new Exception("Нет коробов доступных для удаления");

                //проверить а мож но ли печаталь? 
                if (Job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (Job.JobState != JobStates.InWork)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");



                //найти короб для удаления
                if (boxAssembly.cBox.Number == boxNum)
                {
                    //запросить новый номер короба
                    int ost;
                    boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
                    //проверить если достигнута уставка запроса номеров запустить окно запроса
                    GetMoreBoxNumber(ost);

                    systemState.BoxNumber = boxAssembly.cBox.Number;
                    systemState.ReadCodeCount = 0;
                    this.Dispatcher.Invoke(() => {
                        //если все коды прошли проверку добавить слой в отработанные
                        systemState.StatusText = "Короб №: " + boxAssembly.cBox.Number + " успешно удалён";
                        systemState.StatusBackground = Brushes.Transparent;

                        systemState.ListCurrentSerials?.Clear();
                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление";
                    });

                }
                else
                {
                    //удаляем короб из очереди 
                    Job.boxQueue = new Queue<BoxWithLayers>(Job.boxQueue.Where(s => s.Number != boxNum));
                }
                //пометить короб как брак
                Job.BoxMarckAsBrack(boxNum);

                Job.SaveOrder();

            }
            catch (Exception ex)
            {
                if (!suspendMsg)
                {
                    systemState.StatusText = "Ошибка оператора: " + ex.Message;
                    systemState.StatusBackground = Brushes.Red;
                }
            }
        }
        public bool ClearBox(bool ShowQuestionDialog = true, bool ClearCurentBox = false, bool suspendMsg = false)
        {
            bool updateView = false;


            try
            {
                Log.Write("D.MW." + Thread.CurrentThread.ManagedThreadId + ".10:Запушена процедура удаления короба",EventLogEntryType.Error, 10);

                if ((systemState.CriticalError) && (ShowQuestionDialog))
                    Dispatcher.Invoke(() => { ShowCriticalWarning(); });// "", msg2); });

                if ((boxAssembly.cBox == null) && (Job.boxQueue.Count == 0))
                {
                    SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                    throw new Exception("Нет коробов доступных для удаления");
                }

                //проверить а мож но ли печаталь? 
                if ((Job.JobState == JobStates.Complited) && (Job.boxQueue.Count == 0))
                {
                    SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                    throw new Exception("Нет задания");
                }
                else if (Job.JobState != JobStates.Complited)
                {
                    if (Job.JobState != JobStates.Closes)
                    {
                        if (Job.JobState != JobStates.InWork)
                        {
                            SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                            throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");
                        }

                        if (boxAssembly.cBox != null)
                            boxAssembly.cBox.ManualCodeAdded = false;
                    }
                }

                if (boxAssembly.cBox?.NumbersCount == 0 && Job.boxQueue.Count == 0)
                {
                    SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                    throw new Exception("Нет коробов доступных для удаления!");
                }

                //определеится с описанием короба
                string strMsg1 = "", strMsg2 = "";
                bool clearLayerOnly = false;

                if (Job.boxQueue.Count > 0)
                {
                    BoxWithLayers cBox1 = Job.boxQueue.Peek();

                    strMsg1 = "Вы действительно хотите удалить короб №: " + cBox1.Number + " ?";
                    strMsg2 = "Расформирован собранный короб №: ";
                }
                else if (boxAssembly.cBox?.Numbers.Count > 0)
                {
                    strMsg1 = "Вы действительно хотите удалить короб №: " + boxAssembly.cBox.Number + " ?";
                    strMsg2 = "Расформирован текущий короб №: ";
                }
                else if (boxAssembly.cBox?.cLayer.Count > 0)
                {
                    strMsg1 = "Вы действительно хотите удалить слой №: " + boxAssembly.cBox.LayerNum + " ?";
                    strMsg2 = "Слой удален. Текущий короб №: ";
                    clearLayerOnly = true;
                }

                //вывести запрос на подтверждение если надо
                if (ShowQuestionDialog)
                {
                    ClearMsgPane();

                    StopLine();
                    //переставить таймер
                    windowTimeOut.Stop();
                    windowTimeOut.Start();


                    if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, strMsg1, PharmaLegacy.Windows.MessageBoxExButton.YesNo) != MessageBoxResult.Yes)
                    {
                        windowTimeOut.Stop();
                        return false;
                    }
                    windowTimeOut.Stop();
                    // return;
                }
                //
                //systemState.StatusText = "Расформирован короб №:";
                // systemState.StatusBackground = Brushes.Transparent;

                BoxWithLayers? cBox = null;
                if ((Job.boxQueue.Count > 0) && (!ClearCurentBox))
                {
                    cBox = Job.boxQueue.Dequeue();
                    if (Job.boxQueue.Count == 0)
                        ShowRightBoxAwaitVerify(false); //systemState.RightBoxAwaiVerifyVisiblility = Visibility.Hidden;
                   
                }
                else
                {
                    cBox = boxAssembly.cBox;
                    updateView = true;
                    SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                }


                if (cBox == null)
                    return true;

                if (!suspendMsg)
                {
                    systemState.StatusText = strMsg2 + cBox.Number;//"Расформирован короб №:" + cBox.Number;
                    systemState.StatusBackground = Brushes.Transparent;
                }

                if (clearLayerOnly)
                {
                    cBox.ClearAssembledLayer();
                }
                else
                {

                    Job.BoxMarckAsBrack(cBox.Number);
                    cBox.ClearBox();
                }

                systemState.LayerCount = 1;
                systemState.GoodLayerCodeCount = 0;
                IncLayer = false;

                systemState.ReadCodeCount = 0;

                Job.SaveOrder();

                Dispatcher.Invoke(() =>
                {
                    if (updateView)
                    {
                        systemState.ListCurrentSerials?.Clear();
                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //
                    }

                    //mainPage.ImageView.Children.Clear();
                    //mainPage.ImageView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB3B3B3"));// new ImageBrush();
                });
                return true;
            }
            catch (Exception ex)
            {
                if (!suspendMsg)
                {
                    systemState.StatusText = "Ошибка оператора: " + ex.Message;
                    systemState.StatusBackground = Brushes.Red;
                }
                Log.Write("MW." + Thread.CurrentThread.ManagedThreadId + ".10:" + ex.ToString(),EventLogEntryType.Error, 10);
            }
            finally
            {
                Log.Write("D.MW." + Thread.CurrentThread.ManagedThreadId + ".10:окончена процедура удаления короба",EventLogEntryType.Error, 10);
            }
            return false;
        }
        public void CloseBox(bool notFull)
        {
            try
            {
                if (boxAssembly.cBox.NumbersCount < 1)
                    throw new Exception("Невозможно закрыть короб так как он пуст!");

                boxAssembly.cBox.CloseNotFull = notFull;

                //job.boxQueue.Enqueue(boxAssembly.cBox.Clone());

                AddBoxToQueue(boxAssembly.cBox.Clone(),autoVerify: true);
                //если все коды прошли проверку добавить слой в отработанные
                systemState.StatusText = "Короб №:" + boxAssembly.cBox.Number + "  успешно агрегирован. Верифицируйте короб.";
                //отобразить короб справа
                ShowRightBoxAwaitVerify(true);
                systemState.StatusBackground = Brushes.Blue;



                boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);
                Job.SaveOrder();
                int ost;
                boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);
                GetMoreBoxNumber(ost);

                systemState.LayerCount = 1;
                IncLayer = false;
                systemState.BoxNumber = boxAssembly.cBox.Number;
                systemState.ReadCodeCount = 0;


                //обновить окно
                this.Dispatcher.Invoke(() =>
                {
                    systemState.ListCurrentSerials?.Clear();
                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Кодов {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //

                    mainPage.UpdateView();
                });

                //очистить счетчик
                SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
            }
            catch (Exception ex)
            {
                systemState.StatusText = ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
        }
        public void BtnDeleteCodeFromCurrentBox()
        {
            if (systemState.CriticalError)
            {
                ShowCriticalWarning();
            }

            try
            {
                //очистить панель сообшений
                ClearMsgPane();

                //проверить а мож но ли печаталь? 
                if (Job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (Job.JobState != JobStates.InWork)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");

                systemState.StatusText = "";
                systemState.StatusBackground = Brushes.Transparent;

                HandScannerMode = HandScannerMode.DropCodeFromCurrentBox;

                //переставить таймер
                windowTimeOut.Stop();
                windowTimeOut.Start();

                MessageBoxResult res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Считайте ручным сканером код удаляемого из текущего короба продукта",
                    PharmaLegacy.Windows.MessageBoxExButton.Cancel, Dbg);

                if (res == MessageBoxResult.Cancel)
                    windowTimeOut.Stop();

            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally { HandScannerMode = HandScannerMode.Default; }
        }
        private bool DeleteCodeFromCurrentBox(string fullNumber)
        {
            Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

            //проверить если ли корректное задание?
            if (Job.numРacksInBox < 1)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }
            //проверить запущено ли задание
            if (Job.JobState != JobStates.InWork)
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            //проверить на сооответствие задание
            string msg = Job.VerifyProductNum(ld);
            if (!string.IsNullOrEmpty(msg))// != "")
            {
                systemState.StatusText = "Невозможно отбраковать!\n" + msg;
                systemState.StatusBackground = Brushes.Red;
                return false;
            }

            

            //проверить есть ли такой номер в текущем слое
            //и запретить добавление если он не в текушем слое а например в предыдущем
            var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
            string boxNum = r.boxNum;
            if (r.IsExist)
            {
                if (boxAssembly.cBox.RemoveItem(ld.SerialNumber))
                {
                    systemState.StatusText = @"Продукт №: " + ld.SerialNumber + " удален из текущего короба!";
                    systemState.StatusBackground = Brushes.Transparent;

                    //update counters
                    Dispatcher.Invoke(() =>
                    {
                        ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();

                        //выбрать до 10 последних номеров в обратной последовательности
                        List<Unit> last10 = new ();
                        last10.AddRange(boxAssembly.cBox.Numbers.Skip(Math.Max(0, boxAssembly.cBox.Numbers.Count - 10)));

                        foreach (Unit itm in last10)
                        {
                            pbView.Add(new SerialCode(itm.Number));
                        }

                        pbView.Reverse();

                        systemState.ListCurrentSerials?.Clear();
                        systemState.ListCurrentSerials = pbView;
                        systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //

                    });
                    return true;
                }
            }

            systemState.StatusText = "Невозможно отбраковать!\nПродукт №: " + ld.SerialNumber + " отсутствует в текущем коробе!";
            systemState.StatusBackground = Brushes.Red;
            return false;
        }
        public void SelectDrobBoxOrDropCode()
        {
            MessageBoxResult res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Вы хотите удалить несколько продуктов из текущего короба, или очистить его весь?\n Нажмите:\n\"Да\"  - если Вы хотите удалить из текущего короба несколько продуктов.\n\"Нет\" - Если хотите очистить весь текущий короб\nИли нажмите Отмена для отмены выбора операции", PharmaLegacy.Windows.MessageBoxExButton.YesNoCancel);
            switch (res)
            {
                case MessageBoxResult.Yes:
                    BtnDeleteCodeFromCurrentBox();
                    break;
                case MessageBoxResult.No:
                    ClearBox();
                    break;
                default:
                    break;
            }
        }

        #region Vision  
        public void UpdateView()
        {
            codeAddEventHandler?.Invoke(AddCodeType.Uncknow);

            //неделать ничего если нет задания
            if (Job.JobState == JobStates.Complited)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;

                // SetTimesAction(IoActions.Red, IoActions.RemoveRed);
                // SetTimesAction(IoActions.Sound, IoActions.RemoveSound, 1000);
                return;
            }

            //увелисить номер слоя если надо
            if (IncLayer)
            {
                IncLayer = false;
                systemState.LayerCount++;
            }

            //проверить если ли корректное задание?
            if (Job.numРacksInBox < 1)
            {
                systemState.StatusText = "Нет задания!";
                systemState.StatusBackground = Brushes.Red;
                return;
            }
            //проверить запущено ли задание
            if (Job.JobState != JobStates.InWork)
            {
                systemState.StatusText = $"{TextConstants.WorkTypeUp} короба не начата !";
                systemState.StatusBackground = Brushes.Red;
                return;
            }

            //если текущий слой не полный или имеет добавленные вручную продукты сбросить его
            if ((!boxAssembly.cBox.LastLayer.LayerIsFull) || (boxAssembly.cBox.LastLayer.LayerManualAdd))
                boxAssembly.cBox.ClearLastLayer();

            //проверить полон ли короб если он полон ничего не делать пока не закроют короб
            if (boxAssembly.cBox.NumbersCount == Job.numРacksInBox)
            {
                systemState.StatusText = "Верифицируйте предыдущий короб!";
                systemState.StatusBackground = Brushes.DarkOrange;
                return;
            }


        }
        //управление камерой + онлайн + соннеск
        private void BtnCamSate_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region WebService 
        private bool StartWebService()
        {
            //********************************
            //запустить вебсервис
            webServerWorker.DoWork += delegate
            {
                try
                {
                    hs = new Aardwolf.HttpAsyncHost(
                        new SerializeHttpAsyncHandler(Job,Settings,Settings.PacketLogEnable,Settings.Srv1CLogin,Settings.Srv1CPass,Settings.LineNum), 100);
                    string[] pref = new string[2] { Settings.LocalWebSrvUrl.ToString(CultureInfo.InvariantCulture)+"/jobs/",
                                                Settings.LocalWebSrvUrl.ToString(CultureInfo.InvariantCulture) + "/mark/" };

                    Log.Write("Веб сервис запущен как " + pref[0] + " " + pref[1],EventLogEntryType.Information, 53);
                    systemState.webService = true;

                    hs.Run(pref);

                }
                catch (Exception ex)
                {
                    Log.Write("Ошибка веб интерфейса " + ex.Message,EventLogEntryType.Error, 53);
                    systemState.webService = false;
                }
            };
            webServerWorker.WorkerSupportsCancellation = true;
            webServerWorker.RunWorkerAsync();
            //********************************


            Log.Write("Сервис запущен",EventLogEntryType.Information, 0);
            return true;
        }
        private string SendReport<T>(Uri myUri, T r)
        {
            // string result1;

            if (r == null)
                return "Передача в 1с невозможна. Отчет не сформирован!";



            // System.Uri myUri = new System.Uri(textBox1.Text + "/jobs/");
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(myUri);
            httpWebRequest.ContentType = "application/json";//"application/json;charset=utf-8";
            httpWebRequest.Method = "POST";
            httpWebRequest.Timeout = 50000;

            //**************авторизация***********
            NetworkCredential myNetworkCredential = new NetworkCredential(Settings.Srv1CLogin,
                Settings.Srv1CPass);

            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(myUri, "Basic", myNetworkCredential);

            httpWebRequest.PreAuthenticate = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            httpWebRequest.Credentials = myCredentialCache;
            //*************************

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(T));

            //cгенерировать md5
            string s = Archive.SerializeJSon<T>(r);
            httpWebRequest.Headers.Add(HttpRequestHeader.ContentMd5, MD5Calc.CalculateMD5Hash(s));

            //сохранить отчет на диск
            System.IO.TextWriter logFile = new System.IO.StreamWriter("Reports.txt", true);
            logFile.WriteLine(s);
            logFile.Close();


            jsonSerializer.WriteObject(httpWebRequest.GetRequestStream(), r);

            httpWebRequest.GetRequestStream().Flush();
            httpWebRequest.GetRequestStream().Close();

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                string result;
                using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                //label1.Text = "Статус:" + HttpStatusCode.Created.ToString() + "\n" + result;
                //проверить статус код
                if (httpResponse.StatusCode == HttpStatusCode.Created)
                {
                    Log.Write("Отчет передан успешно",EventLogEntryType.Information, 1000 + 17);
                    return "";
                }
                Log.Write("Не возможно передать отчет на сервер. Ответа сервера: " + result,EventLogEntryType.Error, 1000 + 18);
                return "Не возможно передать отчет на сервер. Ответа сервера: " + result;

            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    Log.Write("Не возможно передать отчет на сервер. " + ex.Message,EventLogEntryType.Error, 1000 + 19);
                    return "Не возможно передать отчет на сервер. " + ex.Message;
                }

                try
                {
                    using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                    {
                        string err = reader.ReadToEnd();
                        Log.Write("Не возможно передать отчет на сервер. " + ex.Message + " Ответа сервера: " + err,EventLogEntryType.Error, 1000 + 19);
                        return "Не возможно передать отчет на сервер. " + ex.Message + " Ответа сервера: " + err;
                    }
                }
                catch
                {

                }
            }
            catch (Exception ex)
            {
                Log.Write("Не возможно передать отчет на сервер. " + ex.Message,EventLogEntryType.Error, 1000 + 20);
                return "Не возможно передать отчет на сервер. " + ex.Message;
            }
            return "Error";
        }
        #endregion

        #region Задание
        //начать задание
        private void BtnOrderStart_Click(object sender, RoutedEventArgs e)
        {
            //очистить панель сообшений
            ClearMsgPane();

            if (UserAuth(true, false, false, true, Master) == null)
                return;

            int ost;
            boxAssembly.cBox = Job.GetNextBoxWithLayers(out ost);


            if (boxAssembly.cBox == null)
            {
                systemState.StatusText = "В задании больше нет номеров коробов! ";
                systemState.StatusBackground = Brushes.DarkOrange;
            }

            // //проверить нет ли в задании короба ожидающего подтверждения
            if (Job.boxQueue.Count > 0)
            {
                BoxWithLayers cBox1 = Job.boxQueue.Peek();
                systemState.StatusText += "\nКороб №: " + cBox1.Number + " ожидает верификации.";
            }


            BtnOrderStart.IsEnabled = false;
            BtnOrderClose.IsEnabled = true;
            Job.JobState = JobStates.InWork;

            GetMoreBoxNumber(ost, true);

            //обновить дланные по коробам
            systemState.ProcessedBoxes?.Clear();

            systemState.ProcessedBoxes = null;


            //обновить список готовых коробов
            mainPage.UpdateBoxView();

            systemState.LayerCount = 1;
            systemState.PackInBox = Job.order1C?.numPacksInBox ?? 0;
            systemState.BoxInOrders = ((Job.order1C?.numPacksInSeries ?? 1) / Job.numРacksInBox);
            systemState.ReadyPalleteCount = Job.readyPallets.Count(x => x.state == NumberState.Верифицирован);
            systemState.BoxNotInPallete = Job.readyBoxes.Count(x => x.state == NumberState.Верифицирован);


            if (boxAssembly.cBox?.Number != null)
                systemState.SerialInBoxCounter = $"Штук {boxAssembly.cBox.Numbers.Count} из {systemState.PackInBox}";


            systemState.Packer = "Мастер:\n" + Master.Name;// 41

            UpdateSystemState();

            /*
            //проверить не остался ли в очеереди какой короб с предыдущей сессии ?
            if (job.boxQueue.Count > 0)
            {
                BoxWithLayers cBox1 = job.boxQueue.Peek();
                systemState.StatusText = "В очереди стоит собранный но не подтвержденный короб №: " + cBox1.Number+".\nВерифицируйте его или расформируйте!";
                systemState.StatusBackground = Brushes.DarkOrange;

                //strMsg1 = "Вы действительно хотите удалить короб №: " + cBox1.Number + " ?";
                //strMsg2 = "Расформирован собранный короб №: ";
            }*/

        }
        private async void BtnOrderClose_Click(object sender, RoutedEventArgs e)
        {

            //очистить панель сообшений
            ClearMsgPane();

            //защита от двойных нажатий
            if (!BtnOrderClose.IsEnabled)
                return;

            try
            {
                BtnOrderClose.IsEnabled = false;

                //проверить останов линии
                if (!(systemState.StopLine == true))
                    throw new Exception("Ошибка оператора: Необходимо остановить линию перед завершением задания!");

                //проверить есть ли неподтвержденные короба
                if (Job.boxQueue.Count > 0)
                {
                    BoxWithLayers cBox1 = Job.boxQueue.Peek();
                    systemState.StatusText += "Короб №: " + cBox1.Number + " ожидает верификации.\nВерифицируйте его или удалите перед завершением задания!";
                    systemState.StatusBackground = Brushes.DarkOrange;
                    return;
                }


                //проверить неполный короб
                if ((boxAssembly.cBox?.NumbersCount < Job.numРacksInBox) && (boxAssembly.cBox?.NumbersCount > 0))
                {
                    if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Последний короб не полный! Вы действительно хотите завершить задание?", PharmaLegacy.Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                        return;
                }
                else if (!App.Settings.AuthorizationEnable && Job.JobState != JobStates.CloseAndAwaitSend)
                {
                    if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Вы действительно хотите завершить задание?", PharmaLegacy.Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                        return;
                }


                if (UserAuth(true, false, false, true, Master) == null)
                    return;

                //проверить текущий короб есть он и в каком состоянии
                if (boxAssembly.cBox != null)
                {
                    //проверить неполный короб
                    if ((boxAssembly.cBox.NumbersCount < Job.numРacksInBox) && (boxAssembly.cBox.NumbersCount > 0))
                    {
                        // if (System.Windows.Forms.MessageBox.Show("Последний короб не полный! Вы действительно хотите завершить агрегацию?", "", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                        // if (Windows.MessageBoxEx.ShowEx(this, "Последний короб не полный! Вы действительно хотите завершить агрегацию?", Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                        //     return;

                        if (boxAssembly.cBox.NumbersCount > 0)
                        {
                            closeAfterVerifyLastBox = true;
                            boxAssembly.cBox.CloseAssembledLayer();
                            CloseBox(true);



                            ///
                            //boxAssembly.cBox.CloseNotFull = true;
                            ////job.boxQueue.Enqueue(boxAssembly.cBox.Clone());
                            //AddBoxToQueue(boxAssembly.cBox.Clone());

                            ////если все коды прошли проверку добавить слой в отработанные
                            //systemState.StatusText = "Короб №:" + boxAssembly.cBox.Number + "  успешно агрегирован. Верифицируйте короб.";
                            //systemState.StatusBackground = Brushes.Blue;


                            ////обновить дланные по коробам
                            //systemState.ProcessedBoxes?.Clear();

                            //boxAssembly.cBox?.Numbers.Clear();
                            //SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);

                            //systemState.ListCurrentSerials?.Clear();
                            //systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", boxAssembly.cBox.Numbers.Count, systemState.PackInBox); //"запустить обновление"; //

                            return;
                        }
                    }
                }

                //WaitPanel.Visibility = Visibility.Visible;
                //MainFrame.Visibility = Visibility.Collapsed;
                //systemState.StatusText = "Идет отправка отчета.";
                //systemState.StatusBackground = Brushes.Transparent;

                //bool b = CloseOrder().Result;
                await CloseOrder();
            }
            catch (Exception ex)
            {
                systemState.StatusText = ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally
            {
                //WaitPanel.Visibility = Visibility.Collapsed;
                //MainFrame.Visibility = Visibility.Visible;
                if (Job.JobState != JobStates.Empty)
                    BtnOrderClose.IsEnabled = true;
            }
        }
        private CancellationTokenSource sendToken;
        //private static readonly int MAIN_ERROR_CODE = 10000;

        //private bool CloseOrder() {
        private async Task<bool> CloseOrder()
        {
            try
            {
                //очистить панель сообшений
                ClearMsgPane();
                //обработать пустое задание
                bool NoSendData = true;
                bool sendEmpty = false;
                //добавить обработанные коды
                foreach (PartAggSrvBoxNumber b in Job.readyBoxes)
                {
                    if (b.state == NumberState.Верифицирован || b.state == NumberState.VerifyAndPlaceToPalete)
                        NoSendData = false;
                }

                //добавить забракованные коды
                foreach (JobController.DefectiveCodeSrv b in Job.brackBox)
                {
                    if (b.state == NumberState.Верифицирован)
                        NoSendData = false;
                }

                if (NoSendData && (Job.OrderaArray.Count == 0))
                {
                    if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Задание не содержит данных для отправки!\n Вы действительно хотите завершить задание?", PharmaLegacy.Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                        return false;
                    sendEmpty = true;
                }
                //закрыть задание
                Job.JobState = JobStates.CloseAndAwaitSend;
                //вклчить окно ожидания
                if (Dispatcher.CheckAccess())
                {
                    WaitPanel.Visibility = Visibility.Visible;
                    MainFrame.Visibility = Visibility.Collapsed;
                    Thread.Sleep(500);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        WaitPanel.Visibility = Visibility.Visible;
                        MainFrame.Visibility = Visibility.Collapsed;
                    });
                }

                sendToken = new CancellationTokenSource();

                //перед отправкой создать архив файлов задания и сохранить его.
                string sourceDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp";
                string destDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\DbArchive";
                string filename = $"{DateTime.Now.ToString("yyyy.MM.dd-HH.mm", CultureInfo.InvariantCulture)}.{Job.order1C.gtin}.{Job.order1C.lotNo}";


                // if (! await Utilite.Archive.ArchiveDir(sourceDir, destDir, filename))
                //    Log.Write("Архив рабочей БД не создан!!!",EventLogEntryType.Error, 2115);


                string result = "send report error";
                if (Settings.UseReportServerFtp)
                {
                    result = await Task<string>.Run(() => Job.SendFtpReports(Settings.ReportServerFtp, Settings.Srv1CLogin, Settings.Srv1CPass,
                       Settings.ReportServerFtpDir, sendEmpty, Settings.Srv1CRequestTimeout));
                }
                else if (Settings.UseReportUncDir)
                {
                    result = await Task<string>.Run(() => Job.SendUncReports(Settings.ReportUncDir, sendEmpty, Settings.Srv1CRequestTimeout));
                }
                else
                {
                    result = await Job.SendBaikalReport(Settings.Srv1CUrl, Settings.Srv1CLogin, Settings.Srv1CPass, false, sendEmpty,
                          Settings.Srv1CRequestTimeout, sendToken.Token);

                    //result = await Job.SendSerishevoReport(Settings.Srv1CUrl, Settings.Srv1CLogin, Settings.Srv1CPass, false, sendEmpty,
                    //       Settings.Srv1CRequestTimeout, sendToken.Token);
                }

                if (!string.IsNullOrEmpty(result))
                {
                    // job.jobComplited = false;
                    result = result.Trim();
                    Dispatcher.Invoke(() =>
                    {
                        systemState.StatusText = result;
                        systemState.StatusBackground = Brushes.Red;
                    });
                    return false;
                }


               

                //очистить окно
                Dispatcher.Invoke(() =>
                {
                    systemState.ProcessedBoxes?.Clear();
                    systemState.ListCurrentSerials?.Clear();

                    // job.JobState = JobState.Complited;
                    BtnOrderStart.IsEnabled = false;
                    BtnOrderClose.IsEnabled = false;

                    systemState.StatusText = "Задание завершено и успешно передано в систему верхнего уровня.";
                    systemState.StatusBackground = Brushes.Transparent;

                    systemState.ReadyBoxCount = 0;
                    systemState.ReadyProductCount = 0;
                    systemState.BoxInOrders = 0;
                    systemState.PackInBox = 0;
                    systemState.PartNumber = "";
                    systemState.BoxNumber = "";
                    systemState.LayerCount = 0;
                    systemState.LayersCount = 0;
                    systemState.ReadCodeCount = 0;
                    systemState.GoodLayerCodeCount = 0;
                    systemState.LayerCodeCount = 0;
                    systemState.ProductName = "";
                    //systemState.SerialInBoxCounter = "запустить обновление"; //

                });

                //очистить задание
                Job.Clear();
                Job.DeleteOrder();

                Job.OrderAcceptedEvent += Job_OrderAcceptedEvent;
                boxAssembly.cBox = new BoxWithLayers("", Job.numLayersInBox, Job.numРacksInBox);


                systemState.GoodLayerCodeCount = 0;
                systemState.LayerCodeCount = 0;
                IncLayer = false;
                systemState.ReadCodeCount = 0;
                systemState.LayerCount = 0;

                UpdateSystemState();

                //очистить счетчик
                SetTimesAction(IoActions.ClearCounter, IoActions.RemoveClearCounter, 400);
                return true;
            }
            finally
            {
                if (Dispatcher.CheckAccess())
                {
                    WaitPanel.Visibility = Visibility.Collapsed;
                    MainFrame.Visibility = Visibility.Visible;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        WaitPanel.Visibility = Visibility.Collapsed;
                        MainFrame.Visibility = Visibility.Visible;
                    });
                }
            }
            //return false;
            //
            //UpdateView();
        }
        private void CancelSend_Click(object sender, RoutedEventArgs e)
        {
            sendToken?.Cancel();
        }
        //вызов формы отбраковки
        public void BtnBrack()
        {
            if (systemState.CriticalError)
            {
                   //systemState.CriticalError = false;
                ShowCriticalWarning();// "", msg2);
            }

            try
            {
                //очистить панель сообшений
                ClearMsgPane();

                //проверить а мож но ли печаталь? 
                if (Job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (Job.JobState != JobStates.InWork)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");

                systemState.StatusText = "";
                systemState.StatusBackground = Brushes.Transparent;

                HandScannerMode = HandScannerMode.Brack;
                //if (System.Windows.Forms.MessageBox.Show("Вы действительно хотите удалить все данные о продуктах в коробе?", "", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                //if (
                //переставить таймер
                windowTimeOut.Stop();
                windowTimeOut.Start();

                MessageBoxResult res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Считайте ручным сканером код отбраковымаемой пачки",// или короба",
                    PharmaLegacy.Windows.MessageBoxExButton.Cancel, Dbg,
                    () => 
                    {
                        //найти кодик для отбраковки
                        Unit? u = boxAssembly.cBox.cLayer.FirstOrDefault();

                        if (u is null)
                        {
                            if (Job.readyBoxes.FirstOrDefault(x => x.state == NumberState.Верифицирован) is PartAggSrvBoxNumber num)
                            {
                                //для кода короба
                                //ProssedData($"00{num}", WorckMode.Both);

                                if (num.productNumbers.FirstOrDefault() is string num2)
                                {
                                    //для кода пачки
                                    ProssedData(num2, WorckMode.Both);
                                }
                            }
                        }
                        else if (u is Unit)
                        {
                            ProssedData(u.Barcode, WorckMode.Both);
                        }
                    });// == MessageBoxResult.Cancel)

                if (res == MessageBoxResult.Cancel)
                    windowTimeOut.Stop();

            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally { HandScannerMode = HandScannerMode.Default; }
        }
        public void BtnSample()
        {
            try
            {
                throw new NotImplementedException();

                /*
                //очистить панель сообшений
                ClearMsgPane();

                //проверить а мож но ли печаталь? 
                if (job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (job.JobState != JobStates.InWorck)
                    throw new Exception("$"{TextConstants.WorkTypeUp} не начата!!");

                //авторизовать контролера
               // if (!MasteAuth(false, true, false,out controler)) 
                 //   return;

                //controler = autWin.user;


                handScannerMode = MainWindow.HandScannerMode.Sample;
                //if (System.Windows.Forms.MessageBox.Show("Вы действительно хотите удалить все данные о продуктах в коробе?", "", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                //if (
                Windows.MessageBoxEx.ShowEx(this, "Считайте ручным сканером код отбираемой как образец пачки", Windows.MessageBoxExButton.Cancel);//== MessageBoxResult.Cancel)
                   */                                                                                                                               //   return;

            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally { HandScannerMode = HandScannerMode.Default; }
            //owner.systemState.StatusText = "Считайте ручным сканером код отбраковываемой пачки";
            //owner.systemState.StatusBackground = Brushes.DarkOrange;
        }
        public void BtnHelp()
        {
            try
            {
                if (systemState.CriticalError)
                    ShowCriticalWarning();

                //очистить панель сообшений
                ClearMsgPane();
                string boxNumInPal = Job.order1C?.numBoxInPallet.ToString();

                HandScannerMode = HandScannerMode.Help;
                PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, $@"Текущее задание

{systemState.ProductName}
GTIN:                          {Job.GTIN}
Партия:                      {Job.lotNo}
Дата производства: {Job.date.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)}
Годен до:                   {Job._expDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)}

Для вывода справки считайте ручным сканером код продукта", PharmaLegacy.Windows.MessageBoxExButton.Cancel);

                HandScannerMode = HandScannerMode.Default;

            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally { HandScannerMode = HandScannerMode.Default; }
        }
        public void btnCloseBox(bool stop)
        {
            try
            {
                if (systemState.CriticalError)
                {
                     //systemState.CriticalError = false;
                    ShowCriticalWarning();// "", msg2);
                }

                //очистить панель сообшений
                ClearMsgPane();
                //проверить а мож но ли печаталь? 
                if (Job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (Job.JobState != JobStates.InWork)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");

                if (!stop)
                    throw new Exception("Необходимо остановить линию перед закрытием короба!");


                if (Job.boxQueue.Count > 0)
                    throw new Exception("Перед закрытием необходимо верифицировать предыдущий короб!");


                windowTimeOut.Stop();
                windowTimeOut.Start();
                if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Вы действительно хотите закрыть текущий короб?", PharmaLegacy.Windows.MessageBoxExButton.YesNo) != MessageBoxResult.Yes)
                    return;

                Autorization.User user = new Autorization.User();
                if (UserAuth(true, false, false, true, user) == null)
                    return;

                boxAssembly.cBox.CloseAssembledLayer();
                CloseBox(true);
                //
            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally
            { //handScannerMode = MainWindow.HandScannerMode.Default;
                windowTimeOut.Stop();
            }
        }
        #endregion


        #region Debug Tests
        public void DbgTestFnc1(string mode)
        {
            if (HandScannerMode == HandScannerMode.Brack)
            {
                //проверяем брак короба.
                //найти последний нормальный короб и взять его номер для удаления
                var box = Job.readyBoxes.FirstOrDefault(x => x.state == NumberState.Верифицирован);
                if (box != default)
                    ProssedData(box.boxNumber, WorckMode.Left);
            }
        }

        public void DbgTestDropCodeFromCurrentBox()
        {
            if (HandScannerMode == HandScannerMode.DropCodeFromCurrentBox)
            {
                //проверяем брак короба.
                //найти последний нормальный короб и взять его номер для удаления
                var num = boxAssembly.cBox.Numbers.FirstOrDefault();
                if (num != default)
                    ProssedData(num.Barcode, WorckMode.Left);
            }
        }

        public async Task TestFunc() 
        {
            await modBus.StartScan();
        }

        public async Task TestSetGreenLight(bool on)
        {
            if (on)
                await modBus.OnGreenLight();
            else
                await modBus.OffGreenLight();
        }

        public async Task TestSetRedLight(bool on)
        {

            if (on)
                await modBus.OnRedLight();
            else
                await modBus.OffRedLight();
        }

        
        #endregion

        #region Window Controls and Close
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (PharmaLegacy.Windows.MessageBoxEx.ShowEx(this, "Вы действительно хотите выйти из программы?", PharmaLegacy.Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                return;

            //if (System.Windows.Forms.MessageBox.Show("Вы действительно хотите выйти из программы?","",System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
            //    return;

            if (webServerWorker != null)
            {
                if (webServerWorker.WorkerSupportsCancellation)
                    webServerWorker.CancelAsync();
            }

           

            Close();
        }
        //таскание окна
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
        }

        private void MainWindow1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (!e.Handled)
            {
                object o = e.OriginalSource;
                if (this.WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            }/**/
        }
        private void SettingsPage_BackClick(object sender, RoutedEventArgs e)
        {
            //if (Settings.BoxGrid)
            //    mainPage = mainPageMatrix;
            //else
            //    mainPage = mainPageLun;

            MainFrame.Navigate(mainPage);
            BannerGrid.Visibility = Visibility.Visible;
            CloseApp.Visibility = Visibility.Hidden;

            //если установлен режим матрици отрисовать пустую
            if (Settings.BoxGrid)
                mainPage?.ShowEmptyMatrix();
        }
        #endregion

        #region Печать
        public static string CreateExpDateFix(string strExpDate, JobController job)
        {
            string result = "";
            DateTime _data = DateTime.MinValue;
            try
            {
                if (strExpDate == null)
                    throw new Exception();

                if (strExpDate.Length == 6)
                {
                    String dFormat = "yyMMdd";
                    _data = DateTime.ParseExact(strExpDate, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                }
                else
                    _data = DateTime.Parse(strExpDate, CultureInfo.CurrentCulture);

                result = _data.ToString(job.order1C.formatExpDate, CultureInfo.InvariantCulture);

                return result;
            }
            catch
            {
                return "ErrData";
            }
        }
        public static string CreateDateFix(string strExpDate, JobController job)
        {
            string result = "";
            DateTime _data = DateTime.MinValue;
            try
            {
                if (strExpDate == null)
                    throw new Exception();

                if (strExpDate.Length == 6)
                {
                    String dFormat = "yyMMdd";
                    _data = DateTime.ParseExact(strExpDate, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                }
                else
                    _data = DateTime.Parse(strExpDate, CultureInfo.CurrentCulture);

                result = _data.ToString(job.order1C.formatExpDate, CultureInfo.InvariantCulture);

                return result;
            }
            catch
            {
                return "ErrData";
            }
        }
        //репринт окно
        public void ReprintEvent()
        {
            if (systemState.CriticalError)
                ShowCriticalWarning();


            try
            {
                //очистить панель сообшений
                ClearMsgPane();

                //проверить а мож но ли печаталь? 
                if(Job.JobState != JobStates.InWork && !Job.readyBoxes
                    .Where(x=>x.state == NumberState.Верифицирован || x.state == NumberState.Собирается).Any())
                    throw new Exception("Нет коробов для которых был бы возможен повтор печати этикетки");

                //if (Job.JobState == JobStates.Complited)
                //    throw new Exception("Нет задания");

                //if (Job.JobState != JobStates.InWork)
                //    throw new Exception("$"{TextConstants.WorkTypeUp} не начата!!");

                systemState.StatusText = "";
                systemState.StatusBackground = Brushes.Transparent;

                HandScannerMode = HandScannerMode.Reprint;

                //переставить таймер
                windowTimeOut.Stop();
                windowTimeOut.Start();

                MessageBoxResult res = PharmaLegacy.Windows.ReprintWindow.ShowEx(this, "Считайте ручным сканером код  пачки или короба для повтора этикетки", PharmaLegacy.Windows.MessageBoxExButton.Cancel);

                if (res == MessageBoxResult.No)
                    Print();

                //if (res == MessageBoxResult.Cancel)
                windowTimeOut.Stop();

            }
            catch (Exception ex)
            {
                systemState.StatusText = "Ошибка оператора: " + ex.Message;
                systemState.StatusBackground = Brushes.Red;
            }
            finally { HandScannerMode = HandScannerMode.Default; }

        }
        //печать репринт
        private bool ReprintAnyReleasedBox(string indata)
        {
            try
            {
                //определить тип номера. короб или продукт
                GsLabelData ld = new Util.GsLabelData(indata);
                if (!string.IsNullOrEmpty(ld.SerialNumber))
                {
                    string result = Job.VerifyProductNum(ld);
                    if (!string.IsNullOrEmpty(result))// != "")
                    {

                        systemState.StatusText = $"Продукт не соответствует заданию! {result}";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }

                    //проверить в текущей коробке
                    var r = Job.IsAlreadyInCurrentBoxes(boxAssembly, ld.SerialNumber);
                    string boxNum = r.boxNum;
                    if (r.IsExist)

                        //проверить в уже верифицированных номерах
                        Job.IsAlreadyInProcessedBox(indata, out boxNum);

                    if (string.IsNullOrEmpty(boxNum))
                    {
                        systemState.StatusText = $"Номер: {indata} не найден в выпущенных коробах!\nПечать не возможна!";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }
                    PartAggSrvBoxNumber? b = Job.readyBoxes.FirstOrDefault(x => x._boxNumber.Equals(boxNum, StringComparison.Ordinal));

                    if (b == null)
                    {
                        systemState.StatusText = $"Номер: {boxNum} не найден в выпущенных коробах!\nПечать не возможна!";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }
                    BoxWithLayers cBox = new BoxWithLayers(b.boxNumber, Job.numLayersInBox, Job.numРacksInBox);
                    foreach (string s in b.productNumbers)
                        cBox.Numbers.Add(new Unit() { Barcode = s });

                    CreateLabelAndPrint(cBox, Job, this);
                }
                else if (!string.IsNullOrEmpty(ld.SerialShippingContainerCode00))
                {
                    PartAggSrvBoxNumber? b = Job.readyBoxes.FirstOrDefault(x => x._boxNumber.Equals(indata, StringComparison.Ordinal));

                    if (b == null)
                    {
                        systemState.StatusText = $"Номер: {indata} не найден в выпущенных коробах!\nПечать не возможна!";
                        systemState.StatusBackground = Brushes.DarkOrange;
                        return false;
                    }
                    BoxWithLayers cBox = new BoxWithLayers(b.boxNumber, Job.numLayersInBox, Job.numРacksInBox);
                    foreach (string s in b.productNumbers)
                        cBox.Numbers.Add(new Unit() { Barcode = s });

                    CreateLabelAndPrint(cBox, Job, this);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("MW", ex.Message, EventLogEntryType.Error, 120024);
                systemState.StatusText = $"Ошибка печати! {ex.Message}";
                systemState.StatusBackground = Brushes.DarkOrange;
            }
            return false;
        }
        //создать палету. вызывается по кнопке 
        public async Task CreatePallete()
        {
            try
            {


                if (Job.JobState != JobStates.InWork && Job.JobState != JobStates.InWorkNoNum && Job.JobState != JobStates.CloseAndAwaitSend)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");

                if (Job.readyPallets.Exists(x => x.state == NumberState.Собирается))
                    throw new Exception("Предыдущая палета не верифицированна!\nСчитайте ручным сканером код предыдушей палеты для ее верификации прежде чем формировать новую!");

                //получить список коробов в палете 
                //List<PartAggSrvBoxNumber> 
                var boxes = Job.readyBoxes.Where(x => x.PalId < 0 && x.state == NumberState.Верифицирован).Take(Job.order1C.numBoxInPallet);//.ToList();

                if (boxes.Count() == 0)
                    throw new Exception("Ошибка печати: нет коробов для агрегации!");

                //посчитать количество
                int NumItemInPallet = 0;// boxes.Count() * job.order1C.numPacksInBox;// App.Settings.NumItemInPack;
                foreach (var b in boxes)
                {
                    NumItemInPallet += b.productNumbers.Count();
                }
                //создать новый палетный номер
                int Counter = 0;
                string num = Job.readyPallets.LastOrDefault()?.GS1SerialOrSSCC18;
                if (!string.IsNullOrEmpty(num))
                {
                    int tmp;
                    if (int.TryParse(num, out tmp))
                    {
                        Counter = tmp;
                        Counter++;
                    }
                }
                else { Counter = Job.palletStartNumber; }


                string manDate = Job.order1C.ManufactureDate.ToString("yyMMdd", CultureInfo.InvariantCulture);
                string ItemInBox = Job.order1C.numPacksInBox.ToString(CultureInfo.InvariantCulture);

                //добавить номера коробов
                PartAggSrvBoxNumber newPal = new PartAggSrvBoxNumber($"01{Job.GTIN}11{manDate}10{Job.lotNo}\u001d37{NumItemInPallet}\u001d21{Counter:D5}");

                //добавить налету
                newPal.state = NumberState.Собирается;
                Job.readyPallets.Add(newPal);
                int palId = Job.readyPallets.Count - 1;
                //добавить в палету короба
                foreach (PartAggSrvBoxNumber box in boxes)
                {
                    box.state = NumberState.VerifyAndPlaceToPalete;
                    box.PalId = palId;
                    newPal.productNumbers.Add(box.boxNumber);
                }

                Job.SaveOrder();
                systemState.ReadyPalleteCount = Job.readyPallets.Count(x => x.state == NumberState.Верифицирован);
                systemState.BoxNotInPallete = Job.readyBoxes.Count(x => x.state == NumberState.Верифицирован);

                //обновить дланные по коробам
                mainPage.UpdateBoxView();

                await PrintPallete();

                //автоматически верифицировать

                //VerifyPalletCode(newPal.boxNumber);

                ;
            }
            catch (Exception ex)
            {
                string msg = $"Ошибка формирования паллеты {ex.Message}";

                if (Dispatcher.CheckAccess())
                {
                    systemState.StatusText = msg;
                    systemState.StatusBackground = Brushes.Red;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        systemState.StatusText = msg;
                        systemState.StatusBackground = Brushes.Red;
                    });
                }
                Log.Write(msg);
            }
        }
        public bool VerifyPalletCode(string fullNumber)
        {
            try
            {


                PartAggSrvBoxNumber newPal = Job.readyPallets.FirstOrDefault(x => x.state == NumberState.Собирается);
                if (newPal == default)
                    throw new Exception("Нет палеты для верификации! Для верификации этикетки сформируйте палету!");

                if (string.IsNullOrEmpty(fullNumber))
                    throw new Exception("Нет данных для верификации! Считан некорректный код!");

                if (fullNumber.Length > 3)
                {
                    if (fullNumber.Substring(0, 3)?.Equals("]C1", StringComparison.Ordinal) == true)
                        fullNumber = fullNumber.Remove(0, 3);
                }

                if (!fullNumber.Equals(newPal.boxNumber))
                    throw new Exception("Нет данных для верификации! Считан некорректный код!");

                newPal.state = NumberState.Верифицирован;
                Job.SaveOrder();

                //перед применением контрольных кодов убедится в том что стоит режим оператор
                systemState.StatusText = "Палета №:" + newPal.GS1SerialOrSSCC18 + " успешно верифицирована.";
                systemState.StatusBackground = Brushes.Transparent;

                SetTimesAction(IoActions.Green, IoActions.RemoveGreen);

                systemState.ReadyPalleteCount = Job.readyPallets.Count(x => x.state == NumberState.Верифицирован);
                systemState.BoxNotInPallete = Job.readyBoxes.Count(x => x.state == NumberState.Верифицирован);


            }
            catch (Exception ex)
            {
                string msg = $"VerifyPalletCode {ex.Message}";

                if (Dispatcher.CheckAccess())
                {
                    systemState.StatusText = msg;
                    systemState.StatusBackground = Brushes.Red;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        systemState.StatusText = msg;
                        systemState.StatusBackground = Brushes.Red;
                    });
                }
                Log.Write(msg);
            }
            return false;
        }
        public async Task PrintPallete()
        {
            try
            {
                var pl = Job.readyPallets
                     .Select((value, index) => new { Value = value, Index = index })
                     .FirstOrDefault(x => x.Value.state == NumberState.Собирается);

                PartAggSrvBoxNumber? newPal = pl?.Value;
                int palletCounter = (pl?.Index+1) ?? -1;

                if (newPal == default)
                    throw new Exception("Нет палеты для печати! Для печати этикетки сформируйте палету!");

                string nettoPack = Job.order1C.packWeightGramm.ToString(CultureInfo.InvariantCulture);
                double boxWeight = (Job.order1C.packWeightGramm * Job.order1C.numPacksInBox);
                string nettoBox = (boxWeight / 1000).ToString("0.0", CultureInfo.InvariantCulture);


                //чифровой счетчик
                int tmp = 0;

                string palCount = "";

                if (int.TryParse(newPal.GS1SerialOrSSCC18, out tmp))
                {
                    //contNum = tmp;
                    palCount = tmp.ToString(CultureInfo.InvariantCulture);
                }

                //диапазон паллет
                string diapason = "";
                string firstFirst = newPal.productNumbers.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstFirst))
                {
                    string fbn = new GsLabelData(firstFirst)?.SerialNumber;
                    if (int.TryParse(fbn, out tmp))
                    {
                        diapason = $"{tmp}-";
                    }
                }

                string lastFirst = newPal.productNumbers.LastOrDefault();
                if (!string.IsNullOrEmpty(firstFirst))
                {
                    string fbn = new GsLabelData(lastFirst)?.SerialNumber;
                    if (int.TryParse(fbn, out tmp))
                    {
                        diapason += $"{tmp}";
                    }
                }
                GsLabelData ld = new GsLabelData(newPal.boxNumber);
                //посчитать вес палеты
                int ItemCount = 0;
                if (int.TryParse(ld.QuantityInParts, out tmp))
                {
                    ItemCount = tmp;
                }

                double nettoPalVal = ((double)(Job.order1C.packWeightGramm * ItemCount) / 1000);
                string nettoPal = nettoPalVal.ToString("0.0", CultureInfo.InvariantCulture);


                //new PartAggSrvBoxNumber($"01{job.gtin}11{manDate}10{job.lotNo}\u001d37{ItemInBox}\u001d21{Counter:D5}");
                string hCONTAINERCODE = $"(01){ld.GTIN}(11){ld.ProducerDate_JJMMDD}(10){ld.Charge_Number_Lot}(37){ld.QuantityInParts}(21){ld.SerialNumber}";

                byte[] data = TemplateDataGenerator.CreateTemplateDataMax(newPal.GS1SerialOrSSCC18, "XX", newPal.productNumbers.Count().ToString(CultureInfo.InvariantCulture),
                   Job.order1C.lotNo, CreateExpDateFix(Job.ExpDate, Job), Job.order1C.boxLabelFields, systemState.Packer, 1, CreateDateFix(Job.Date, Job),
                   nettoBox, nettoPack, nettoPal, Job.GTIN, newPal.GS1SerialOrSSCC18, palCount, diapason,
                   newPal.boxNumber, hCONTAINERCODE, ItemCount, nettoPalVal, palletCounter, _prnDataPrepare,"Pallet.tmpl");

                if (data == null)
                    throw new Exception("Ошибка формирования этикетки для подробностей смотрите лог!");

                bool result = false;

                if (noPrint)
                {
                    //создать директорию отчётов если её нет
                    string path = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\lastPrint.txt";
                    //
                    try
                    {
                        #region дата для найслейбл
                        string nspath = System.IO.Path.GetDirectoryName(
                                          System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\PrintData";

                        if (!Directory.Exists(nspath))
                            Directory.CreateDirectory(nspath);

                        nspath = nspath + "\\PrintData.csv";
                        if (File.Exists(nspath))
                            File.Delete(nspath);

                        //создать пакет данных для записи
                        string printData = $"{ld.GTIN};{ld.ProducerDate_JJMMDD};{ld.Charge_Number_Lot};{ld.QuantityInParts};{ld.SerialNumber}";
                        printData += $";{newPal.GS1SerialOrSSCC18};{newPal.productNumbers.Count()}";
                        printData += $";{Job.order1C.lotNo};{CreateExpDateFix(Job.ExpDate, Job)};{CreateDateFix(Job.Date, Job)}";
                        printData += $";{nettoBox};{nettoPack};{ld.SerialNumber}; {ItemCount};{nettoPalVal}";

                        //добавить поля из 1с
                        foreach (var f in Job.order1C.boxLabelFields)
                            printData += $";{f.FieldData}";

                        System.IO.File.WriteAllText(nspath, printData);
                        #endregion

                        //удалить предыдущий отчет если он какимто чудом есть
                        System.IO.File.Delete(path);
                        System.IO.File.WriteAllBytes(path, data);
                    }
                    catch (Exception ex)
                    {
                        string WriteStr = String.Format(CultureInfo.InvariantCulture, "Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
                        Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                    }


                    //bAgr.tests.testBoxCode tb = new tests.testBoxCode("]C100" + cBox.Number, WorckMode.None);
                    tests.testBoxCode tb = new tests.testBoxCode("]C100" + newPal.boxNumber, WorckMode.None);
                    //tests.testBoxCode tb = new tests.testBoxCode(newPal.boxNumber, WorckMode.None);

                    tb.ScanCompleted += Tb_ScanCompleted;
                    tb.Show();
                    result = true;
                }
                else
                {

                    result = await Task<bool>.Run(() =>
                    {
                        //for (int i = 0; i < job.numLabelAtBox; i++)
                        string ip = Settings.UsePalletPrinter ? Settings.PrinterPalleteIp : Settings.BoxPrinterIp;
                        int port = Settings.UsePalletPrinter ? Settings.PrinterPalletPort : Settings.BoxPrinterPort;


                        return Peripherals.PrinterBox.Print(data, ip, port);

                    });
                }

                if (!result)
                    throw new Exception("Не удалось сформировать палету! Устраните ошибки и повторите попытку");

            }
            catch (Exception ex)
            {
                string msg = $"Ошибка печати этикетки паллеты {ex.Message}";

                if (Dispatcher.CheckAccess())
                {
                    systemState.StatusText = msg;
                    systemState.StatusBackground = Brushes.Red;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        systemState.StatusText = msg;
                        systemState.StatusBackground = Brushes.Red;
                    });
                }
                Log.Write(msg);
            }
        }
        public void TestPrint()
        {
            System.ComponentModel.BackgroundWorker tmp1 = new System.ComponentModel.BackgroundWorker();
            tmp1.DoWork += delegate
            {
                try
                {
                    string fileName = System.IO.Path.GetDirectoryName(
                      System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

                    if (!System.IO.Directory.Exists(fileName))
                        System.IO.Directory.CreateDirectory(fileName);

                    if (Job.order1C?.urlLabelBoxTemplate?.Length > 0)
                    {
                        //загрузить шаблон для печати повторно
                        if (!DownLoadFile(Job.order1C.urlLabelBoxTemplate, fileName + "BoxTmp.tmpl"))
                            throw new Exception("Ошибка загрузки шаблона. url: " + Job.order1C.urlLabelBoxTemplate);

                        //проверить шаблон на содержание константы sscc18


                        //скопировать шаблон вместо текущего
                        System.IO.File.Copy(fileName + "BoxTmp.tmpl", fileName + "Box.tmpl", true);
                    }

                    byte[] data;
                    //проверить а мож но ли печаталь? 
                    if (Job.JobState == JobStates.Empty)
                    {
                        string path = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\BoxTst.tmpl"; ;


                        if (System.IO.File.Exists(path))
                        {
                            // загрузить шаблон для печати
                            data = System.IO.File.ReadAllBytes(path);
                            if (data == null)
                                throw new Exception("Ошибка чтения файла тестового шаблона.");
                        }
                        else
                            throw new Exception("Ошибка - нет файла тестового шаблона.");
                    }
                    else
                    {
                        //data = CreateTemplateDataMax("000000000000000000", "XX", "000", "TEST001", CreateExpDateFix(job.ExpDate),
                        //    job.boxLabelFields, "--", 1, CreateDateFix(job.Date),"4.1","400","80.1", job.gtin,
                        //    "000000","0","0-0000","(00)0000000000000000000000000000000000000",
                        //    "010000000000000000000000000000000000000000000000000000");// "00000000000");
                        //010000000000000011000000100000!102370000!102210000
                        string boxnumber = "000000000000000000";//$"010000000000000011{DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture)}100000\u001d37{Job.numРacksInBox}\u001d2100000";
                        string hCONTAINERCODE = "000000000000000000";// $"(01)00000000000000(11){DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture)}(10){0000}(37){Job.numРacksInBox}(21)00000";

                        string nettoPack = Job.order1C.packWeightGramm.ToString(CultureInfo.InvariantCulture);
                        double nettoBoxVal = ((double)(Job.order1C.packWeightGramm * Job.order1C.numPacksInBox) / 1000);
                        string nettoBox = nettoBoxVal.ToString("0.0", CultureInfo.InvariantCulture);

                        data = TemplateDataGenerator.CreateTemplateDataMax("000000000000000000", "XX", Job.numРacksInBox.ToString(CultureInfo.InvariantCulture),
                                        Job.order1C.lotNo, CreateExpDateFix(Job.ExpDate, Job), Job.order1C.boxLabelFields, "--", 1, CreateDateFix(Job.Date, Job),
                                        nettoBox, nettoPack, "-", Job.GTIN, "00000", "-", "-", boxnumber, hCONTAINERCODE, (Job.order1C.packWeightGramm * Job.order1C.numPacksInBox), nettoBoxVal,0, _prnDataPrepare);


                        #region дата для найслейбл
                        string nspath = System.IO.Path.GetDirectoryName(
                                          System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\PrintData";

                        if (!Directory.Exists(nspath))
                            Directory.CreateDirectory(nspath);

                        nspath = nspath + "\\PrintData.csv";
                        if (File.Exists(nspath))
                            File.Delete(nspath);

                        //создать пакет данных для записи
                        string printData = $"{Job.GTIN};201222;{Job.order1C.lotNo};1020;123918273";
                        printData += $";000000000001;123";
                        printData += $";{Job.order1C.lotNo};{CreateExpDateFix(Job.ExpDate, Job)};{CreateDateFix(Job.Date, Job)}";
                        printData += $";{nettoBox};{nettoPack};000000001; 401;{nettoBoxVal}";
                        //добавить поля из 1с
                        foreach (var f in Job.order1C.boxLabelFields)
                            printData += $"{f.FieldData}";

                        System.IO.File.WriteAllText(nspath, printData);
                        #endregion
                    }
                    /*
                     * byte[] data = CreateTemplateDataMax(cBox.Number, "XX", cBox.NumbersCount.ToString(),
                        job.order1C.lotNo, job.ExpDate, job.order1C.boxLabelFields, "--", 1, DateTime.Now.ToString(job.order1C.formatExpDate),job.gtin);
                     */




                    if (data != null)
                    {
                        //for (int i = 0; i < job.numLabelAtBox; i++)
                        Peripherals.PrinterBox.Print(data, Settings.BoxPrinterIp,
                            Settings.BoxPrinterPort);
                    }
                    else
                        throw new Exception("Ошибка формирования тестового шаблона.");
                }
                catch (Exception ex)
                {
                    systemState.StatusText = "Ошибка печати: " + ex.Message;
                    systemState.StatusBackground = Brushes.Red;
                }
            };
            tmp1.WorkerSupportsCancellation = true;
            tmp1.RunWorkerAsync();

        }
        //печать этикетки на принтер
        public void Print()
        {
            try
            {
                if (Job.boxQueue.Count < 1)
                {
                    systemState.StatusText = "Ошибка печати: нет данных для печати";
                    systemState.StatusBackground = Brushes.Red;
                    return;
                }

                BoxWithLayers cBox = Job.boxQueue.Peek();

                if (cBox == null)
                    throw new Exception("Нет короба");

                //проверить а мож но ли печаталь? 
                if (Job.JobState == JobStates.Complited)
                    throw new Exception("Нет задания");

                if (Job.JobState != JobStates.InWork)
                    throw new Exception($"{TextConstants.WorkTypeUp} не начата!!");

                //верифицировать номер коробки 
                if ((cBox.NumbersCount != Job.numРacksInBox) && (!closeAfterVerifyLastBox) && (!cBox.CloseNotFull))
                    throw new Exception("Короб не полный! Доложите упаковки или завершите агрегацию");

                CreateLabelAndPrint(cBox, Job, this);

            }
            catch (Exception ex)
            {
                systemState.StatusText = $"Ошибка печати: {ex.Message}";
                systemState.StatusBackground = Brushes.Red;
                return;
            }
        }
        public void CreateLabelAndPrint(BoxWithLayers cBox, JobController job, MainWindow mw)
        {
            try
            {
                //выделить поле серийника 
                GsLabelData ld = new GsLabelData(cBox.Number);
                string nettoPack = job.order1C.packWeightGramm.ToString(CultureInfo.InvariantCulture);

                double nettoBoxVal = (double)(job.order1C.packWeightGramm * cBox.NumbersCount) / 1000;
                string nettoBox = nettoBoxVal.ToString("0.00", CultureInfo.InvariantCulture);


                //скорректировать номер короба в коробе пачек не стандартное количество
                //if (cBox.NumbersCount != job.order1C.numPacksInBox)
                //{
                //    string newNum = ($"01{ld.GTIN}11{ld.ProducerDate_JJMMDD}10{ld.Charge_Number_Lot}\u001d37{cBox.NumbersCount}\u001d21{ld.SerialNumber}");
                //    //найти и заменить номер  в задании
                //    PartAggSrvBoxNumber rBox = job.readyBoxes.FirstOrDefault(x => x.boxNumber.Equals(cBox.Number));
                //    if (rBox != default)
                //    {
                //        rBox.boxNumber = newNum;
                //        cBox.Number = newNum;
                //        job.SaveOrder();
                //        ld = new GsLabelData(cBox.Number);
                //    }
                //}

                //получить порядковый номре короба в партии
                var rBox = job.readyBoxes
                     .Select((value, index) => new { Value = value, Index = index })
                     .FirstOrDefault(x => x.Value.boxNumber.Equals(cBox.Number));
               
                int boxCounter = (rBox?.Index + 1) ?? 0;

                //01{GTIN}11{дата производства}10{номер партии}37{кол-во едениц в коробе}21{сквозной счетчик в рамках партии 5 символов}
                //new PartAggSrvBoxNumber($"01{job.gtin}11{manDate}10{job.lotNo}\u001d37{ItemInBox}\u001d21{Counter:D5}");
                string hCONTAINERCODE = $"(01){ld.GTIN}(11){ld.ProducerDate_JJMMDD}(10){ld.Charge_Number_Lot}(37){ld.QuantityInParts}(21){ld.SerialNumber}";

                byte[] data = TemplateDataGenerator.CreateTemplateDataMax(cBox.Number, "XX", cBox.NumbersCount.ToString(CultureInfo.InvariantCulture),
                    job.order1C.lotNo, CreateExpDateFix(job.ExpDate, job), job.order1C.boxLabelFields, "--", 1, CreateDateFix(job.Date, job),
                    nettoBox, nettoPack, "-", job.GTIN, ld.SerialNumber, "-", "-", cBox.Number, hCONTAINERCODE, cBox.NumbersCount, nettoBoxVal, boxCounter, _prnDataPrepare);


                //если количество коробов включая этот достигло полной палеты то остановить линию если надо
                if (Settings.StopLineAfterPalletFull)
                {
                    if ((App.SystemState.BoxNotInPallete + 1) >= job.order1C.numBoxInPallet)
                    {
                        mw.StopLine();
                        App.SystemState.StatusText = "Линия остановлена так как собрана полная палета. Верифицируйте последний короб";
                        App.SystemState.StatusBackground = Brushes.DarkOrange;
                    }

                }

                if (mw.noPrint)
                {
                    mw.Dispatcher.Invoke(() =>
                    {
                        //создать директорию отчётов если её нет
                        string path = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\lastPrint.txt";


                        //
                        try
                        {


                            #region дата для найслейбл
                            string nspath = System.IO.Path.GetDirectoryName(
                                              System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\PrintData";

                            if (!Directory.Exists(nspath))
                                Directory.CreateDirectory(nspath);

                            nspath = nspath + "\\PrintData.csv";
                            if (File.Exists(nspath))
                                File.Delete(nspath);

                            //создать пакет данных для записи
                            string printData = $"{ld.GTIN};{ld.ProducerDate_JJMMDD};{ld.Charge_Number_Lot};{ld.QuantityInParts};{ld.SerialNumber}";
                            printData += $";{cBox.Number};{cBox.NumbersCount}";
                            printData += $";{job.order1C.lotNo};{CreateExpDateFix(job.ExpDate, job)};{CreateDateFix(job.Date, job)}";
                            printData += $";{nettoBox};{nettoPack};{ld.SerialNumber}; {cBox.NumbersCount};{nettoBoxVal}";

                            //добавить поля из 1с
                            foreach (var f in job.order1C.boxLabelFields)
                                printData += $"{f.FieldData}";

                            System.IO.File.WriteAllText(nspath, printData);
                            #endregion


                            //удалить предыдущий отчет если он какимто чудом есть
                            System.IO.File.Delete(path);
                            System.IO.File.WriteAllBytes(path, data);
                        }
                        catch (Exception ex)
                        {
                            string WriteStr = String.Format(CultureInfo.InvariantCulture, "Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
                            Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                        }

                        //просто номер как он есть в БД
                        tests.testBoxCode tb = new tests.testBoxCode(cBox.Number, WorckMode.None);

                        //sscc18
                        //tests.testBoxCode tb = new tests.testBoxCode("]C100" + cBox.Number, WorckMode.None);
                        
                        
                        //tests.testBoxCode tb = new tests.testBoxCode(cBox.Number, WorckMode.None);
                        tb.ScanCompleted += mw.Tb_ScanCompleted;
                        tb.Show();
                    });
                    return;
                }


                System.ComponentModel.BackgroundWorker tmp1 = new System.ComponentModel.BackgroundWorker();
                tmp1.DoWork += delegate
                {
                    try
                    {

                        if (data != null)
                        {
                            for (int i = 0; i < job.numLabelAtBox; i++)
                                Peripherals.PrinterBox.Print(data, Settings.BoxPrinterIp,
                                   Settings.BoxPrinterPort);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (mw != null)
                        {
                            App.SystemState.StatusText = "Ошибка печати: " + ex.Message;
                            App.SystemState.StatusBackground = Brushes.Red;
                        }
                    }
                };
                tmp1.WorkerSupportsCancellation = true;
                tmp1.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

        }
        private void Tb_ScanCompleted(string indata, WorckMode e)
        {
            if (Dispatcher.CheckAccess())
            {
                if (e == WorckMode.Left)
                    ProssedData(indata, WorckMode.Left);
                else
                    ProssedData(indata, WorckMode.Right);
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (e == WorckMode.Left)
                        ProssedData(indata, WorckMode.Left);
                    else
                        ProssedData(indata, WorckMode.Right);
                });
            }


        }



        private static bool DownLoadFile(string sUrl, string safePath)
        {
            try
            {

                if (sUrl == null)
                    throw new Exception("Ошибка шаблона");
                // Create a new WebClient instance.
                using (System.Net.WebClient myWebClient = new System.Net.WebClient())
                {
                    Uri url = new Uri(sUrl);
                    //**************авторизация***********
                    System.Net.NetworkCredential myNetworkCredential = new System.Net.NetworkCredential(
                        App.Settings.Srv1CLogin,
                        App.Settings.Srv1CPass);

                    System.Net.CredentialCache myCredentialCache = new System.Net.CredentialCache();
                    myCredentialCache.Add(url, "Basic", myNetworkCredential);

                    myWebClient.Credentials = myCredentialCache;
                    //*************************
                    // Download the Web resource and save it into the current filesystem folder.
                    myWebClient.DownloadFile(url, safePath);
                }
                return true;

            }
            catch (ArgumentNullException ex)
            {
                Log.Write("DownLoadFile  ArgumentNullException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (WebException ex)
            {
                if (ex.InnerException != null)
                    Log.Write("DownLoadFile  WebException\nОшибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОшибка источника " + ex.InnerException.Message + "\nОтвет сервера" + ex.Response,EventLogEntryType.Error, 701);
                else
                    Log.Write("DownLoadFile  WebException\nОшибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОтвет сервера" + ex.Response,EventLogEntryType.Error, 701);

            }
            catch (NotSupportedException ex)
            {
                Log.Write("DownLoadFile  NotSupportedException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (Exception ex)
            {
                Log.Write("DownLoadFile " + ex.Message,EventLogEntryType.Error, 701);
            }
            return false;
        }
        #endregion

        #region Авторизация https://remote.drgrp.ru:8443/FarmaKR/hs/InfoTech/Authorization
        //обрабатывает запросы на вход мастеров. наладчиков. контролеров
        private bool ParseAuthCode(string scanData)
        {

            //пример строки для ввода 
            //id#Мастер#123#
            //id#Мастер#123
            string[] data = scanData.Split('@');

            if (data == null)
                return false;
            if (data.Length < 2)
                return false;

            //перед применением контрольных кодов убедится в том что стоит режим оператор
            if (data[0] == "id")//мастер
            {
                enterUserEventHandler?.Invoke(data[1]);
                return true;
            }
            else if (data[0] == "ids")//отбор образцов
            {
                return false;
            }
            else if (data[0] == "idn")//наладчик
            {
                enterUserEventHandler?.Invoke(data[1]);
                return true;
            }

            return false;
        }
        private Autorization.User UserAuth(bool _onlyMaster, bool _onlyControl, bool _onlyServiceMen, bool authRepitEnable, Autorization.User _user, bool registerMaster = true)
        {
            //если авторизации не требуется
            if (!App.Settings.AuthorizationEnable)
            {
                _user = new Autorization.User() { ID = "1", IsMaster = true, IsServiceMen = true, IsControler = true, Name = "Не указан" };
                ChangeMaster(_user);
                systemState.Packer = "Мастер:\n" + _user.Name;// 41
                return _user;
            }
            PharmaLegacy.Windows.LogonWindow autWin = new PharmaLegacy.Windows.LogonWindow(_onlyMaster, _onlyControl, 
                _onlyServiceMen, authRepitEnable, _user,Settings.WindowTimeOut,Settings.Srv1CUrlAuthorize,Settings.Srv1CLogin,Settings.Srv1CPass);
            this.enterUserEventHandler += autWin.MainWindow_enterUserEventHandler;
            autWin.Owner = this;
            //запустить таймер контроля бездействия окна
            //windowTimeOut.Start();

            autWin.ShowDialog();

            this.enterUserEventHandler -= autWin.MainWindow_enterUserEventHandler;
            if (autWin.result == MessageBoxResult.Cancel)
                return null;

            //master = autWin.user;
            //user = autWin.user;
            if (registerMaster)
            {
                _user = autWin.user;
                ChangeMaster(autWin.user);
                systemState.Packer = "Мастер:\n" + _user.Name;// 41
            }
            return _user;
        }

        public bool ChangeMaster(Autorization.User user)
        {
            if (user == null)
            {
                Log.Write("Ошибка выбора мастера указан новый мастер:null ");
                return false;
            }

            Master = user;
            Log.Write("Новый мастер: " + Master.Name + " зарегистрирован на линии");

            if ((Job.JobState == JobStates.InWork) ||
                (Job.JobState == JobStates.New) ||
                (Job.JobState == JobStates.Paused))
            {
                if (Job.mastersArray == null)
                    Job.mastersArray = new List<Operator>();

                Operator op = new Operator(Master.ID);
                // добавить мастера если это новый 
                if (Job.mastersArray.Count > 0)
                {
                    if (Job.mastersArray.Last()?.id == op.id)
                    {
                        //если сессия еще открыта не начинать новую
                        if (Job.mastersArray.Last().endTime == null)
                            return true;
                    }
                    else //присвоить время окончания работы предыдущему мастер//if(job.mastersArray.Last().endTime == "")//DateTime.MinValue)
                        Job.mastersArray.Last().endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz", CultureInfo.InvariantCulture);
                }

                Job.mastersArray.Add(op);

                Job.SaveOrder();
            }

            return true;
        }
        public void CloseSession()
        {
            if (Job == null)
                return;

            if (Job.mastersArray == null)
                return;
            //присвоить время окончания работы предыдущему мастеру
            if (Job.mastersArray.Count > 0)
                Job.mastersArray.Last().endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz", CultureInfo.InvariantCulture);

            Job.SaveOrder();
        }
        #endregion

        //открыть станицу настроек
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {

            if (UserAuth(true, false, true, true, Master, false) == null)
                return;

            //serviceMen = autWin.user;

            e.Handled = true;
            //обновить данные по настройкам
            settingsPage.PackCounter = 0;
            settingsPage.PackInBox = Job.numРacksInBox;
            settingsPage.BoxCounter = 0;
            settingsPage.BrackCount = 0;
            settingsPage.SampleCount = 0;


            settingsPage.PrinterPort = Settings.BoxPrinterIp;
            settingsPage.PrinterStatus = "Не известно";
            // settingsPage.CameraPort = bAgr.Properties.Settings.Default.IpVision;
            settingsPage.CameraStatus = systemState.CamConnect ? "Подключён" : "Не подключён";
            settingsPage.SerialPort232Name = Settings.SerialPort232Name;
            settingsPage.StatusSerialPort232Name = "xxxxxx";
            settingsPage.HandSerialPort232Name = Settings.HandSerialPort232Name;
            settingsPage.StatusHandSerialPort232Name = "xxxx";
            //settingsPage.LayerFailCount = bAgr.Properties.Settings.Default.LayerFailCount;
            settingsPage.MaxPackInOneScanMode = Settings.MaxPackInOneScanMode;
            settingsPage.BoxFullPercentToStop = Settings.BoxFullPercentToStop;
            settingsPage.MinBoxNumberBeforeWarning = Settings.MinBoxNumberBeforeWarning;
            settingsPage.Version = "Версия: " + systemState.ver;
            settingsPage.PalletAutoCreate = Settings.PalletAutoCreate;
            settingsPage.StopLineAfterPalletFull = Settings.StopLineAfterPalletFull;

            settingsPage.BoxGrid = Settings.BoxGrid;
            settingsPage.NumRows = Settings.NumRows;
            settingsPage.NumColumns = Settings.NumColumns;
            settingsPage.BoxHeight = Settings.BoxHeight;
            settingsPage.BoxWidth = Settings.BoxWidth;
            //при присвоении контекста обновятся все переменные на екране. 
            settingsPage.DataContext = settingsPage;


            MainFrame.Navigate(settingsPage);
            BannerGrid.Visibility = Visibility.Hidden;
            CloseApp.Visibility = Visibility.Visible;

            //очистить панель сообшений
            ClearMsgPane();

            return;
        }

        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                //Device.Trigger(VisionSDK.Afterward.DoNothing());
                object o = e.OriginalSource;
                if (this.WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            }
        }

        private void BtnUserLogon_Click(object sender, RoutedEventArgs e)
        {
            // GetMoreBoxNumber(2);
            // SetAction(new IoAction[2] { IoActions.Stop, IoActions.Red });
            //
            //  SetTimesAction(IoActions.Red, IoActions.RemoveRed);

            if (UserAuth(false, false, false, false, Master) == null)
                return;

        }

        private void BtnNewPalNumbers_Click(object sender, RoutedEventArgs e)
        {
            //имитация чтения кода короба
            BoxWithLayers b = Job.boxQueue.Peek();
            VerifyBoxCode("xxxxx" + b.Number, WorckMode.Left);
            /*  if (job.lBox.Numbers.Count>0)
                  VerifyBoxCode("xxxxx"+job.lBox.Number);

              if (job.rBox.Numbers.Count > 0)
                  VerifyBoxCode("xxxxx" + job.rBox.Number);
                  */


        }

        private void MainWindow1_Deactivated(object sender, EventArgs e)
        {
            if (!topMost)
                return;

            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));

        }


        // Touch event window message constants [winuser.h]
        private const int WM_TOUCHMOVE = 0x0240;
        private const int WM_TOUCHDOWN = 0x0241;
        private const int WM_TOUCHUP = 0x0242;

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        private const int TOUCHEVENTF_MOVE = 0x0001;
        private const int TOUCHEVENTF_DOWN = 0x0002;
        private const int TOUCHEVENTF_UP = 0x0004;
        private const int TOUCHEVENTF_INRANGE = 0x0008;
        private const int TOUCHEVENTF_PRIMARY = 0x0010;
        private const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        private const int TOUCHEVENTF_PEN = 0x0040;

        // Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
        private const int TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001; // the dwTime field contains a system generated value
        private const int TOUCHINPUTMASKF_EXTRAINFO = 0x0002; // the dwExtraInfo field is valid
        private const int TOUCHINPUTMASKF_CONTACTAREA = 0x0004; // the cxContact and cyContact fields are valid

        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TOUCHUP)
            {
                //DecodeTouch(wParam, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }


    }

}
