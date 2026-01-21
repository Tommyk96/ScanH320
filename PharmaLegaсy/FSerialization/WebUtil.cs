using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Threading;

//using System.Net.Http.Headers;

using System.Collections.Generic;

using System.Text;
using Util;
using System.Threading.Tasks;

namespace FSerialization
{
    public static class WebUtil
    {
        private static ReaderWriterLockSlim gzipWr = new ReaderWriterLockSlim();
        public static string SendReport<T>(string uri,string user,string pass,string metod, T r,string storeId,string orderid,int reguestTimeOut) where T:class
        {

            if (r == null)
                return "Передача в 1с невозможна. Отчет не сформирован!";

            try {

                Uri myUri = new Uri(uri);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(myUri);
                httpWebRequest.ContentType = "application/json";//"application/json;charset=utf-8";
                httpWebRequest.Method = "POST";// metod;// "POST" или "PUT";
                httpWebRequest.Timeout = reguestTimeOut;// 600000;

                //**************авторизация***********
                //if (false)
                //{ //старая авторизация нигде не проверенная
                //    NetworkCredential myNetworkCredential = new NetworkCredential(user, pass);

                //    CredentialCache myCredentialCache = new CredentialCache();
                //    myCredentialCache.Add(myUri, "Basic", myNetworkCredential);

                //    httpWebRequest.PreAuthenticate = true;
                //    //httpWebRequest.AuthenticationLevel = Sy
                //    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                //    httpWebRequest.Credentials = myCredentialCache;
                //}
                //else
                //{
                    //работающая версия. проверено на сервере апача
                    byte[] credentialBuffer = new UTF8Encoding().GetBytes($"{user}:{pass}");
                    httpWebRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credentialBuffer));
                //}
                //*************************

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(T));

                //cгенерировать md5
                string s = FSerialization.Archive.SerializeJSon<T>(r);
                httpWebRequest.Headers.Add(HttpRequestHeader.ContentMd5, CalculateMD5Hash(s));

                //сохранить отчет на диск
                string fileName = System.IO.Path.GetDirectoryName(
                  System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\ReportsArchiv";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);


                using (FileStream compressedFileStream = File.Create($@"{fileName}\.{orderid}.{storeId}rgz"))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                       CompressionMode.Compress))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(s);
                        compressionStream.Write(data, 0, data.Length);
                        compressionStream.Close();
                    }
                }

                //передать на сервер 1С
                jsonSerializer.WriteObject(httpWebRequest.GetRequestStream(), r);

                httpWebRequest.GetRequestStream().Flush();
                httpWebRequest.GetRequestStream().Close();

          
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                string result;
                using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                
                //проверить статус код
                if (httpResponse.StatusCode == HttpStatusCode.Created)
                {
                    Log.Write("Отчет передан успешно. id:" + orderid, EventLogEntryType.Information, 1000 + 17);
                    return "";
                }
                Log.Write("Не возможно передать отчет на сервер. Ответ сервера: " + result, EventLogEntryType.Error, 1000 + 18);
                return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";

            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    Log.Write("Не возможно передать отчет на сервер. " + ex.Message,  EventLogEntryType.Error, 1000 + 19);
                    return "Не возможно передать отчет на сервер. " + ex.Message +".";
                }

                try
                {
                    using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                    {
                        string err = reader.ReadToEnd();
                        Log.Write("Не возможно передать отчет на сервер. " + ex.Message + " Ответ сервера: " + err, EventLogEntryType.Error, 1000 + 19);
                        return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";
                    }
                }
                catch
                {

                }
            }
            catch (Exception ex)
            {
                Log.Write("Не возможно передать отчет на сервер. " + ex.Message, EventLogEntryType.Error, 1000 + 20);
                return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";
            }
            return "Error";
        }
        public static async Task<string> SendReportAsync<T>(string uri, string user, string pass, string metod, T r,
            string storeId, string orderid, int reguestTimeOut, CancellationToken token) where T : class
        {

            if (r == null)
                return "Передача в 1с невозможна. Отчет не сформирован!";

            try
            {

                Uri myUri = new Uri(uri);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(myUri);
                httpWebRequest.ContentType = "application/json";//"application/json;charset=utf-8";
                httpWebRequest.Method = "POST";// metod;// "POST" или "PUT";
                httpWebRequest.Timeout = reguestTimeOut;// 600000;

               
                //работающая версия. проверено на сервере апача
                byte[] credentialBuffer = new UTF8Encoding().GetBytes($"{user}:{pass}");
                httpWebRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(credentialBuffer));
                //}
                //*************************

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(T));

                //cгенерировать md5
                string s = FSerialization.Archive.SerializeJSon<T>(r);
                httpWebRequest.Headers.Add(HttpRequestHeader.ContentMd5, CalculateMD5Hash(s));

                //сохранить отчет на диск
                string fileName = System.IO.Path.GetDirectoryName(
                  System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\ReportsArchiv";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);


                using (FileStream compressedFileStream = File.Create(fileName + "\\" + storeId + ".rgz"))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                       CompressionMode.Compress))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(s);
                        compressionStream.Write(data, 0, data.Length);
                        compressionStream.Close();
                    }
                }

                //передать на сервер 1С
                jsonSerializer.WriteObject(httpWebRequest.GetRequestStream(), r);

                httpWebRequest.GetRequestStream().Flush();
                httpWebRequest.GetRequestStream().Close();


                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                string result;
                using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                //проверить статус код
                if (httpResponse.StatusCode == HttpStatusCode.Created)
                {
                    Log.Write("Отчет передан успешно. id:" + orderid, EventLogEntryType.Information, 1000 + 17);
                    return "";
                }
                Log.Write("Не возможно передать отчет на сервер. Ответ сервера: " + result, EventLogEntryType.Error, 1000 + 18);
                return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";

            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    Log.Write("Не возможно передать отчет на сервер. " + ex.Message, EventLogEntryType.Error, 1000 + 19);
                    return "Не возможно передать отчет на сервер. " + ex.Message + ".";
                }

                try
                {
                    using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                    {
                        string err = reader.ReadToEnd();
                        Log.Write("Не возможно передать отчет на сервер. " + ex.Message + " Ответ сервера: " + err, EventLogEntryType.Error, 1000 + 19);
                        return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";
                    }
                }
                catch
                {

                }
            }
            catch (Exception ex)
            {
                Log.Write("Не возможно передать отчет на сервер. " + ex.Message, EventLogEntryType.Error, 1000 + 20);
                return "Не возможно передать отчет на сервер. обратитесь в службу поддержки";
            }
            return "Error";
        }
        public static string CalculateMD5Hash(string input)
        {

                // step 1, calculate MD5 hash from input

                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

                byte[] hash = md5.ComputeHash(inputBytes);

                // step 2, convert byte array to hex string

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)

                {

                    sb.Append(hash[i].ToString("X2"));

                }


            return sb.ToString();
            }
        public static string SafeGzipFile<T>( T r, string dir,string _fileName) where T : class
        {
            if (r == null)
                return "!";

            if (gzipWr.TryEnterWriteLock(200))
            {
                try
                {

                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(T));
                    //cгенерировать md5
                    string s = FSerialization.Archive.SerializeJSon<T>(r);
                    if (s == "")
                        return "!2";

                    //сохранить отчет на диск
                    string fileName = System.IO.Path.GetDirectoryName(
                      System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\"+ dir;

                    if (!System.IO.Directory.Exists(fileName))
                        System.IO.Directory.CreateDirectory(fileName);


                    using (FileStream compressedFileStream = File.Create(fileName + "\\" + _fileName + ".ogz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            byte[] data = Encoding.UTF8.GetBytes(s);
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Close();
                        }
                    }
                    return "";
                }
                catch (SecurityException ex)
                {
                    Util.Log.Write(ex.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    Util.Log.Write(ex.ToString());
                }
                catch (ArgumentException ex)
                {
                    Util.Log.Write(ex.ToString());
                }
                catch (Exception ex)
                {
                    Util.Log.Write(ex.ToString());
                }
                finally
                {
                    gzipWr.ExitWriteLock();
                }
            }

            return "!1";
        }
        public static bool DownLoadFile(string sUrl, string user, string pass, string safePath, string filename)
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
                    user,pass);

                System.Net.CredentialCache myCredentialCache = new System.Net.CredentialCache();
                myCredentialCache.Add(url, "Basic", myNetworkCredential);

                myWebClient.Credentials = myCredentialCache;
                //*************************

                if (!System.IO.Directory.Exists(safePath))
                    System.IO.Directory.CreateDirectory(safePath);

                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(url, safePath + filename);
                return true;

            }
            catch (ArgumentNullException ex)
            {
                Log.Write("DownLoadFile Запрос:" + sUrl + " ArgumentNullException " + ex.Message, EventLogEntryType.Error, 701);
            }
            catch (WebException ex)
            {
                if (ex.InnerException != null)
                    Log.Write("DownLoadFile  WebException\nЗапрос:" + sUrl + "Ошибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОшибка источника " + ex.InnerException.Message + "\nОтвет сервера" + ex.Response, EventLogEntryType.Error, 701);
                else
                    Log.Write("DownLoadFile  WebException\nЗапрос:" + sUrl + "Ошибка:" + ex.Message + "\nСтатус:" + ex.Status + "\nОтвет сервера" + ex.Response, EventLogEntryType.Error, 701);

            }
            catch (NotSupportedException ex)
            {
                Log.Write("DownLoadFile Запрос:" + sUrl + " NotSupportedException " + ex.Message, EventLogEntryType.Error, 701);
            }
            catch (Exception ex)
            {
                Log.Write("DownLoadFile Запрос:" + sUrl + "" + ex.Message, EventLogEntryType.Error, 701);
            }
            return false;
        }

    }
   

}
