using Aardwolf;
using AgrBox.data;
using FSerialization;
using PharmaLegaсy.Interfaces;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Util;

namespace PharmaLegacy
{

    static public class MD5Calc {
#pragma warning disable CA5351 
        public static  string CalculateMD5Hash(string input)

        {

            // step 1, calculate MD5 hash from input

            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            //byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)

            {

                sb.Append(hash[i].ToString("X2"));

            }

            return sb.ToString();
        }
#pragma warning restore CA5351

        public static T DeserializeJSon<T>(string jsonString)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T obj = (T)ser.ReadObject(stream);
            return obj;
        }

    }

    public sealed class MyHttpAsyncHandler : IHttpAsyncHandler
    {
        private readonly bool _packetLogEnable;
        private readonly string _srv1CLogin;
        private readonly string _srv1CPass;
        private readonly int _lineNum;

        Dictionary<string, string> extensions = new Dictionary<string, string>()
        { 
            //{ "extension", "content type" }
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"},
            { "js", "text/javascript"},
            { "class", "application/java-vm"},
            {"jar","application/java-archive"},
            {"jnlp","application/x-java-jnlp-file"}
        };
        //private readonly IMainFrame owner;
        private readonly IJob _job;
        private readonly IConfig _config;

        private static Random random = new Random();

        //public static readonly MyHttpAsyncHandler Default = new MyHttpAsyncHandler(null,false);

        private static readonly Task<IHttpResponseAction> NullTask = Task.FromResult<IHttpResponseAction>(null);

        public MyHttpAsyncHandler(IJob job,IConfig config, bool packetLogEnable,string srv1CLogin, string srv1CPass,int lineNum)
        {
            //owner = o;
            _config = config;
            _job = job;
            _packetLogEnable = packetLogEnable;
            _srv1CLogin = srv1CLogin;
            _srv1CPass = srv1CPass;
            _lineNum = lineNum;
        }

        public Task<IHttpResponseAction> Execute(IHttpRequestContext state)
        {
            string[] url = state.Request.RawUrl.Split('/');
            string JobName = url[1];

            try
            {
                if (state.Request.HttpMethod?.Equals("POST") == true)
                {
                    //проверка по БД и запись в бд
                    if (state.Request.Url.AbsolutePath.Equals("/mark/hs/CV/CodeVerify", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Stopwatch sp = new ();
                        ScanCode sc = null;
                        string result = "не распознан";
                        string content = "";
                        try
                        {
                            sp.Restart();

                           
                            //string msg = "";
                            using (StreamReader rs = new StreamReader(state.Request.InputStream))
                            {
                                content = rs.ReadToEnd();

                                #region проверки кода
                                if (string.IsNullOrEmpty(content))
                                {
                                    result = "не верный формат запроса";
                                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad format", null));
                                }

                              

                                //проверить код на корректность и вернуть ответ
                                sc = new ScanCode(content);
                                if (string.IsNullOrEmpty(sc?.Sn))
                                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(434, "Field 21 not exist", null));

                              
                                bool dropSequence = false;
                                //if (owner?.ProcessCode(sc.gs,sc.FullNum,out dropSequence) == true)
                                //{
                                //    result = "Принят";// Log.Write("SSC", $"Принят линия :{sc.LineId}   номер:{sc.FullNum}", EventLogEntryType.Information, 778);
                                //    sp.Stop();
                                //    return Task.FromResult<IHttpResponseAction>(new JsonResponse(200, "OK", $"processing time {sp.ElapsedMilliseconds}"));
                                //}

                              

                                result = "логика программы";//Log.Write("SSC", $"Повтор линия:{sc.LineId}   номер:{sc.FullNum}", EventLogEntryType.Information, 778);
                                return Task.FromResult<IHttpResponseAction>(new JsonResponse(435, "discarded by the program logic", null));

                                #endregion
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write("HTTP", ex.Message,EventLogEntryType.Error, 49);
                        }
                        finally
                        {
                            sp.Stop();
                            if (_packetLogEnable)
                              Log.Write("HTTP", $"msg: {content} code: {sc?.FullNum} result {result}  time {sp.ElapsedMilliseconds} ",EventLogEntryType.Error, 49);
                        }
                        return Task.FromResult<IHttpResponseAction>(new JsonResponse(200, "OK", ""));
                    }

                    //прием заданий медики. абар
                    if (state.Request.Url.AbsolutePath.Equals("/jobs", StringComparison.OrdinalIgnoreCase))
                    {
                        #region /jobs  
                        FSerialization.NewPartAggregate1СOrder order1C = null;
                        using (state.Request.InputStream)
                        {
                            //DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(FSerialization.PartAggregate1СOrder));
                            //order1C = (Sotex.Serialization.PartAggregate1СOrder)jsonSerializer.ReadObject(state.Request.InputStream);


                            string json = "";
                            using (StreamReader rs = new StreamReader(state.Request.InputStream))
                            {
                                json = rs.ReadToEnd();

                                System.IO.TextWriter logFile = new System.IO.StreamWriter("lastOrder.txt", false);
                                logFile.WriteLine(json);
                                logFile.Close();
                            }


                            //сравнить хеш
                            if (state.Request.Headers["Content-MD5"] != null)
                            {
                                string md5 = state.Request.Headers["Content-MD5"];

                                md5 = md5.Replace(" ", "");
                                string cmd5 = MD5Calc.CalculateMD5Hash(json); //B6E4623350509B41D3CADA1432E42E5A

                                // Create a StringComparer an compare the hashes.
                                StringComparer comparer = StringComparer.OrdinalIgnoreCase;

                                if (0 != comparer.Compare(md5, cmd5))
                                    throw new Exception("Контрольная сумма не верна!");
                            }

                            order1C = deserializeJSON<FSerialization.NewPartAggregate1СOrder>(json);
                            order1C.CheckContent();
                            //проверить номер линии
                            if (_lineNum != order1C.lineNum)
                                throw new FSerialization.JobFormatException("Неверно указан номер линии.");
                            //3B 6D 47 F8 AA 75 A6 6C 58 B5 2E EC 4D 19 F3 BE
                            /////////////


                            ////////////
                            //a8ef237f733d6638f6f563c7be6680ff
                            string fileName = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";
                            if (!Directory.Exists(fileName))
                                Directory.CreateDirectory(fileName);

                            //перекинуть слеши из пути  если надо
                            order1C.urlLabelBoxTemplate = order1C.urlLabelBoxTemplate.Replace("\u005c", "\u002f");

                            //загрузить файл шаблона коробки
                            if (!DownLoadFile(order1C.urlLabelBoxTemplate, fileName + "Box.tmpl"))
                                throw new Exception("Ошибка загрузки шаблона. url: " + order1C.urlLabelBoxTemplate);

                            string result = "Внутрення ошибка сервера";
                           
                                result = _job.AcceptOrderToWork(order1C,_config);

                            if (result != "")
                                return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", result));

                            _job.SaveOrder();

                            //сохранить в архив
                            FSerialization.WebUtil.SafeGzipFile<FSerialization.NewPartAggregate1СOrder>(order1C, "OrderArh", order1C.id);

                            Log.Write("Получено задание: " + order1C.id);
                            return Task.FromResult<IHttpResponseAction>(new JsonResponse(201, "Created", null));
                        }
                        #endregion

                    }
                }
            }
            catch (FSerialization.JobFormatException ex)
            {
                Log.Write(ex.Message,EventLogEntryType.Error, 1111);
                return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", ex.Message));
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message,EventLogEntryType.Error, 1112);
                return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", ex.Message));
            }

            #region old
            /*
            //если пришло задание от л3
            if (//(state.Request.ContentType == "application/json" ||
                // state.Request.ContentType=="application/json;charset=utf-8")&&
                    state.Request.HttpMethod == "POST" &&
                    state.Request.RawUrl == "/jobs" &&
                    state.Request.ContentLength64 > 0)
            {
                #region /jobs  Прием задания от L3
                string c = "";
               
                    //распарсить задание
                    try
                    {
                        string md5 = state.Request.Headers["Content-MD5"];

                        using (state.Request.InputStream)
                        {
                            StreamReader sr = new StreamReader(state.Request.InputStream);
                            c = sr.ReadToEnd();

                            System.IO.TextWriter logFile = new System.IO.StreamWriter("ReciveOrderArchiv.txt", false);
                            logFile.WriteLine(c);
                            logFile.Close();                      

                            string cmd5 = MD5Calc.CalculateMD5Hash(c);
                            //временно отключить проверку md5
                            // if (md5 != cmd5)
                            //     return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", null));

                            //если сушествует необработанное задание вернуть ошибку
                            if (Program.Disp.OrderInProgress)
                                return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", "Сервис в работе с другим заданием"));

                            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Order));
                            Order o = MD5Calc.DeserializeJSon<Order>(c);

                            
                            if(o == null)
                                return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", "Невозможно рапознать формат задания!"));

                            string result = o.CheckContent();
                            if (result != "")
                                return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", "Формат задания распознан. Но содержит некорретные данные." + result));

                            result = Program.Disp.AcceptOrderToWork(o);
                            if (result != "")
                            return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", "Задание не принято в работу. " + result));

                    }
                        //ответ все ок
                        return Task.FromResult<IHttpResponseAction>(new JsonResponse(201, "Created", null));
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex.Message,EventLogEntryType.Error, 1111);
                        return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", null));
                    }
                #endregion
            }
            else if (state.Request.RawUrl == "/op/barcode")
            {
                #region Прием с встроенного Грифона кодов коробок через веб
                string barcode;
                try
                {
                    if(state.Request.InputStream == null)
                        Log.Write("Ошибка распозначания данных с встроенного сканера");

                    using (state.Request.InputStream)
                    {
                        StreamReader sr = new StreamReader(state.Request.InputStream);
                        barcode = sr.ReadToEnd();
                        //проверить корректность посылки
                        if (barcode.Last() == '#')
                        {
                            Log.Write("Считан код коробки: " + barcode);
                            barcode = barcode.TrimEnd('#');
                            Program.l1Disp.ProcessBoxCode(barcode);
                        }else
                            Log.Write("Не корректная посылка с встроенного сканера");
                    }
                    //ответ все ок
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(201, "Created", null));
                }
                 catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1130);
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", null));
                }
            #endregion
            }
            else if (state.Request.RawUrl.Contains("/op/SystemState"))
            {
                #region запрос состояния системы через веб
               // string barcode;
                try
                {
                    // using (state.Request.InputStream)
                    // {
                    //     StreamReader sr = new StreamReader(state.Request.InputStream);
                    //      barcode = sr.ReadToEnd();
                    //     Log.Write("Считан код коробки: " + barcode);
                    // }

                    //DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Report));
                    //cгенерировать md5
                    //string s = SerializeJSon<Report>(Program.r);

                    //ответ все ок
                    //return Task.FromResult<IHttpResponseAction>(new JsonResponse(200, "OK", Program.systemState));
                    Program.systemState.UpdateAlarm();
                    return Task.FromResult<IHttpResponseAction>(new SystemSateResponse(201, "OK", Program.systemState));
                    //Order o = new Order();
                   // return Task.FromResult<IHttpResponseAction>(new MyResponse(200, "OK", o));
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1120);
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", null));
                }
                #endregion
            }
            else if (state.Request.RawUrl.Contains("/op/Event"))
            {
                #region прием событий системы через веб
                string command;
                try
                {
                    using (state.Request.InputStream)
                    {
                        StreamReader sr = new StreamReader(state.Request.InputStream);
                        command = sr.ReadToEnd();
                        Program.l1Disp.L1Dispetcher_OperatorCommandEvent(command,null);
                        //EventHtml he = MD5Calc.DeserializeJSon<EventHtml>(barcode);
                        //DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(EventHtml));
                       
                         Log.Write("Http Событие: " + command);
                     }

                    //
                    //cгенерировать md5
                    //string s = SerializeJSon<Report>(Program.r);

                    //ответ все ок
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(200, "OK", Program.systemState));
                   // return Task.FromResult<IHttpResponseAction>(new SystemSateResponse(201, "OK", Program.systemState));
                    //Order o = new Order();
                    // return Task.FromResult<IHttpResponseAction>(new MyResponse(200, "OK", o));
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1210);
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", null));
                }
                #endregion
            }
            else if (state.Request.RawUrl.Contains("/op/EvParam"))
            {
                #region прием событий с параметрами EventParam системы через веб
                string command;
                try
                {
                    using (state.Request.InputStream)
                    {
                        StreamReader sr = new StreamReader(state.Request.InputStream);
                        command = sr.ReadToEnd();
                        string[] s = command.Split('#');
                        if(s.Length > 1)
                            Program.l1Disp.L1Dispetcher_OperatorCommandEvent(s[0],s);
                        //EventHtml he = MD5Calc.DeserializeJSon<EventHtml>(barcode);
                        //DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(EventHtml));

                        Log.Write("Http Событие: " + command);
                    }

                    //
                    //cгенерировать md5
                    //string s = SerializeJSon<Report>(Program.r);

                    //ответ все ок
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(200, "OK", Program.systemState));
                    // return Task.FromResult<IHttpResponseAction>(new SystemSateResponse(201, "OK", Program.systemState));
                    //Order o = new Order();
                    // return Task.FromResult<IHttpResponseAction>(new MyResponse(200, "OK", o));
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1210);
                    return Task.FromResult<IHttpResponseAction>(new JsonResponse(500, "Internal Server Error", null));
                }
                #endregion
            }
            else// if (state.Request.RawUrl == "/op")
            {
                #region весь файловый веб сервис
                try
                {
                    string page = state.Request.RawUrl.Replace("/", "\\").Replace("\\..", ""); // Not to go back
                    int start = page.LastIndexOf('.') + 1;
                    if (start > 0)
                    {
                        int length = page.Length - start;
                        string extension = page.Substring(start, length);
                        if (extensions.ContainsKey(extension)) // Мы поддерживаем это расширение?
                        {
                            string fileName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\www" + page;

                            if (File.Exists(fileName)) // Если да
                            {
                                // response.ContentType = extensions[extension];
                                //  response.StatusCode = 200;
                                //  response.StatusDescription = "OK";
                                //   response.KeepAlive = true;
                                //   response.SendChunked = true;

                                //response.Conten
                                //    byte[] buf = File.ReadAllBytes(Directory.GetCurrentDirectory() + "\\www" + page);


                                //  response.OutputStream.Write(buf, 0, buf.Length);
                                return Task.FromResult<IHttpResponseAction>(new FileResponse(200, "OK", fileName, extensions[extension]));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1121);

                }
                #endregion
            }*/
            #endregion
            return Task.FromResult<IHttpResponseAction>(new JsonResponse(400, "Bad Request", null));
        }

        private bool DownLoadFile(string sUrl, string safePath)
        {
            try
            {
                
                if (sUrl == null)
                    throw new Exception("Ошибка шаблона");
                // Create a new WebClient instance.
                System.Net.WebClient myWebClient = new System.Net.WebClient();
                Uri url = new Uri(sUrl);
                //**************авторизация***********
                System.Net.NetworkCredential myNetworkCredential = new System.Net.NetworkCredential(
                    _srv1CLogin,
                    _srv1CPass);

                System.Net.CredentialCache myCredentialCache = new System.Net.CredentialCache();
                myCredentialCache.Add(url, "Basic", myNetworkCredential);

                myWebClient.Credentials = myCredentialCache;
                //*************************
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(url, safePath);
                return true;

            }
            catch (ArgumentNullException ex)
            {
                Log.Write("DownLoadFile  ArgumentNullException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (WebException ex)
            {
                if (ex.InnerException != null)
                    Log.Write("DownLoadFile  WebException\nОшибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОшибка источника " + ex.InnerException.Message + "\nОтвет сервера" + ex.Response,EventLogEntryType.Error, 701);
                else
                    Log.Write("DownLoadFile  WebException\nОшибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОтвет сервера" + ex.Response,EventLogEntryType.Error, 701);

            }
            catch (NotSupportedException ex)
            {
                Log.Write("DownLoadFile  NotSupportedException " + ex.Message,EventLogEntryType.Error, 701);
            }
            catch (Exception ex)
            {
                Log.Write("DownLoadFile " + ex.Message,EventLogEntryType.Error, 701);
            }
            return false;
        }

        public static string SerializeJSon<T>(T t)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T));
            DataContractJsonSerializerSettings s = new DataContractJsonSerializerSettings();
            ds.WriteObject(stream, t);
            string jsonString = Encoding.UTF8.GetString(stream.ToArray());
            stream.Close();
            return jsonString;
        }

        public static T deserializeJSON<T>(string json)
        {
            var instance = typeof(T);
            // var lst = new List<SomeDataClass>();

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var deserializer = new DataContractJsonSerializer(instance);
                return (T)deserializer.ReadObject(ms);
            }
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public sealed class SystemSateResponse : StatusResponse, IHttpResponseAction
    {
        readonly object value;

        public SystemSateResponse(int statusCode, string statusDescription, object v)
            : base(statusCode, statusDescription)
        {
            value = v;
           
        }
#pragma warning disable CS1998
        public override async Task Execute(IHttpRequestResponseContext context)
        {
            SetStatus(context);
            context.Response.ContentType = "plain/text"; //"application/json";//; charset=utf-8""text/xml";
                                                              //context.Response.ContentEncoding = UTF8.WithoutBOM;
                                                              //using (context.Response.OutputStream)

            using (context.OutputStream)
            {
                var tw = new StreamWriter(context.Response.OutputStream, UTF8.WithoutBOM);
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(value.GetType());
                jsonSerializer.WriteObject(tw.BaseStream, value);

                //Program.systemState.GotoControl = false;
            }
        }
#pragma warning restore CS1998
    }

    public sealed class FileResponse : StatusResponse, IHttpResponseAction
    {
        readonly string value;
        readonly string content;
        public FileResponse(int statusCode, string statusDescription, string v,string c)
            : base(statusCode, statusDescription)
        {
            value = v;
            content = c;
        }
#pragma warning disable CS1998
        public override async Task Execute(IHttpRequestResponseContext context)
        {
            SetStatus(context);
            context.Response.ContentType = content;
            //context.Response.ContentEncoding = UTF8.WithoutBOM;

            using (context.Response.OutputStream)
            {

                //var tw = new StreamWriter(context.Response.OutputStream, UTF8.WithoutBOM);
                byte[] buf = File.ReadAllBytes(value);
                context.Response.OutputStream.Write(buf, 0, buf.Length);
                //DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(value.GetType());
                //jsonSerializer.WriteObject(context.Response.OutputStream, value);

                //httpWebRequest.GetRequestStream().Flush();
                // httpWebRequest.GetRequestStream().Close();

                // Json.Serializer.Serialize(tw, _value);
            }
        }
#pragma warning restore CS1998
    }

    public sealed class MyResponse : StatusResponse, IHttpResponseAction
    {
        readonly object value;

        public MyResponse(int statusCode, string statusDescription, object v)
            : base(statusCode, statusDescription)
        {
            value = v;
        }
#pragma warning disable CS1998
        public override async Task Execute(IHttpRequestResponseContext context)
        {
            SetStatus(context);
            context.Response.ContentType = "application/json; charset=utf-8";
            //context.Response.ContentEncoding = UTF8.WithoutBOM;

            using (context.Response.OutputStream)
            {
                var tw = new StreamWriter(context.Response.OutputStream, UTF8.WithoutBOM);

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(value.GetType());
                jsonSerializer.WriteObject(context.Response.OutputStream, value);

                //httpWebRequest.GetRequestStream().Flush();
               // httpWebRequest.GetRequestStream().Close();

                // Json.Serializer.Serialize(tw, _value);
            }
        }
#pragma warning restore CS1998
    }
}
