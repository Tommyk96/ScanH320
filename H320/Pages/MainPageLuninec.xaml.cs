
using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using FSerialization;
using PharmaLegaсy.Interfaces;
using PharmaLegaсy.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Util;

namespace BoxAgr.Pages
{
    /// <summary>
    /// Логика взаимодействия для MainPageLuninec.xaml
    /// </summary>
    public partial class MainPageLuninec : Page , IMainPage
    {

        //
        public string PageId { get; set; } 

        private readonly IMainFrame owner;

        private static LocalSystemState systemState => App.SystemState;
        private static JobController _job => App.Job;
        //
       

        public MainPageLuninec(IMainFrame o)    
        {
            InitializeComponent();
            owner = o;
            PageId = "2";
            DataContext = systemState;

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
        }
        public void BlurImage() { }


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
                            pbView.Add(new SerialCode(itm.GS1SerialOrSSCC18)); //(itm.boxNumber));
                    }
                    systemState.ReadyBoxCount = _job.readyBoxes.Count(x => x.state == NumberState.Верифицирован
                    || x.state == NumberState.VerifyAndPlaceToReport
                    || x.state == NumberState.VerifyAndPlaceToPalete);  //pbView.Count;
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
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {

        }
        public void AddLayer(int id, int layer,bool manualAdd, BoxAddStatus state, Unit[] barcodes, BoxWithLayers box)
        {
            //отбрасываем ошибки
            if (state == BoxAddStatus.LogicError || state == BoxAddStatus.Defected || state == BoxAddStatus.Uncknow)
                return;

            Dispatcher.Invoke(new Action(() =>
            {
                //закрыть короб
                if (state == BoxAddStatus.BoxFull)
                {
                    owner.CloseFullBox(autoVerify: false);
                    return;
                }

                //сообщить о успешном сборе слоя
                if (state == BoxAddStatus.LayerFull)
                    owner.ShowMessage($"Слой номер {layer} собран! ", EventLogEntryType.Information);


                if (systemState.ListCurrentSerials != null)
                {

                    foreach (Unit u in barcodes)
                        systemState.ListCurrentSerials.Insert(0, new SerialCode(u.Number));

                    while (systemState.ListCurrentSerials.Count > 10)
                        systemState.ListCurrentSerials.RemoveAt(systemState.ListCurrentSerials.Count - 1);


                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", box.NumbersCount, systemState.PackInBox); //"запустить обновление"; //

                }
                else
                {
                    ObservableCollection<SerialCode> pbView = new ObservableCollection<SerialCode>();
                    foreach (Unit itm in barcodes)
                    {
                        pbView.Add(new SerialCode(itm.Number));
                    }
                    systemState.ListCurrentSerials = pbView;
                    systemState.SerialInBoxCounter = string.Format(CultureInfo.InvariantCulture, "Штук {0} из {1} ", box.NumbersCount, systemState.PackInBox); //"запустить обновление"; //

                }
            }));
        }
        public void UpdateView()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvNumber.ItemsSource);
            view.Refresh();
        }
        public void ShowEmptyMatrix() { }
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void btnReprint_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner?.ReprintEvent();
            //owner.Print();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.ClearBox();
        }
        //брак
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            owner.BtnBrack();
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

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            //owner.ChangeMode(1,btnLeft.IsChecked == true);
            e.Handled = true;

            if (btnLeft.IsChecked == true)
                systemState.CurentMode =
                    (systemState.CurentMode == WorckMode.Right ? WorckMode.Both : WorckMode.Left);
            else
                systemState.CurentMode =
                   (systemState.CurentMode == WorckMode.Both ? WorckMode.Right : WorckMode.None);

            //передать в плк настройки линии 
            //if (owner.plcS71200 != null)
            //    owner.plcS71200.DelayedCommand.FlowAEnable = btnLeft.IsChecked == true;

        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            //owner.ChangeActiveScanner(2);
            //owner.ChangeMode(2, btnRight.IsChecked == true);
            e.Handled = true;

            if (btnRight.IsChecked == true)
                systemState.CurentMode =
                    (systemState.CurentMode == WorckMode.Left ? WorckMode.Both : WorckMode.Right);
            else
                systemState.CurentMode =
                   (systemState.CurentMode == WorckMode.Both ? WorckMode.Left : WorckMode.None);

            //передать в плк настройки линии 
            //if (owner.plcS71200 != null)
            //    owner.plcS71200.DelayedCommand.FlowBEnable = btnRight.IsChecked == true;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

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

        public void SetStop(bool Checked)
        {

            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    btnStop.IsChecked = Checked;
                });
            }
            catch (Exception ex)
            {
                Log.Write("MW." + System.Threading.Thread.CurrentThread.ManagedThreadId + ".10:" + ex.Message,EventLogEntryType.Error, 10);
            }
        }

        private void btnStart_TouchDown(object sender, TouchEventArgs e)
        {

        }

        private void btnStart_TouchUp(object sender, TouchEventArgs e)
        {
            //btnStart_Click(sender, null);
        }

        private async void btnCreatePallete_Click(object sender, RoutedEventArgs e)
        {
            await owner.CreatePallete();
        }

        private async void btnReprintPallete_Click(object sender, RoutedEventArgs e)
        {
            await owner.PrintPallete();
        }

        private  void btnGenManScan_Click(object sender, RoutedEventArgs e)
        {
            owner.ProssedData($"01{systemState.GTIN}215ZOTi8hucu5Ba93vvvvvvvvv", WorckMode.Right);
            //owner.ProssedData(Helper.RandomMilkPackNum(_job.GTIN), WorckMode.Right);
        }
    }
}
