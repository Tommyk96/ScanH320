using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Globalization;

namespace Autorization
{
    public class HttpRequestResult
    {
        public HttpStatusCode resultCode;
        public string resultData;

    }
    //
    //http://remote.drgrp.ru/FarmaKR/hs/InfoTech/Authorization
    //http://192.168.3.33:7085/jobs/authorize
    public static class AuthUser1C
    {
        public static T GetReguest<T>(string requestUrl, out HttpRequestResult result, string lineUser, string linePass, string user, string pass) where T : User1C
        {
            result = new HttpRequestResult();
            T resultData;
            try
            {
                //получить данные по заданию с сервера
                Uri url = new Uri(requestUrl);
                var httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";//"application/json;charset=utf-8";
                httpWebRequest.Method = "GET";
                httpWebRequest.AllowAutoRedirect = true;
                httpWebRequest.Timeout = 10000;
                //**************авторизация***********
                System.Net.NetworkCredential myNetworkCredential = new System.Net.NetworkCredential(lineUser, linePass);

                System.Net.CredentialCache myCredentialCache = new System.Net.CredentialCache();
                myCredentialCache.Add(url, "Basic", myNetworkCredential);

                httpWebRequest.PreAuthenticate = true;
                httpWebRequest.Credentials = myCredentialCache;
                //*************************
                //авторизация пользователя на линии
                //string token =  L2DataService.MD5Calc.CalculateMD5Hash(user + L2DataService.MD5Calc.CalculateMD5Hash(pass)); //
                string token = CalcUserToken1C(user, pass);

                httpWebRequest.Headers.Add("UserAuthToken", token);
                //*************************
                try
                {
                    var httpResponse = (System.Net.HttpWebResponse)httpWebRequest.GetResponse();
                    result.resultCode = httpResponse.StatusCode;


                    //проверить статус код
                    //временно считать код 201 тоже успешным. ошибка на стороне 1с
                    if ((httpResponse.StatusCode != HttpStatusCode.OK) && (httpResponse.StatusCode != HttpStatusCode.Created))
                        throw new WebException("", new Exception(), WebExceptionStatus.RequestCanceled, httpResponse);

                    //получен ок загрузить и распарсить
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        string json = streamReader.ReadToEnd();

                        //257300,00

                        var instance = typeof(T);
                        using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                        {
                            var deserializer = new DataContractJsonSerializer(instance);
                            resultData = (T)deserializer.ReadObject(ms);
                        }

                        //T resultData = JsonConvert.DeserializeObject<T>(json);
                        if (resultData == null)
                            throw new Exception("Ответ получен, но данные пользователя не распознаны?!");

                        httpResponse.Close();
                        resultData.Hash = token;
                        return resultData;
                    }

                    //httpResponse.Close();
                    //return null;
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            try
                            {
                                //прочитать стрим ответа сервера
                                using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
                                {
                                    result.resultData = streamReader.ReadToEnd();
                                }
                                ex.Response.Close();

                                result.resultCode = response.StatusCode;
                                result.resultData = ex.Message;
                                return null;
                            }
                            catch { }
                        }
                    }
                    // no http status code available
                    result.resultCode = HttpStatusCode.SeeOther;
                    result.resultData = ex.Message;
                    return null;
                }
                catch (ArgumentNullException ex)
                {
                    result.resultCode = HttpStatusCode.SeeOther;
                    result.resultData = ex.Message;
                }
                catch (NotSupportedException ex)
                {
                    result.resultCode = HttpStatusCode.SeeOther;
                    result.resultData = ex.Message;
                }
                catch (Exception ex)
                {
                    result.resultCode = HttpStatusCode.SeeOther;
                    result.resultData = ex.Message;
                }
            }
            catch (Exception ex)
            {
                result.resultCode = HttpStatusCode.SeeOther;
                result.resultData = ex.Message;
            }
            return null;
        }

#pragma warning disable CA5350 
#pragma warning disable CA5351 
        public static string CalcUserToken1C(string login, string pass)
        {
            //Перевести пароль в нижний регистр
            byte[] data = Encoding.Default.GetBytes(pass.ToLower(CultureInfo.InvariantCulture));

            using (System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                //SHA1.
                byte[] result = sha.ComputeHash(data);
                //перевести массив хеша в строку  представляюшую его в виде base64 (шестнадцатиричных значений)
                string passSha1 = ByteArrayToString(result);//для пароля 101 - dbc0f004854457f59fb16ab863a3a1722cef553f
                //Перевести логин в нижний регистр и добавить к нему хеш пароля
                string stdData = login.ToLower(CultureInfo.InvariantCulture) + passSha1; // логин и пароль 101 : 101dbc0f004854457f59fb16ab863a3a1722cef553f

                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(stdData);
                    //MD5
                    byte[] md5Hash = md5.ComputeHash(inputBytes);
                    //перевести массив хеша в строку  представляюшую его в виде base64 (шестнадцатиричных значений) и вернуть её как готовый токен
                    return ByteArrayToString(md5Hash);
                }
            }
        }
#pragma warning restore CA5350 
#pragma warning restore CA5351 
        public static string ByteArrayToString(byte[] ba)
        {
            if (ba == null)
                return "";

            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat(CultureInfo.InvariantCulture,"{0:x2}", b);
            return hex.ToString();
        }
    }

    [DataContract]
    public class User1C
    {
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public bool Master { get; set; }
        [DataMember]
        public bool ServiceMan { get; set; }
        [DataMember]
        public bool Operator { get; set; }
        [DataMember]
        public bool Сontroller { get; set; }

        public string Hash { get; set; }
    }
}
