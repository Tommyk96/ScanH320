using FSerialization;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Util;

namespace AgrBox.http
{

    public static class HttpHelper
    {
        private const int MAIN_ERROR_CODE = 11000;
        public static TimeSpan RequestTimeout { get; set; } = new TimeSpan(0, 0, 5);

        public static async Task<string> SendReport<T>(string uri, string user, string pass, string metod, T r,
            string storeId, string orderid, int reguestTimeOut, System.Threading.CancellationToken token) where T : class
            //SendReport(string url, string legalEntity, string code, System.Threading.CancellationToken token)
        {
            var handler = new HttpClientHandler();
            using (HttpClient client = new HttpClient(handler, true) { Timeout = RequestTimeout  })
            {
                try
                {  
                    byte[] bytes = new UTF8Encoding().GetBytes(user + ":" + pass);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
                  
                    //client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(bytes));

                    // Set relevant properties from ServicePointManager
                    System.Net.Security.RemoteCertificateValidationCallback rcvc = ServicePointManager.ServerCertificateValidationCallback;
                    if (rcvc != null)
                    {
                        System.Net.Security.RemoteCertificateValidationCallback localRcvc = rcvc;
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => localRcvc(null, cert, chain, errors);
                    }

                    //создать запрос
                    string json = Archive.SerializeJSon<T>(r);

                    string text2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\ReportsArchiv";
                    if (!Directory.Exists(text2))
                    {
                        Directory.CreateDirectory(text2);
                    }

                    using (FileStream stream = File.Create(text2 + "\\." + orderid + "." + storeId + "rgz"))
                    {
                        using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress))
                        {
                            byte[] bytes2 = Encoding.UTF8.GetBytes(json);
                            gZipStream.Write(bytes2, 0, bytes2.Length);
                            gZipStream.Close();
                        }
                    }

                    using (StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                    {
                        var requestResult = await client.PostAsync($@"{uri}", httpContent, token);
                        if (requestResult.IsSuccessStatusCode)
                        {
                            Log.Write("Отчет передан успешно. id:" + orderid, EventLogEntryType.Information, 1017);
                            return "";
                        }
                        else
                        {
                            Log.Write("HRQ", $"Ошибка отправки сервер вернул {requestResult.StatusCode}\n{requestResult.ReasonPhrase}",EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                            return $"Ошибка отправки сервер вернул {requestResult.StatusCode}: {requestResult.ReasonPhrase}";
                        }
                    }
                }
                catch (ArgumentNullException ex)
                {
                    string msg = $"Не удалось отправить отчет: {ex.Message}";
                    Log.Write("HRQ", msg,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;
                }
                catch (HttpRequestException ex)
                {
                    string msg = $"Не удалось отправить отчет: {ex.Message}";
                    Log.Write("HRQ", msg,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;
                }
                catch (TaskCanceledException ex)
                {
                    //ex.ToString();
                    // Log.Write("HRQ", $"Получение списка заданий прервано оператором",EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    string msg = $"Не удалось отправить отчет. прервано оператором";
                    if (ex.InnerException?.HResult != -2146232800)//- 2146233029)//0x8013153B)
                        msg = ex.Message;
                    Log.Write("HRQ", ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;

                }
                catch (JsonException ex)
                {
                    string msg = $"Не удалось отправить отчет: {ex.Message}";
                    Log.Write("HRQ", msg,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;
                }
                catch (NotSupportedException ex)
                {
                    string msg = $"Не удалось отправить отчет: {ex.Message}";
                    Log.Write("HRQ", msg,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;
                }
                catch (Exception ex)
                {
                    string msg = $"Не удалось отправить отчет: {ex.Message}. {ex?.InnerException?.Message}. {ex?.InnerException?.InnerException?.Message}. {ex?.InnerException?.InnerException?.InnerException?.Message}";
                    Log.Write("HRQ", msg,EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                    return msg;
                }
                return "";
            }
        }
        public static void IgnoreSSLCertificat()
        {
            if (ServicePointManager.ServerCertificateValidationCallback == null)
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) =>
                {
                    return true;
                };



        }
    }
}