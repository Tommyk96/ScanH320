using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PharmaLegacy.data;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using AgrBox;
using FluentFTP;

namespace PharmaLegacy.Windows
{
    /// <summary>
    /// Логика взаимодействия для ApplyWindow.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer windowTimeOut = new System.Windows.Threading.DispatcherTimer();

        //inside the class definition
        private Process _touchKeyboardProcess = null;
        public MessageBoxResult result { get; set; }
        public Autorization.User user { get; set; }
        public Autorization.User curentUser { get; set; }

        public string UserLogin { get; set; }
        public string MessageText { get; set; }
        private bool onlyMaster;
        private bool onlyControl;
        private bool onlyServiceMen;
        private bool authRepitEnable;

        private string _srv1CUrlAuthorize;
        private string _srv1CLogin;
        private string _srv1CPass;

        public LogonWindow() : this(false,false,false,false,null,60000,"","","") { }
        public LogonWindow(bool _onlyMaster,bool _onlyControl,bool _onlyServiceMen,bool _authRepitEnable,Autorization.User _cu,
            int winTimeOut,string srv1CUrlAuthorize, string srv1CLogin, string Srv1CPass)
        {
            InitializeComponent();
            DataContext = this;
            onlyMaster = _onlyMaster;
            onlyControl = _onlyControl;
            onlyServiceMen = _onlyServiceMen;
            curentUser = _cu;
            authRepitEnable = _authRepitEnable;
            _srv1CUrlAuthorize = srv1CUrlAuthorize;
            _srv1CLogin = srv1CLogin;
            _srv1CPass = Srv1CPass;



            //настроить таймер бездействия в окнах
            windowTimeOut.Tick += WindowTimeOut_Tick;
            windowTimeOut.Interval = new TimeSpan(0, 0, winTimeOut);
            windowTimeOut.Start();
        }

        //таймер бездействия окна
        private void WindowTimeOut_Tick(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                windowTimeOut.Stop();
                CloseApp_Click(null,null);
            });
        }
        //aimp
        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {

            result = MessageBoxResult.Cancel;
            Close();
        }
        public static MessageBoxResult ShowEx(Window owner, string msg)//, Windows.MessageBoxExButton buttons)
        {
            return MessageBoxEx.ShowEx(owner, msg,
                    PharmaLegacy.Windows.MessageBoxExButton.OK);
            //MessageBoxEx w = new MessageBoxEx();
            //w.MessageText = msg;
            //w.Owner = owner;
            ////w.InitButtons(buttons);
            //w.ShowDialog();
            //return w.result;
        }
        public void MainWindow_enterUserEventHandler(string data)
        {
            this.Dispatcher.Invoke(() =>
            {
                UserId.Text = data;
                //переставить таймер
                windowTimeOut.Stop();
                windowTimeOut.Start();
            });
                // throw new NotImplementedException();
        }
        public void InitButtons(MessageBoxButton buttons)
        {
            

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();

            result = MessageBoxResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            //переставить таймер
            windowTimeOut.Stop();

            result = MessageBoxResult.No;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            BtnPanel.Visibility = Visibility.Collapsed;
            WaitPoint.Visibility = Visibility.Visible;
            

            string userName = UserId.Text;
            string pass = UserPass.Password;

            UserId.IsEnabled = false;
            UserPass.IsEnabled = false;

            System.Threading.Thread DataInThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                System.Threading.Thread.CurrentThread.IsBackground = true;
                try
                {
                    //сьросить предыдущие данные
                    user = null;

                    //авторизация по 1С
                    Autorization.HttpRequestResult httpResult;
                    //string userName = Properties.Settings.Default.Srv1CLogin;
                    //string pass =  Properties.Settings.Default.Srv1CPass;
                    string url = _srv1CUrlAuthorize;
                    Autorization.User1C us = Autorization.AuthUser1C.GetReguest<Autorization.User1C>(url, out httpResult,
                        _srv1CLogin,
                        _srv1CPass, userName, pass);

                    if (us != null)
                    {
                        user = new Autorization.User(us.ID);
                        user.IsControler = us.Сontroller;
                        user.IsMaster = us.Master;
                        user.IsServiceMen = us.ServiceMan;
                        user.Name = userName;
                        user.Hash = us.Hash;

                        //получили пользователя теперь обновляем его в локальном кеше
                        System.Threading.Tasks.Task.Factory.StartNew(() => { Autorization.UsersCatalog.UpdateUser(user); });
                    }
                    else if (httpResult.resultCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new Exception("Неверный логин или пароль.");
                   

                    //если нет связи с верхним сервером  авторизируем по внутреннему справочнику
                    if(user == null)
                        user = Autorization.UsersCatalog.GetUser(userName, pass);

                    //если не прошли проверку по справочнику ругаемся на ... сервер :) ну а кули?!
                    if (user == null)
                        throw new Exception("Невозможно соединится с удаленным сервером.");


                    //проверить что пользователь новый отличается от старого
                    if((user.ID == curentUser?.ID) && !authRepitEnable)
                        throw new Exception("Пользователь уже авторизован.");

                    if (onlyServiceMen)
                    {
                        if (!user.IsServiceMen && !user.IsMaster)
                            throw new Exception("Требуется вход наладчика или мастера.");

                        //if (!user.IsMaster) 
                        //    throw new Exception("Требуется вход наладчика или мастера.");

                        this.Dispatcher.Invoke(() =>
                        {
                            result = MessageBoxResult.OK;
                            Close();
                            return;
                        });

                    }

                    if (onlyMaster)
                    {
                        if (!user.IsMaster)
                            throw new Exception("Требуется вход мастера.");

                    }
                    if (onlyControl)
                    {
                        if (!user.IsMaster)
                            throw new Exception("Требуется вход контролера.");

                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        result = MessageBoxResult.OK;
                        Close();
                    });
                }
                catch (Exception ex)
                {

                    this.Dispatcher.Invoke(() =>
                    {
                        UserPass.Password = "";
                        UserId.Text = "";

                        LoginPan.Visibility = Visibility.Hidden;
                        LoginErrorPan.Visibility = Visibility.Visible;
                        MsgText.Text = ex.Message;
                    });

                    // MessageBox.Show("При обработке возникла ошибка. Операция отменена.\n\r Ошибка:", ex.ToString());
                    //result = MessageBoxResult.Cancel;
                    //Close();
                }
            }));
            DataInThread.Start();

            
            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void UserPass_LostFocus(object sender, RoutedEventArgs e)
        {
            /*
            if (_touchKeyboardProcess != null)
            {
                _touchKeyboardProcess.Kill();
                //nullify the instance pointing to the now-invalid process
                _touchKeyboardProcess = null;
            }*/

            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }

        private void UserPass_GotTouchCapture(object sender, TouchEventArgs e)
        {
            try
            {
                string touchKeyboardPath = @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";
                _touchKeyboardProcess = Process.Start(touchKeyboardPath);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (_touchKeyboardProcess != null)
                {
                    _touchKeyboardProcess.Kill();
                    //nullify the instance pointing to the now-invalid process
                    _touchKeyboardProcess = null;
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void CloseErrorPane_Click(object sender, RoutedEventArgs e)
        {
            LoginPan.Visibility = Visibility.Visible;
            LoginErrorPan.Visibility = Visibility.Hidden;
            BtnPanel.Visibility = Visibility.Visible;
            WaitPoint.Visibility = Visibility.Collapsed;

            UserId.IsEnabled = true;
            UserPass.IsEnabled = true;

            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
            // MsgText.Text = ex.Message;
        }

        private void UserId_KeyUp(object sender, KeyEventArgs e)
        {
            windowTimeOut.Stop();
            windowTimeOut.Start();
        }
    }
}
