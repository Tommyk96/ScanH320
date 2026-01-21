using BoxAgr.BLL;
using BoxAgr.BLL.Models;
using BoxAgr.Configure;
using BoxAgr.Windows;
using FSerialization;
using PharmaLegacy.Pages;
using PharmaLegaсy.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace BoxAgr.Pages
{
    /// <summary>
    /// Логика взаимодействия для SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, INotifyPropertyChanged
    {
       
        protected MainWindow owner;
        private Config Settings => App.Settings;
        private LocalSystemState systemState => App.SystemState;

        public static readonly RoutedEvent BackClickEvent = EventManager.RegisterRoutedEvent(
           "BackClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PageBase));

        // Provide CLR accessors for the event
        public event RoutedEventHandler BackClick
        {
            add { AddHandler(BackClickEvent, value); }
            remove { RemoveHandler(BackClickEvent, value); }
        }

        private System.Windows.Threading.DispatcherTimer windowTimeOut = new System.Windows.Threading.DispatcherTimer();

        #region Data
        public int PackCounter { get; set; }
        public int PackInBox { get; set; }
        public int BoxCounter { get; set; }
        public int BrackCount { get; set; }
        public int SampleCount { get; set; }
        public int LayerFailCount { get; set; }
        public int MaxPackInOneScanMode { get; set; }
        public int BoxFullPercentToStop { get; set; }
        public int MinBoxNumberBeforeWarning { get; set; }

        
        public string PrinterPort { get; set; } 
        public string PrinterStatus { get; set; }
        public string CameraPort { get; set; }
        private string _cameraStatus;
        public string CameraStatus {
            get
            {
                return _cameraStatus;
            }
            set
            {
                _cameraStatus = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CameraStatus)));
            }
        }
        private string _plcStatus;
        public string PlcStatus
        {
            get
            {
                
                return _plcStatus;
            }
            set
            {
                _plcStatus = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(PlcStatus)));
            }
        }

        public string SerialPort232Name { get; set; }
        public string StatusSerialPort232Name { get; set; }
        public string HandSerialPort232Name { get; set; }
        public string StatusHandSerialPort232Name { get; set; }


        bool boxGrid;
        public bool BoxGrid {
            get
            {
                return boxGrid;
            }
            set
            {
                boxGrid = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxGrid)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxGridState)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(GridSettingsVisible)));
                

            }
        }
        public string BoxGridState
        {
            get { return BoxGrid ? "Да" : "Нет"; }
        }     
        public Visibility GridSettingsVisible { get { return BoxGrid ?  Visibility.Visible : Visibility.Collapsed; }  }

        private int numRows;
        public int NumRows { get { return numRows; } set { SetProperty(ref numRows, value); } }

        private int numColumns;
        public int NumColumns { get { return numColumns; } set { SetProperty(ref numColumns, value); } }

        private int boxHeight;
        public int BoxHeight { get { return boxHeight; } set { SetProperty(ref boxHeight, value); } }

        private int boxWidth;
        public int BoxWidth { get { return boxWidth; } set { SetProperty(ref boxWidth, value); } }

        



        public string Version { get; set; }

        private bool palletAutoCreate;
        public bool PalletAutoCreate
        {
            get
            {
                return palletAutoCreate;
            }
            set
            {
                palletAutoCreate = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(PalletAutoCreate)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(PalletAutoCreateState)));

            }
        }
        public string PalletAutoCreateState
        {
            get { return PalletAutoCreate ? "Да" : "Нет"; }
        }


        private bool stopLineAfterPalletFull;
        public bool StopLineAfterPalletFull
        {
            get
            {
                return stopLineAfterPalletFull;
            }
            set
            {
                stopLineAfterPalletFull = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLineAfterPalletFull)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLineAfterPalletFullState)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));

            }
        }
        public string StopLineAfterPalletFullState
        {
            get { return StopLineAfterPalletFull ? "Да" : "Нет"; }
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
        protected bool SetProperty<T>(ref T backingStore, T value,
         [CallerMemberName] string propertyName = "",
         Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            InvokePropertyChanged(new PropertyChangedEventArgs(propertyName));
            return true;
        }

        #endregion
        #endregion

        // This method raises the Tap event
        protected void RaiseBackClickEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(SettingsPage.BackClickEvent);
            RaiseEvent(newEventArgs);
        }


        public SettingsPage(MainWindow o) 
        {
            InitializeComponent();
            owner = o;

            //передать указатель на главное окно
            //PrintTab.Content = new Pages.PrinterSettingsPage(o);
            // UserTab.Content = new Pages.UserSettingsPage(o);
            //настроить таймер бездействия в окнах
            windowTimeOut.Tick += WindowTimeOut_Tick; ;
            windowTimeOut.Interval = new TimeSpan(0, 15, 0);
            // windowTimeOut.Start();

            //blOneOperator.Visibility = Visibility.Hidden;
            //blTwoOperators.Visibility = Visibility.Hidden;

        }

        private void WindowTimeOut_Tick(object sender, EventArgs e)
        {
            windowTimeOut.Stop();
            
            if (this.IsVisible)
                RaiseBackClickEvent();

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            //спросить применить изменения или нет 
            //MessageBoxResult res = MessageBox.Show("Сохранить изменения?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            windowTimeOut.Stop();

            MessageBoxResult res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(owner,"Сохранить изменения?", PharmaLegacy.Windows.MessageBoxExButton.YesNoCancel);

            if (res == MessageBoxResult.Cancel)
            {
                windowTimeOut.Start();
                return;
            }

            if (res == MessageBoxResult.Yes)
            {
                Settings.BoxPrinterIp = PrinterPort;
               // Properties.Settings.Default.IpVision=CameraPort ;
                Settings.SerialPort232Name=SerialPort232Name;
                Settings.HandSerialPort232Name=HandSerialPort232Name;
              //  Properties.Settings.Default.LayerFailCount = LayerFailCount;
                Settings.MaxPackInOneScanMode = MaxPackInOneScanMode;
                Settings.BoxFullPercentToStop = BoxFullPercentToStop;
                Settings.MinBoxNumberBeforeWarning = MinBoxNumberBeforeWarning;
                Settings.PalletAutoCreate = PalletAutoCreate;
                Settings.StopLineAfterPalletFull = StopLineAfterPalletFull;

                //Settings.BoxGrid = BoxGrid;
                Settings.NumRows = NumRows;
                Settings.NumColumns = NumColumns;
                Settings.BoxHeight = BoxHeight;
                Settings.BoxWidth = BoxWidth;

                
                owner.SelectMainPage(BoxGrid);
                Settings.Save();
            }

            windowTimeOut.Stop();
            RaiseBackClickEvent();
            e.Handled = true;
        }

        private void btnShutdown_Click(object sender, RoutedEventArgs e)
        {
            windowTimeOut.Stop();
            MessageBoxResult res;
            //проверить остановлена ли лента
            if (systemState.StopLine != true)
            {
                 res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(owner, "Перед выключением нужно остановить линию\nОстановить её сейчас?", PharmaLegacy.Windows.MessageBoxExButton.OKCancel);
                if (res == MessageBoxResult.Cancel)
                {
                    windowTimeOut.Start();
                    return;
                }

                //остановить линию
                owner.StopLine();
            }
            
            res = PharmaLegacy.Windows.MessageBoxEx.ShowEx(owner, "Завершить работу комплекса?\nСохранены будут только завершённые операции!", PharmaLegacy.Windows.MessageBoxExButton.OKCancel);
            //res = bAgr.Windows.MessageBoxEx.ShowEx(owner, "Завершить работу комплекса?\nВсе незаконченные операции не будут сохранены!", Windows.MessageBoxExButton.OKCancel);


            if (res == MessageBoxResult.Cancel)
            {
                windowTimeOut.Start();
                return;
            }

            //закрыть сессию
            owner.CloseSession();

            Process.Start("shutdown", "/s /t 0");
           
            //RaiseBackClickEvent();
            e.Handled = true;
           
        }

        private void btnPrintTest_Click(object sender, RoutedEventArgs e)
        {
            owner.TestPrint();
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void printerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void cameraPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void handSerialPort232Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void serialPort232Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void LayerFailCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void MaxPackInOneScanMode_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void tbBoxFullPercentToStop_TextChanged(object sender, TextChangedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue == true)
                windowTimeOut.Start();
        }

        private void btnSelectBox_Click(object sender, RoutedEventArgs e)
        {
            SelectNomenclatureWindow hw = new();
            hw.Owner = owner;
            e.Handled = true;

            if (hw.ShowDialog() == true)
            {
                //NumRows = hw.SelectedItem.NumRows;
                //NumColumns = hw.SelectedItem.NumColumns;
                //BoxHeight = hw.SelectedItem.BoxHeight;
                //BoxWidth = hw.SelectedItem.BoxWidth;
            }
        }
    }
    
}


