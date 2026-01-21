using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.Configure;
using FSerialization;
using PharmaLegaсy.Interfaces;
using PharmaLegaсy.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Util;

namespace BoxAgr.Pages
{
    /// <summary>
    /// Логика взаимодействия для ImageBoxPage.xaml
    /// </summary>
    public partial class ImageBoxPage : Page, IMainPage
    {
        private readonly IMainFrame owner;
        private static LocalSystemState systemState => App.SystemState;
        private static Config Settings => App.Settings;
        private static JobController _job => App.Job;
        public ImageBoxPage()
        {
            InitializeComponent();
            owner = new MainWindow();
        }

        public ImageBoxPage(IMainFrame o)
        {
            InitializeComponent();
            owner = o;
            systemState.ScanImgShow = Settings.ScanImgShow;
            DataContext = systemState;
            ShowEmptyMatrix();
        }
        public void SetStop(bool Checked)
        {
        }
        public void UpdateView()
        {
        }
        public void UpdateBoxView()
        {
            try
            {


                if (Dispatcher.CheckAccess())
                {
                    ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                    foreach (PartAggSrvBoxNumber itm in _job.readyBoxes)
                    {
                        if (itm.state == NumberState.Верифицирован || itm.state == NumberState.VerifyAndPlaceToReport)
                            pbView.Add(new SerialCode(itm.GS1SerialOrSSCC18[6..])); //(itm.boxNumber));
                    }
                    systemState.ReadyBoxCount = _job.readyBoxes.Count(x => x.state == NumberState.Верифицирован
                    || x.state == NumberState.VerifyAndPlaceToReport
                    || x.state == NumberState.VerifyAndPlaceToPalete);  //pbView.Count;

                    systemState.ReadyProductCount = _job.GetVerifyProductCount();

                    systemState.ProcessedBoxes = pbView;
                    lvBox.ScrollIntoView(pbView.LastOrDefault());
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                        foreach (PartAggSrvBoxNumber itm in _job.readyBoxes)
                        {
                            if (itm.state == NumberState.Верифицирован || itm.state == NumberState.VerifyAndPlaceToReport)
                                pbView.Add(new SerialCode(itm.GS1SerialOrSSCC18)); //(itm.boxNumber));
                        }
                        systemState.ReadyBoxCount = _job.readyBoxes.Count(x => x.state == NumberState.Верифицирован
                       || x.state == NumberState.VerifyAndPlaceToReport
                       || x.state == NumberState.VerifyAndPlaceToPalete);  //pbView.Count;

                        systemState.ReadyProductCount = _job.GetVerifyProductCount();

                        systemState.ProcessedBoxes = pbView;
                        lvBox.ScrollIntoView(pbView.LastOrDefault());
                    });
                }

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
            }
        }
        //delete all *.bpm files in dir exclude excludeFile
        private static void ClearBmpDir(string dir, string excludeFile,string searchPatern = "*.bmp")
        {
            string[] files = Directory.GetFiles(dir, searchPatern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (file.Contains(excludeFile)) continue;
                File.Delete(file);
            }
        }
   

        //gets information about the time of creation of all files of bmp type in the dir directory and sorts them by date in descending order and return last file name
        private static string GetImgPath(string dir,string searchPatern = "*.bmp", long horizon = 5000)
        {
         
           // return ImageStorage.FindLatestBmpFile(dir,2000);
            string[] files = Directory.GetFiles(dir, searchPatern, SearchOption.AllDirectories);
            if (files.Length == 0)
                return string.Empty;

            Array.Sort(files, (x, y) => DateTime.Compare(File.GetLastWriteTimeUtc(x), File.GetLastWriteTimeUtc(y)));
            string LastLayerFileName = files[files.Length - 1];

            //
            foreach (var file in files)
            {
                try
                {
                    if (file.Contains(LastLayerFileName)) continue;
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                   // Log.Write(ex.Message);
                }
            }

           
            //проверить горизонт
            DateTime horizonTime = DateTime.Now.AddMilliseconds(-horizon);//DateTime.Now.AddHours(-horizon); 
            DateTime lastTime = File.GetLastWriteTime(LastLayerFileName);
            if (lastTime >= horizonTime)
                return LastLayerFileName;

            Log.Write($"last img:{lastTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} - Horizon :{horizonTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} -  {LastLayerFileName}");

            return string.Empty;
            
        }
        private static void ClearViewbox(Viewbox viewbox)
        {
            if (viewbox is null)
                return;

            if(viewbox.Child is Canvas can)
                can.Children?.Clear();

            viewbox.Child = null;
        }
        public void BlurImage()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (BoxMatrix.Child is Canvas c)
                {
                    c.Effect = new BlurEffect() { Radius = 60 };
                }
            }));
            Task.Delay(10).Wait();
        }
        public void AddLayer(int id,int layer, bool manualAdd, BoxAddStatus state, Unit[] barcodes, BoxWithLayers box)
        {
            try
            {
                Unit? u = barcodes.FirstOrDefault();
                string imgFile = string.Empty;

                //очистить экран 
                Dispatcher.Invoke(new Action(() =>
                {
                    //BoxMatrix.Child = null;
                    if (BoxMatrix.Child is Canvas c)
                    {
                        c.Effect = new BlurEffect () { Radius = 60 };
                    }
                    //GC.Collect();
                }));
                Task.Delay(10).Wait();

                // ждем пока загрузиться картинка это пару секунд
                if (Settings.ScanImgShow && u?.CodeState != CodeState.ManualAdd)
                {
                   Task.Run(() => 
                    {  
                        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                        while (sw.ElapsedMilliseconds < Settings.ScanImgTimeout)
                        {
                            try
                            {

                                //ищем файл картинки и проверяем его на возможность открытия.
                                //тоесть что он уже записался целиком     horizon:3000 - int.MaxValue
                                long horizon = 3000;
#if DEBUG
                                horizon = 9203685477580;// long.MaxValue;
#endif
                                if (GetImgPath(Settings.ScanImgPath, "*.jpg", horizon: horizon) is string file)
                                {
                                    //Log.Write($"{Settings.ScanImgPath} найден файл {imgFile}");
                                    imgFile = file;
                                    using FileStream f = File.Open(imgFile, FileMode.Open);
                                    f.Close();
                                    break;
                                }

                                 Task.Delay(100).Wait(); //Log.Write($"{Settings.ScanImgPath} найден файл не найден.  ");
                            }
                            catch (IOException ex)
                            {
                                if (ex.HResult != -2147024864)
                                    Log.Write(ex.ToString());

                                 Task.Delay(100).Wait();
                                
                            }
                            catch (Exception ex)
                            {
                                Log.Write(ex.ToString());
                                System.Threading.Tasks.Task.Delay(100).Wait();
                            }
                        }
                        sw.Stop();
                    }).Wait();
                }

                //отбрасываем ошибки
                if (state == BoxAddStatus.LogicError || state == BoxAddStatus.Defected)
                {
                    //попытатся остановить чтение и вывести матрицу ошибки
                    Dispatcher.Invoke(new Action(() =>
                    {
                        owner?.StopLine();

                        

                        BoxMatrix.Child = null;
                        BoxMatrix.Child = MatrixController.DrawGridWithImg(barcodes.ToArray(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows,
                            Settings.NumColumns, imgFile, Settings.ScanImgShow,App.Settings.Angle);
                        //обновить данные статистики
                        systemState.ReadCodeCount = barcodes.Length;
                        systemState.GoodLayerCodeCount = barcodes.Where(x => x.CodeState == FSerialization.CodeState.Verify).Count();
                        systemState.BoxNumber = box.Number;

                        GC.Collect();
                    }));
                    return;
                }
                
                //ничего не делать если массив пуст
                //if (barcodes.Length < 1)
                //    return;

                //сообщить о успешном сборе слоя
                if (state == BoxAddStatus.LayerFull)
                    owner.ShowMessage($"Слой номер {layer} собран! ", EventLogEntryType.Information);

                //закрыть короб
                if (state == BoxAddStatus.BoxFull)
                {
                    owner.CloseFullBox(autoVerify:true);
                    //return;
                }

                //запустить отрисовку  короба
                Dispatcher.Invoke(new Action(() =>
                {
                    // BoxMatrix.Child = null;

                    //state == BoxAddStatus.LayerFull ||
                    if (state == BoxAddStatus.Uncknow)
                    {
                        ClearViewbox(BoxMatrix);
                        BoxMatrix.Child = MatrixController.DrawGridWithImg(Array.Empty<Unit>(), Settings.BoxWidth, Settings.BoxHeight, 
                            Settings.NumRows, Settings.NumColumns, imgFile, Settings.ScanImgShow, App.Settings.Angle);
                        systemState.ReadCodeCount = 0;
                        systemState.GoodLayerCodeCount = 0;
                        //systemState.LayerCount = box.LayerNum;
                        systemState.BoxNumber = box.Number;
                    }
                    else
                    {
                        if (state == BoxAddStatus.BoxFull)
                        {
                            if (!manualAdd)
                            {
                                ClearViewbox(BoxMatrix);
                                BoxMatrix.Child = MatrixController.DrawGridWithImg(barcodes.ToArray(), Settings.BoxWidth, Settings.BoxHeight,
                                    Settings.NumRows, Settings.NumColumns, imgFile, Settings.ScanImgShow, App.Settings.Angle);
                            }

                            systemState.GoodLayerCodeCount = barcodes.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                                                                            x.CodeState == FSerialization.CodeState.ManualAdd).Count();
                        }
                        else if (state == BoxAddStatus.LayerFull)
                        {
                            if (!manualAdd)
                            {
                                ClearViewbox(BoxMatrix);
                                BoxMatrix.Child = MatrixController.DrawGridWithImg(barcodes.ToArray(), Settings.BoxWidth, Settings.BoxHeight,
                                    Settings.NumRows, Settings.NumColumns, imgFile, Settings.ScanImgShow, App.Settings.Angle);
                            }

                            systemState.GoodLayerCodeCount = barcodes.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                                                                            x.CodeState == FSerialization.CodeState.ManualAdd).Count();

                        }
                        else
                        {

                            if (Settings.ScanImgShow && u?.CodeState == CodeState.ManualAdd)
                            {
                                ;
                            }
                            else
                            {
                                ClearViewbox(BoxMatrix);
                                BoxMatrix.Child = MatrixController.DrawGridWithImg(box.cLayer.ToArray(), Settings.BoxWidth, Settings.BoxHeight,
                                    Settings.NumRows, Settings.NumColumns, imgFile, Settings.ScanImgShow, App.Settings.Angle);
                            }

                                systemState.GoodLayerCodeCount = box.cLayer.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                                                                               x.CodeState == FSerialization.CodeState.ManualAdd).Count();
                            
                            // systemState.LayerCount = box.LayerNum;
                        }
                        //обновить данные статистики
                        systemState.ReadCodeCount = barcodes.Length;
                        systemState.LayerCount = box.LayerNum > 0 ? box.LayerNum : 1;
                        systemState.BoxNumber = box.Number;
                    }

                    GC.Collect();
                }));
            }
            catch (Exception ex)
            {
                owner.ShowMessage(ex.Message, EventLogEntryType.Error);
                Log.Write(ex.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                //восстановить если нужно
                //Dispatcher.Invoke(new Action(() =>
                //{
                //    BoxMatrix.Child = null;
                //    GC.Collect();
                //}));
                //Task.Delay(10).Wait();
            }

        }
        public void ShowEmptyMatrix()
        {

            BoxMatrix.Child = null;
            //BoxMatrix.Child = MatrixController.DrawGrid(Array.Empty<Unit>(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
            systemState.LayerCount = systemState.LayerCount > 0 ? systemState.LayerCount : 1;
            GC.Collect();
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (owner.ClearBox())
            {
                ShowEmptyMatrix();
            }
        }
        private void btnBrack_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.BtnBrack();
        }
        private void btnReprint_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner?.ReprintEvent();
        }
        private void btnSample_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.BtnSample();
        }
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.BtnHelp();
        }
        private void btnCLoseBox_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.btnCloseBox(btnStop.IsChecked == true);
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShowEmptyMatrix();

            if (owner?.StartLine() != true)
            {
                if (sender is ToggleButton b)
                    b.IsChecked = false;
            }
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner?.StopLine();
        }
        private void btnRight_Click(object sender, RoutedEventArgs e)
        {

            e.Handled = true;

            if (btnRight.IsChecked == true)
                systemState.CurentMode =
                    (systemState.CurentMode == WorckMode.Left ? WorckMode.Both : WorckMode.Right);
            else
                systemState.CurentMode =
                   (systemState.CurentMode == WorckMode.Both ? WorckMode.Left : WorckMode.None);


        }
        private void Checker3_Click(object sender, RoutedEventArgs e)
        {
            Settings.ScanImgShow = systemState.ScanImgShow;
            Settings.Save();
        }


        private void btnTestGen_Click(object sender, RoutedEventArgs e)
        {
            //загрузить данные 
            //string cfgFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + @"\testData\";
            //string scData = File.ReadAllText(cfgFileName+ "scanData");

            owner.TestFunc().ConfigureAwait(false);
            e.Handled = true;

            ;
        }



    }
}
