using PharmaLegaсy.Models;
using System;
using System.Windows;

namespace BoxAgr.tests
{
    /// <summary>
    /// Логика взаимодействия для testBoxCode.xaml
    /// </summary>

    public delegate void ScanEventHandler(string indata, WorckMode e);

    public partial class testBoxCode : Window
    {
        public event ScanEventHandler ScanCompleted;
        private WorckMode scannerSide = WorckMode.None;

        public testBoxCode() : this("", WorckMode.None) { }
        public testBoxCode(string boxNumber, WorckMode scs)
        {
            InitializeComponent();

            //создать картинку
            //System.Windows.Controls.Canvas c = new System.Windows.Controls.Canvas();
            try
            {
                tbCode.Text = boxNumber;
                scannerSide = scs;

                //BarcodeStandard.Barcode b = new ();
                //BarcodeStandard.Type type = BarcodeStandard.Type.Code128B;
             
                //b.IncludeLabel = false;

                ////===== Encoding performed here =====
                //var image = b.Encode(type, boxNumber.Trim(), 600, 80).Encode().AsStream();
                //canvas1.Height =600;
                //canvas1.Width = 80;
                ////===================================
                //// ImageSource ...
                //System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
                //bi.BeginInit();
                //System.IO.MemoryStream ms = new System.IO.MemoryStream();

                // Save to a memory stream...
               // image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                // Rewind the stream...
                //ms.Seek(0, System.IO.SeekOrigin.Begin);

                // Tell the WPF image to use this stream...
                //bi.StreamSource = image;
                //bi.EndInit();

                //canvas1.Background = new System.Windows.Media.ImageBrush(bi);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ScanCompleted?.Invoke(tbCode.Text, scannerSide);
            Close();
        }
    }
}
