using PharmaLegacy.data;
using PharmaLegaсy.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace PharmaLegacy.Windows
{
    /// <summary>
    /// Логика взаимодействия для ReprintWindow.xaml
    /// </summary>
    public partial class ReprintWindow : Window
    {
        public MessageBoxResult result { get; set; }
        public string MessageText { get; set; }
        public ReprintWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Cancel;
            Close();
        }
        public static MessageBoxResult ShowEx(Window owner, string msg, MessageBoxExButton buttons)
        {
            ReprintWindow w = new ReprintWindow();
            w.MessageText = msg;
            w.Owner = owner;
            try
            {
                if (owner != null)
                    ((IMainFrame)owner).codeAddEventHandler += w.MainWindow_codeAddEventHandler;

                w.ShowDialog();
            }
            finally { 
                if (owner != null)
                    ((IMainFrame)owner).codeAddEventHandler -= w.MainWindow_codeAddEventHandler;
             }
            return w.result;
        }


        private void MainWindow_codeAddEventHandler(AddCodeType typeRead)
        {
            result = MessageBoxResult.Yes;
            this.Dispatcher.Invoke(
                     System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() => { Close(); }));
        }
       

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.No;
            Close();
        }
    }
}
