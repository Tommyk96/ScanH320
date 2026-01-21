using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.Configure;
using BoxAgr.DAL;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using Util;

namespace BoxAgr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IAppMsg
    {

        private static Config config = null!;
        public static Config Settings
        {
            get
            {
                if (config == null)
                    config = Config.Load();

                return config;
            }
        }


        private static LocalSystemState systemState = null!;
        public static LocalSystemState SystemState
        {
            get
            {
                if (systemState == null)
                    systemState = new LocalSystemState();

                return systemState;
            }
        }


        private static JobController job = null!;
        public static JobController Job
        {
            get
            {
                if (job == null)
                    job = JobController.RestoreOrder(config);

                return job;
            }
        }


        static AppDbContext database = null!;
        public static AppDbContext Database
        {
            get
            {
                database ??= new();
                return database;
            }
        }
        static JsonDbContext jsonDbContext = null!;
        public static JsonDbContext JsonDbContext
        {
            get
            {
                jsonDbContext ??= new(Job);
                return jsonDbContext;
            }
        }

        public static MainWindow MainForm => Current.MainWindow as MainWindow;


        public App() : base()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            Settings.AggregateOn.ToString();

            #region проверка лицензии
            //if(!License.License.IsLicenseValid(out string machineId))
            //{
            //    MessageBox.Show("Лицензионный ключ отсутствует или неверен. HardwareId: " + machineId + "\nПриложение будет закрыто.");
            //    this.Shutdown();
            //    return;
            //}
            #endregion

            //для дебага     
            try
            {    
                
                
                
                

                //string s = System.IO.File.ReadAllText("111\\3.json");

                //BiotikiReport br = FSerialization.Archive.DeserializeJSon<BiotikiReport>(s);
                //SerishevoReport r = new();


                //r.id = br.id;
                //r.startTime = br.startTime;
                //r.endTime = br.endTime;

                ////добавить обработанные коды
                //// Выбираем только часть строк, начиная с 16-го символа, и объединяем их в один список
                //List<string> readyProductNumbers = br.readyBox
                //    .SelectMany(box => box.productNumbers.Select(pn => pn[16..]))
                //    .ToList();

                //// Используем LINQ для выбора уникальных значений
                //List<string> uniqueProductNumbers = readyProductNumbers.Distinct().ToList();

                ////проверка на корректность
                //if (readyProductNumbers.Count != uniqueProductNumbers.Count)
                //    Log.Write($"При формировании отчета обнаружены коды продукта с одинаковыми номерами!");


                //r.Packs.AddRange(uniqueProductNumbers);
                //r.defectiveCodes = [];
                //r.operators = [];
                //string json = Archive.SerializeJSon<SerishevoReport>(r);
                //System.IO.File.WriteAllText($"111\\{r.id}.json", json);
                //json.ToString();

            }           
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
                System.Threading.Tasks.Task.Delay(100).Wait();
            }

            #region Защита от повторного запуска
            ///////////////////////////////////////////////
            bool firstInstance;
            Mutex mutex = new Mutex(false, "bdfda8c5-e86a-4fae-8547-cf5e86a776c1", out firstInstance);
            if (!firstInstance)
            {
                MessageBox.Show("Программа уже запущена!");
                this.Shutdown();
                return;
            }

            if (IsApplicationAlreadyRunning())
            {
                MessageBox.Show("Программа уже запущена!");
                this.Shutdown();
                return;
            }
            #endregion

        }

        static bool IsApplicationAlreadyRunning()
        {
            string proc = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(proc);
            if (processes.Length > 1)
                return true;
            else
                return false;
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            e.Handled = true;
            //создать дамп
            string fileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Logs";

            //создать директорию если надо
            if (!System.IO.Directory.Exists(fileName))
                System.IO.Directory.CreateDirectory(fileName);


            fileName += "\\dump-" + DateTime.Now.ToString("dd.MM.yyyy hh_mm", CultureInfo.InvariantCulture) + ".dmp";

            //если есть InnerException то надо сохранить его как основной
            try
            {

                using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Write))
                {
                    Util.MiniDump.Write(fs.SafeFileHandle, Util.MiniDump.Option.WithFullMemory, Util.MiniDump.ExceptionInfo.Present);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            string errorMessage = string.Format(CultureInfo.InvariantCulture, "Критическая ошибка: {0}. Создан дамп: {1}", e.Exception.Message, fileName);
            Util.Log.Write("Критическая ошибка программы, . Создан дамп " + fileName);

            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region IAppMsg
        public void ClearMsgBelt()
        {
            try
            {
                if (Application.Current?.Dispatcher.CheckAccess() == true)
                {
                    MainForm?.ClearMsgBelt();
                }
                else
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MainForm?.ClearMsgBelt();
                    });
                }

            }
            catch (Exception ex)
            {
                Log.Write("APP", ex.Message, EventLogEntryType.Error, 87);
            }
        }
        public void ShowMessageOnUpBanner(string moduleId, string msgData, EventLogEntryType errType, int eventId)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                MainForm?.ShowMessage(msgData, errType);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainForm?.ShowMessage(msgData, errType);
                });
            }
            Log.Write(moduleId, msgData, errType, eventId);
        }
        #endregion

    }
}
