using PharmaLegacy.data;
using PharmaLegaсy.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;
using Util;

namespace PharmaLegacy.Windows
{
    public enum MessageBoxExButton
    {
        //
        // Сводка:
        //     В окне сообщения отображается кнопка ОК.
        OK = 0,
        //
        // Сводка:
        //     В окне сообщения отображаются кнопки ОК и Отмена.
        OKCancel = 1,
        //
        // Сводка:
        //     В окне сообщения отображаются кнопки Да, Нет, and Отмена.
        YesNoCancel = 3,
        //
        // Сводка:
        //     В окне сообщения отображаются кнопки Да и Нет.
        YesNo = 4,
        No=5,
        Cancel=6,
        LastBox =7
    }
    /// <summary>
    /// Логика взаимодействия для ApplyWindow.xaml
    /// </summary>
    public partial class MessageBoxEx : Window
    {
       
        public MessageBoxResult result { get; set; }
        public string MessageText { get; set; } = "";
        public bool DebugMode { get; set; }
        private Action? _dbgAction { get; }
        public MessageBoxEx(Action? DbgAction)
        {
            _dbgAction = DbgAction;
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
           
            return ShowEx(owner, msg, buttons,false);

        }
        public static MessageBoxResult ShowEx(Window owner,string msg, MessageBoxExButton buttons,
            bool dbgMode = false, Action? action = null)
        {
            try
            {
                // ((MainWindow)owner).BlurWindow(true);
                MessageBoxEx w = new MessageBoxEx(action)
                {
                    MessageText = msg,
                    Owner = owner,
                    DebugMode = dbgMode
                };

                w.InitButtons(buttons);
                if (owner != null)
                    ((IMainFrame)owner).codeAddEventHandler += w.MainWindow_codeAddEventHandler;
                w.ShowDialog();
                if (owner != null)
                    ((IMainFrame)owner).codeAddEventHandler -= w.MainWindow_codeAddEventHandler;

                Log.Write($"MBW.{Environment.CurrentManagedThreadId}.1:{msg}\nResult={w.result}");
                // ((MainWindow)owner).BlurWindow(false);
                return w.result;
            }
            catch (Exception)
            {

            }
            return MessageBoxResult.Cancel;
        }

        private void MainWindow_codeAddEventHandler(AddCodeType typeRead)
        {
            //throw new NotImplementedException();
            result = MessageBoxResult.Yes;
            this.Dispatcher.Invoke(
                     System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() => { result = MessageBoxResult.None; Close(); }));
        }
        public void ReInit(string msg, MessageBoxExButton buttons)
        {
            MessageText = msg;
            InitButtons(buttons);
        }
        public void MainWindow_codeAddEventHandler2(AddCodeType typeRead)
        {
            //throw new NotImplementedException();
            result = MessageBoxResult.Yes;
            this.Dispatcher.Invoke(
                     System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(() => { result = MessageBoxResult.None; Close(); }));
        }

        public void InitButtons(MessageBoxExButton buttons)
        {
            BtnPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxExButton.OK:
                    BtnPanel.Children.Add(Yes);
                    break;
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
                case MessageBoxExButton.LastBox:
                    BtnPanel.Children.Add(LastBox); 
                    break;
                default:
                    break;
            }

            if (DebugMode)
                BtnPanel.Children.Add(DbgMode);

            //настроить таймер бездействия в окнах
            //windowTimeOut.Tick += WindowTimeOut_Tick; ;
            //windowTimeOut.Interval = new TimeSpan(0, 0, 10);
            //windowTimeOut.Start();

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

        private void LastBox_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Yes;
            Close();
        }

        private void DbgMode_Click(object sender, RoutedEventArgs e)
        {
            _dbgAction?.Invoke();
            //if (Owner is IMainFrame mw)
            //    mw.
            //mw.DbgTestDropCodeFromCurrentBox();

            // mw.DbgTestFnc1("");
        }
    }
}
