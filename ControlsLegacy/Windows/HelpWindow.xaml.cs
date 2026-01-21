using PharmaLegacy.data;
using PharmaLegaсy.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace PharmaLegacy.Windows
{
    /// <summary>
    /// Логика взаимодействия для HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public MessageBoxResult result { get; set; }
        public string MessageText { get; set; }
        public HelpWindow()
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
            HelpWindow w = new HelpWindow();
            w.MessageText = msg;
            w.Owner = owner;
            w.InitButtons(buttons);
            if (owner != null)
                ((IMainFrame)owner).codeAddEventHandler += w.MainWindow_codeAddEventHandler;
                // ((MainWindow)owner).scanDataEventHandler += w.HelpWindow_scanDataEventHandler; 
            
            w.ShowDialog();
            if (owner != null)
                ((IMainFrame)owner).codeAddEventHandler -= w.MainWindow_codeAddEventHandler;
           // ((MainWindow)owner).scanDataEventHandler -= w.HelpWindow_scanDataEventHandler; ;
            return w.result;
        }

        private  void HelpWindow_scanDataEventHandler(string data)
        {
            //throw new NotImplementedException();
        }

        private void MainWindow_codeAddEventHandler(AddCodeType typeRead)
        {
            //throw new NotImplementedException();
            result = MessageBoxResult.Yes;
            this.Dispatcher.Invoke(
                     System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() => { Close(); }));
        }
        public void InitButtons(MessageBoxExButton buttons)
        {
            BtnPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxExButton.YesNo:
                    BtnPanel.Children.Add(Yes);
                    BtnPanel.Children.Add(No);
                    break;
                case MessageBoxExButton.OKCancel:
                    BtnPanel.Children.Add(Yes);
                    BtnPanel.Children.Add(Cancel);
                    break;
                case MessageBoxExButton.YesNoCancel:
                    BtnPanel.Children.Add(Yes);
                    BtnPanel.Children.Add(No);
                    BtnPanel.Children.Add(Cancel);
                    break;
                case MessageBoxExButton.Cancel:
                    BtnPanel.Children.Add(Cancel);
                    break;
                default:
                    break;
            }

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
