using BoxAgr.BLL.Interfaces;
using PharmaLegaсy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using Util;

namespace BoxAgr.BLL.Models
{
    /// <summary>
    /// ЗАЛОЧИТЬ ОДНОВРМЕННЫЙ ДОСТОП С РАЗНЫХ ПОТОКОВ!!!!!!!!!!!!!
    /// </summary>
    [DataContract]
    public class LocalSystemState :  INotifyPropertyChanged, IModBusState, ISystemState
    {
        //BarcodeLib.Barcode b = new BarcodeLib.Barcode();
        public LocalSystemState()
        {
        }
        private bool _camOnline;
        private bool _camConnect;
        private string _packer;
        private string _curTime;
        private int _GoodLayerCodeCount;
        private int _ReadyBoxCount;
        private string _CounterInCurentBox;
       

        #region IModBusState
        bool power;

        public bool Power
        {
            get { return power; }
            set { SetProperty(ref power, value); }
        }
        bool isBoxSet;
       
        public bool IsBoxSet
        {
            get { return isBoxSet; }
            set { SetProperty(ref isBoxSet, value); }
        }

        private string diagnostic;
        public string Diagnostic
        {
            get { return diagnostic; }
            set { SetProperty(ref diagnostic, value); }
        }

        private bool clientConnected;
        public bool ClientConnected
        {
            get { return clientConnected; }
            set { SetProperty(ref clientConnected, value); }
        }
        
        #endregion


        #region Блок переменных передающихся по веб
        //web ссервис 
        [DataMember]
        public bool webService;
        [DataMember]
        public string OrderInProgresss;
        #endregion


        private int _readyProductCount;
        public int ReadyProductCount
        {
            get => _readyProductCount;
            set {
                
                _readyProductCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ReadyProductCount)));
            }
        }

        public int ReadyBoxCount
        {
            get
            {
                return _ReadyBoxCount;
            }
            set
            {
                _ReadyBoxCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ReadyBoxCount)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));

                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxNotInPallete)));

            }
        }
        private int boxNotInPallete;
        public int BoxNotInPallete
        {
            get
            {
                return boxNotInPallete;
            }
            set
            {
                boxNotInPallete = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxNotInPallete)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));

            }
        }

        private int readyPalleteCount;
        public int ReadyPalleteCount
        {
            get
            {
                return readyPalleteCount;
            }
            set
            {
                readyPalleteCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ReadyPalleteCount)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));

            }
        }

        

        public string Packer
        {
            get
            {
                return _packer;
            }
            set
            {
                _packer = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Packer)));
            }
        }
        public string CurTime
        {
            get
            {
                return _curTime;
            }
            set
            {
                _curTime = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CurTime)));
            }
        }
        public int GoodLayerCodeCount
        {
            get
            {
                return _GoodLayerCodeCount;
            }
            set
            {
                _GoodLayerCodeCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(GoodLayerCodeCount)));
            }
        }
        public bool CamOnline
        {
            get
            {
                return _camOnline;
            }
            set
            {
                _camOnline = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CamState)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CamFillColor)));
            }
        }
        public bool CamConnect
        {
            get
            {
                return _camConnect;
            }
            set
            {
                _camConnect = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CamState)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CamFillColor)));
            }
        }
        public string CamState
        {
            get
            {
                if (_camConnect)
                    return _camOnline ? "Камера в работе" : "Камера не в работе!";

                return "Нет соединения";
            }
            set
            {
               
            }
        }
        public SolidColorBrush CamFillColor
        {
            get
            {
                if (_camConnect)
                    return _camOnline ? Brushes.White : Brushes.Gray;

                return Brushes.Red;
            }
            set
            {

            }
        }


        #region Biotiki 
        bool scanImgShow;
        public bool ScanImgShow
        {
            get
            {
                return scanImgShow;
            }
            set
            {
                scanImgShow = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ScanImgShow)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ScanImgShowState)));


            }
        }
        public string ScanImgShowState
        {
            get { return scanImgShow ? "Да" : "Нет"; }
        }
        //private List<SerialCode> currentSerials = new List<SerialCode>() { new SerialCode("sdsdsd")};
        private ObservableCollection<SerialCode> currentSerials = new ObservableCollection<SerialCode>();//{ new SerialCode("sdsdsd")};
        private ObservableCollection<SerialCode> Boxes = new ObservableCollection<SerialCode>();//{ new SerialCode("sdsdsd")};
        public ObservableCollection<SerialCode> ListCurrentSerials
        {
            get { return currentSerials; }
            set
            {
                currentSerials = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ListCurrentSerials)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
            }
        }
        public void ListCurrentSerialsAdd(string value)
        {

            currentSerials.Add( new SerialCode(value));
            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ListCurrentSerials)));
            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
        }
        public string SerialInBoxCounter {
            get
            {
                return _CounterInCurentBox;
                //return "Пачек "+currentSerials.Count.ToString()+" из " + PackInBox.ToString();
            }
            set
            {
                _CounterInCurentBox = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
            }
        }

        private string rightStatus;
        public string RightStatus
        {
            get
            {
                return rightStatus;
            }
            set
            {
                rightStatus = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightStatus)));
            }
        }

        private string leftStatus;
        public string LeftStatus
        {
            get
            {
                return leftStatus;
            }
            set
            {
                leftStatus = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftStatus)));
            }
        }


        private int _PackInBox;
        public int PackInBox {
            get { return _PackInBox; }
            set {
                _PackInBox = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(SerialInBoxCounter)));
            } }
        public int BoxInOrders;

        //
        public ObservableCollection<SerialCode> ProcessedBoxes
        {
            get { return Boxes; }
            set
            {
                Boxes = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ProcessedBoxes)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));
            }
        }
        public void AddProcessedBoxes(string value)
        {

            Boxes.Add(new SerialCode(value));
            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ProcessedBoxes)));
            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxCounter)));
        }
        public string BoxCounter {
            get {
                //return Boxes.Count;
                return $"Коробов {ReadyBoxCount} из {BoxInOrders}";
            } set { } }

        public int _actualScanner = 0;
        public WorckMode _curentMode;
        public WorckMode CurentMode
        {
            get
            {
                return _curentMode;
            }
            set
            {
                _curentMode = value;
               // InvokePropertyChanged(new PropertyChangedEventArgs("LeftScannerLamp"));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CurentMode)));
            }
        }
        public int ActualScanner
        {
            get
            {
                return _actualScanner;
            }
            set
            {
                _actualScanner = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScannerLamp)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScannerLamp)));
            }
        }

        private System.Windows.Media.Brush _LeftScannerLamp;
        private System.Windows.Media.Brush _RightScannerLamp;
        public System.Windows.Media.Brush LeftScannerLamp
        {
            get
            {
         

                return _LeftScannerLamp;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                
                _LeftScannerLamp = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScannerLamp)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScanEnable)));
            }
        }
        public System.Windows.Media.Brush RightScannerLamp
        {
            get
            {
                return _RightScannerLamp;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _RightScannerLamp = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScannerLamp)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScanEnable)));
            }
        }
        private string _gtin;
        public string GTIN { get { return _gtin; } set
            {
                _gtin = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(GTIN)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(InfoBlockEmptyVisiblility)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(InfoBlockVisiblility)));

            }
        }


        private LinearGradientBrush _leftScanLampColor1;
        private LinearGradientBrush _leftScanLampColor2;

        public LinearGradientBrush LeftScanLampColor1
        {
            get
            {


                return _leftScanLampColor1;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _leftScanLampColor1 = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScanLampColor1)));
            }
        }
        public LinearGradientBrush LeftScanLampColor2
        {
            get
            {


                return _leftScanLampColor2;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _leftScanLampColor2 = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScanLampColor2)));
            }
        }

        private LinearGradientBrush _rightScanLampColor1;
        private LinearGradientBrush _rightScanLampColor2;

        public LinearGradientBrush RightScanLampColor1
        {
            get
            {


                return _rightScanLampColor1;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _rightScanLampColor1 = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScanLampColor1)));
            }
        }
        public LinearGradientBrush RightScanLampColor2
        {
            get
            {


                return _rightScanLampColor2;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _rightScanLampColor2 = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScanLampColor2)));
            }
        }

        public bool CriticalError { get; set; } = false;

        private bool _stopLine;
        public bool StopLine
        {
            get
            {


                return _stopLine;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                if (_stopLine != value && value == true)
                    StopCounter = 0;

                _stopLine = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLine)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLineInverce)));
            }
        }
        public bool StopLineInverce
        {
            get
            {


                return !_stopLine;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                //if (_stopLine != value && value == true)
                //    StopCounter = 0;

                //_stopLine = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLineInverce)));
                //InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StopLine22)));
            }
        }

        

        public int StopCounter = 2;

        private bool _leftScanEnable;
        public bool LeftScanEnable
        {
            get
            {
                if (LeftScannerLamp == Brushes.Red)
                    return false;
                else
                    return true;
            }
            set
            {
                _leftScanEnable = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftScanEnable)));
            }
        }

        private bool _rightScanEnable;
        public bool RightScanEnable
        {
            get
            {
                if (RightScannerLamp == Brushes.Red)
                    return false;
                else
                    return true;
            }
            set
            {
                _rightScanEnable = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScanEnable)));
            }
        }

        //private 
        public Visibility InfoBlockEmptyVisiblility
        {
            get
            {


                if (string.IsNullOrEmpty(GTIN))// == "")
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                //_stopLine = value;
               // InvokePropertyChanged(new PropertyChangedEventArgs("StopLine"));
            }
        }
        public Visibility InfoBlockVisiblility
        {
            get
            {

                if (string.IsNullOrEmpty(GTIN))// == "")
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                //_stopLine = value;
                //InvokePropertyChanged(new PropertyChangedEventArgs("StopLine"));
            }
        }

        private Visibility _LeftBoxAwaiVerifyVisiblility;
        private Visibility _RightBoxAwaiVerifyVisiblility;
        public Visibility LeftBoxAwaiVerifyVisiblility
        {
            get
            {
                    return _LeftBoxAwaiVerifyVisiblility;
            }
            set
            {
                _LeftBoxAwaiVerifyVisiblility = value;
                 InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LeftBoxAwaiVerifyVisiblility)));
            }
        }

        public Visibility RightBoxAwaiVerifyVisiblility
        {
            get
            {
                return _RightBoxAwaiVerifyVisiblility;
            }
            set
            {
                _RightBoxAwaiVerifyVisiblility = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightBoxAwaiVerifyVisiblility)));
            }
        }

        //[DataMember]
        // private int _MaxPackInOneScanMode;


        private bool _powerOff;
        public bool PowerOff
        {
            get
            {
                    return _powerOff;
            }
            set
            {
                _powerOff = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(RightScanEnable)));
            }
        }

        #endregion

        private int _readCodeCount;
        private int _layerCodeCount;
        private int _layerCount;
        private int _layersCount;
        private string _boxNumber;
        private string _productName;
        private string _partNumber;
        private string _lastCodeInLastBox;

        private string _statusText;
        private System.Windows.Media.Brush  _statusBackground;
        //считано на текущем такте
        public int ReadCodeCount
        {
            get
            {
                return _readCodeCount;
            }
            set
            {
                _readCodeCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ReadCodeCount)));
            }
        } 
        //текущий слой
        public int LayerCount
        {
            get
            {
                return _layerCount;
            }
            set
            {
                _layerCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LayerCount)));
            }
        }

        //пачек в слое
        public int LayerCodeCount
        {
            get
            {
                return _layerCodeCount;
            }
            set
            {
                _layerCodeCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LayerCodeCount)));
            }
        }
        //слоев в коробе 
        public int LayersCount
        {
            get
            {
                return _layersCount;
            }
            set
            {
                _layersCount = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LayersCount)));
            }
        }

       
        //private System.Windows.Media.Brush _startBackgroundColor;
        ////цвет кнопки старт
        //public System.Windows.Media.Brush StartBackgroundColor
        //{
        //    get
        //    {
        //        return _readCodeCount;
        //    }
        //    set
        //    {
        //        _readCodeCount = value;
        //        InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ReadCodeCount)));
        //    }
        //}

        public string BoxNumber
        {
            get
            {
                return _boxNumber;
            }
            set
            {
                _boxNumber = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxNumber)));
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(BoxNumberImg)));

            }
        }

        public string ProductName
        {
            get
            {
                return _productName;
            }
            set
            {
                _productName = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ProductName)));
            }
        }

        public string PartNumber
        {
            get
            {
                return _partNumber;
            }
            set
            {
                _partNumber = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(PartNumber)));
            }
        }

        public System.Windows.FrameworkElement BoxNumberImg
        {
            get
            {
                System.Windows.Controls.Canvas c = new System.Windows.Controls.Canvas();
                try
                {
                    /*
                    //  System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
                    //int W = Convert.ToInt32(this.txtWidth.Text.Trim());
                    //int H = Convert.ToInt32(this.txtHeight.Text.Trim());

                    //  Convert.ToInt32(this.txtWidth.Text.Trim());
                    // int H = Convert.ToInt32(this.txtHeight.Text.Trim());
                    BarcodeLib.TYPE type = BarcodeLib.TYPE.CODE128B;
                    //{
                    b.IncludeLabel = false;

                    //===== Encoding performed here =====
                    System.Drawing.Image image = b.Encode(type, _boxNumber.Trim(), System.Drawing.Color.Black, System.Drawing.Color.White, 500, 80);
                    c.Height = image.Height;
                    c.Width = image.Width;
                    //===================================
                    // ImageSource ...
                    System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();

                    // Save to a memory stream...
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                    // Rewind the stream...
                    ms.Seek(0, System.IO.SeekOrigin.Begin);

                    // Tell the WPF image to use this stream...
                    bi.StreamSource = ms;
                    bi.EndInit();

                    c.Background = new System.Windows.Media.ImageBrush(bi);*/



                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                return c;


            }
            
        }


        public System.Windows.Media.Brush StatusBackground
        {
            get
            {
                if (_statusBackground == null)
                    _statusBackground = System.Windows.Media.Brushes.Transparent;

                return _statusBackground;
                //return System.Windows.Media.Brushes.Red;
            }
            set
            {
                _statusBackground = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StatusBackground)));
            }
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    InvokePropertyChanged(new PropertyChangedEventArgs(nameof(StatusText)));

                    if(!string.IsNullOrEmpty(_statusText))// != "")
                        Log.Write("ST."+_statusText);
                }
            }
        }

        public string LastCodeInLastBox
        {
            get
            {
                return _lastCodeInLastBox;
            }
            set
            {
                if (_lastCodeInLastBox != value)
                {
                    _lastCodeInLastBox = value;
                    InvokePropertyChanged(new PropertyChangedEventArgs(nameof(LastCodeInLastBox)));

                    //if (_LastCodeInLastBox != "")
                    //    Log.Write("ST." + _LastCodeInLastBox);
                }
            }
        }

        private string _ver;
        public string ver
        {
            get
            {
                return _ver;
            }
            set
            {
                _ver = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ver)));
            }
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
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

    }
}
