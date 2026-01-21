using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.Configure;
using PharmaLegaсy.Interfaces;
using PharmaLegaсy.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Util;

namespace BoxAgr.Pages
{
    /// <summary>
    /// Логика взаимодействия для MainPageMatrix.xaml
    /// </summary>
    public partial class MainPageMatrix : Page, IMainPage
    {
        private readonly IMainFrame owner;
        private static LocalSystemState systemState => App.SystemState;
        private static Config Settings => App.Settings;

        public MainPageMatrix()
        {
            InitializeComponent();
            owner = new MainWindow();
           
        }

        public MainPageMatrix(IMainFrame o)
        {
            InitializeComponent();
            owner = o;
            DataContext = systemState;
            ShowEmptyMatrix();
        }
        public void SetStop(bool Checked)
        {
        }
        public void BlurImage() { }
        public void UpdateView()
        {

        }
        public void UpdateBoxView()
        {

        }
        public void AddLayer(int id,  int layer, bool manualAdd, BoxAddStatus state, Unit[] barcodes, BoxWithLayers box)
        {
            try
            {
                //отбрасываем ошибки
                if (state == BoxAddStatus.LogicError || state == BoxAddStatus.Defected)
                {
                    //попытатся остановить чтение и вывести матрицу ошибки
                    Dispatcher.Invoke(new Action(() =>
                    {
                        owner?.StopLine();


                        BoxMatrix.Child = null;
                        BoxMatrix.Child = MatrixController.DrawGrid(barcodes, Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
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
                    owner.CloseFullBox(autoVerify: false);
                    //return;
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    BoxMatrix.Child = null;
                    //state == BoxAddStatus.LayerFull ||
                    if (state == BoxAddStatus.Uncknow)
                    {
                        BoxMatrix.Child = MatrixController.DrawGrid(Array.Empty<Unit>(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
                        systemState.ReadCodeCount = 0;
                        systemState.GoodLayerCodeCount = 0;
                        //systemState.LayerCount = box.LayerNum;
                        systemState.BoxNumber = box.Number;
                    }
                    //else if (state == BoxAddStatus.HandLayerChange)
                    //{
                    //    BoxMatrix.Child = MatrixController.DrawGrid(box.cLayer.ToArray(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
                    //    //обновить данные статистики
                    //    systemState.ReadCodeCount = barcodes.Length;
                    //    systemState.GoodLayerCodeCount = box.cLayer.Where(x => x.CodeState == FSerialization.CodeState.Verify || 
                    //    x.CodeState == FSerialization.CodeState.ManualAdd).Count();
                    //    systemState.BoxNumber = box.Number;
                    //}
                    else
                    {
                        if ( state == BoxAddStatus.BoxFull)
                        {
                            BoxMatrix.Child = MatrixController.DrawGrid(barcodes, Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
                            //systemState.GoodLayerCodeCount = box.Numbers.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                            //                                                x.CodeState == FSerialization.CodeState.ManualAdd).Count();
                            systemState.GoodLayerCodeCount = barcodes.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                                                                            x.CodeState == FSerialization.CodeState.ManualAdd).Count();
                        }
                        else if (state == BoxAddStatus.LayerFull )
                        {
                            BoxMatrix.Child = MatrixController.DrawGrid(barcodes, Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
                            systemState.GoodLayerCodeCount = barcodes.Where(x => x.CodeState == FSerialization.CodeState.Verify ||
                                                                            x.CodeState == FSerialization.CodeState.ManualAdd).Count();

                        }
                        else
                        {
                            BoxMatrix.Child = MatrixController.DrawGrid(box.cLayer.ToArray(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
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
            }catch (Exception ex) 
            {
                owner.ShowMessage(ex.Message, EventLogEntryType.Error);
                Log.Write(ex.ToString(),EventLogEntryType.Error );
            }
        }
        public void ShowEmptyMatrix()
        {

            BoxMatrix.Child = null;
            BoxMatrix.Child = MatrixController.DrawGrid(Array.Empty<Unit>(), Settings.BoxWidth, Settings.BoxHeight, Settings.NumRows, Settings.NumColumns);
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
        private  void btnStart_Click(object sender, RoutedEventArgs e)
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
        private void btnTestGen_Click(object sender, RoutedEventArgs e)
        {

            e.Handled = true;
            owner.ProssedData(Helper.RandomMilkPackNum(systemState.GTIN), WorckMode.Right);


        }      
        private void btnStart_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (sender is ToggleButton b)
            //{
            //    if (b.IsChecked == true)
            //    {
            //        e.Handled = true;
            //        return;
            //    }

            //    if (owner?.StartLine() != true)
            //    {
            //        e.Handled = true;
            //        return;
            //    }

            //    b.IsChecked = true;
            //}
        }
    }
}
