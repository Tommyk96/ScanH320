using PharmaLegaсy.Models;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Util;

namespace PharmaLegacy.Windows
{

    public delegate void RequestComplitedEventHandler(BoxAdditionalNumbers state);

    /// <summary>
    /// Логика взаимодействия для GetMoreNumberWindow.xaml
    /// </summary>
    public partial class GetMoreNumberWindow : Window
    {
        //события
        public event RequestComplitedEventHandler RequestComplitedEvent; // событие изменения статуса линии.

        private System.Windows.Threading.DispatcherTimer windowTimeOut = new System.Windows.Threading.DispatcherTimer();

        //inside the class definition
        private System.Diagnostics.Process _touchKeyboardProcess = null;
        public MessageBoxResult result { get; set; }
        public Autorization.User user { get; set; }
        public Autorization.User curentUser { get; set; }

        public string UserLogin { get; set; }
        public string MessageText { get; set; }

        public string orderId { get; set; }

        public int AddBoxNumCount { get; set; }
        public string RemainingString { get; set; }

        private string user1c;
        private string pass;
        private string uri;
        private readonly int _windowTimeOut;
        public GetMoreNumberWindow() : this("","",0,null,0,60000) { }
        public GetMoreNumberWindow(string _user,string _pass,int _defNum,string ur,int ost,int wTimeOut )
        {
            InitializeComponent();
            
            user1c = _user;
            pass = _pass;
            AddBoxNumCount = _defNum;
            uri = ur;
            _windowTimeOut = wTimeOut;

            if (ost < 0) ost = 0;
            RemainingString = "Осталось номеров коробов: " +ost;

            DataContext = this;

            //настроить таймер бездействия в окнах
            windowTimeOut.Tick += WindowTimeOut_Tick;
            windowTimeOut.Interval = new TimeSpan(0, 0, _windowTimeOut);
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

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {

            result = MessageBoxResult.Cancel;
            Close();
        }
        public static MessageBoxResult ShowEx(Window owner, string msg)//, Windows.MessageBoxExButton buttons)
        {
            return MessageBoxEx.ShowEx(owner, msg,
                    PharmaLegacy.Windows.MessageBoxExButton.OK);
        }
        public void MainWindow_enterUserEventHandler(string data)
        {
            this.Dispatcher.Invoke(() =>
            {
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

            //string userName = "";// = UserId.Text;
           //string pass = "";//= UserPass.Password;
           // tbAddBoxNumCount.Text;
           
            // UserId.IsEnabled = false;
            //UserPass.IsEnabled = false;

            System.Threading.Thread DataInThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                System.Threading.Thread.CurrentThread.IsBackground = true;
                try
                {
                    uri += AddBoxNumCount.ToString(CultureInfo.InvariantCulture);
                    if (!GetAdditionalBoxNumbers(new Uri(uri), user1c, pass))
                        throw new Exception("Ошибка взаимодействия с сервером. Обратитесь к наладчику.\n");
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            result = MessageBoxResult.OK;
                            Close();
                        });
                    }
                    /*
                    //авторизация по 1С
                    Autorization.HttpRequestResult httpResult;
                    //string userName = Properties.Settings.Default.Srv1CLogin;
                    //string pass =  Properties.Settings.Default.Srv1CPass;
                    string url = Properties.Settings.Default.Srv1CUrlAuthorize;
                    Autorization.User1C us = Autorization.AuthUser1C.GetReguest<Autorization.User1C>(url, out httpResult,
                        Properties.Settings.Default.Srv1CLogin,
                        Properties.Settings.Default.Srv1CPass, userName, pass);

                    if (us != null)
                    {
                        user = new Autorization.User(us.ID);
                        user.IsControler = us.Сontroller;
                        user.IsMaster = us.Master;
                        user.IsServiceMen = us.ServiceMan;
                        user.Name = userName;
                    }
                    else if (httpResult.resultCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new Exception("Неверный логин или пароль.");

                    //если нет связи с верхним сервером  авторизируем по внутреннему справочнику
                    user = Autorization.UsersCatalog.GetUser(userName, pass);

                    //если не прошли проверку по справочнику ругаемся на ... сервер :) ну а кули?!
                    if (user == null)
                        throw new Exception("Невозможно соединится с удаленным сервером.");

                    */

                   
                }
                catch (Exception ex)
                {

                    this.Dispatcher.Invoke(() =>
                    {
                        //UserPass.Password = "";
                       // UserId.Text = "";

                        LoginPan.Visibility = Visibility.Collapsed;
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
                _touchKeyboardProcess = System.Diagnostics.Process.Start(touchKeyboardPath);
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

            //UserId.IsEnabled = true;
            //UserPass.IsEnabled = true;

            //переставить таймер
            windowTimeOut.Stop();
            windowTimeOut.Start();
            // MsgText.Text = ex.Message;
        }

        private void tbAddBoxNumCount_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /// <summary>
        /// запрос на дополнительные номера коробок
        /// </summary>
        /// <param name="url"></param>
        private bool GetAdditionalBoxNumbers(Uri url,string user,string pass)
        {
            Log.Write("GetAdditionalProdNumbers " + url.AbsoluteUri);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";//"application/json;charset=utf-8";
            httpWebRequest.Method = "GET";
            httpWebRequest.AllowAutoRedirect = true;
            //**************авторизация***********
            NetworkCredential myNetworkCredential = new NetworkCredential(user,pass);

            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(url, "Basic", myNetworkCredential);

            httpWebRequest.PreAuthenticate = true;
            httpWebRequest.Credentials = myCredentialCache;
            //*************************
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                //проверить статус код
                if (httpResponse.StatusCode != HttpStatusCode.Created)
                    return false;

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string c = streamReader.ReadToEnd();
                    string cmd5 = MD5Calc.CalculateMD5Hash(c);

                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(BoxAdditionalNumbers));
                    BoxAdditionalNumbers or = MD5Calc.DeserializeJSon<BoxAdditionalNumbers>(c);

                    RequestComplitedEvent?.Invoke(or);
                    return true;
                    /*
                    if (or.boxNumbers != null)
                    {
                        
                        //Program.r.CreateFromOrder(Program.or);
                        //if (or.id == orderId)
                        //    currentOrder.AddBoxNumbers(or.boxNumbers);
                        //else
                        //    throw new Exception("Ответ получен, но номер задания не совпадает?!");



                    }
                    else
                        throw new Exception("Ответ получен, но массив номеров пуст?!");*/
                }
                //Program.systemState.LowBoxNumWarning = false;
                //label1.Text = "Статус:" + HttpStatusCode.Created.ToString();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return false;
                try
                {
                    //using (var stream = ex.Response.GetResponseStream())📌
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //label1.Text = "Статус:" + ex.Message + "\n" + reader.ReadToEnd();
                        string s = reader.ReadToEnd();
                        Log.Write("GetAdditionalBoxNumbers  WebException " + ex.Message + "\n" + s,EventLogEntryType.Error, 701);
                        //throw new GetMoreNumberException("Невозможно получить доп. номера коробов:" + ex.Message);
                    }
                }
                catch
                {
                }
                //throw new GetMoreNumberException("Невозможно получить доп. номера коробов:" + ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                Log.Write("GetAdditionalBoxNumbers  ArgumentNullException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (NotSupportedException ex)
            {
                Log.Write("GetAdditionalBoxNumbers  NotSupportedException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (Exception ex)
            {
                Log.Write("GetAdditionalBoxNumbers " + ex.Message,EventLogEntryType.Error, 701);
                //throw new GetMoreNumberException("Невозможно получить доп. номера пачек:" + ex.Message);
            }
            finally
            {
                /*
                if (error)
                {
                    owner.systemState.StatusText = "Ошибка при расознавании ответа сервера.\nНе удалось получить доп номера коробов! Обратитесь к наладчику.";
                    owner.systemState.StatusBackground = Brushes.Red;
                }*/

            }
            return false;

        }
    }


}
