using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace FSerialization
{
    public static class Archive
    {
        public static string SerializeJSon<T>(T t)
        {
            try
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T));
                DataContractJsonSerializerSettings s = new DataContractJsonSerializerSettings();
                ds.WriteObject(stream, t);
                string jsonString = Encoding.UTF8.GetString(stream.ToArray());
                stream.Close();
                return jsonString;
            }
            catch (Exception ex)
            {
                Util.Log.Write(ex.Message);
            }
            return "";
        }
        public static T DeserializeJSon<T>(string jsonString) where T:class
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                T obj = (T)ser.ReadObject(stream);
                return obj;
            }
            catch (Exception ex)
            {
                Util.Log.Write(ex.Message +" \nStoredData: "+ jsonString);
            }
            return null;
        }
        public static T RestoreReport<T>( string id)
        {
            return (T)RestoreReport(typeof(T), id);
        }
        public static object RestoreReport(Type t, string id)
        {
            // T disp = null;

            string fileName = System.IO.Path.GetDirectoryName(
               System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\" + id + ".rep";

            using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
            {
                string s = tmpFile.ReadToEnd();
                tmpFile.Close();
                tmpFile.Dispose();

                DataContractJsonSerializer ser = new DataContractJsonSerializer(t);
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
                object obj = ser.ReadObject(stream);

                //BaseJobInfo bj = obj as BaseJobInfo;
                //if (bj == null)
                //    throw new Exception("Не удалось восстановаить задание ID:" + id);
                //добавлено задание 
                //Util.Log.Write("Восстановлен отчет по  заданию ID:  " + id);
                return obj;

            }
            //return null;
        }
        public static void SaveReport<T>(T t, string id) where T : class
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    string fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\" + id + ".rep";

                    //if (!System.IO.Directory.Exists(fileName))
                    //    System.IO.Directory.CreateDirectory(fileName);

                    string data = SerializeJSon<T>(t);

                    using (System.IO.TextWriter tmpFile = new System.IO.StreamWriter(fileName, false))
                    {
                        tmpFile.Write(data);
                        tmpFile.Close();
                        tmpFile.Dispose();
                    }

                    //return true;

                }
                catch (Exception ex)
                {
                    ex.ToString();
                    Util.Log.Write("Ошибка сохранения отчета!: " + ex.Message, EventLogEntryType.Error, 701);
                }
                finally
                {
                    //  orderSaveSync.ExitWriteLock();
                }
            }).Start();
            //  }
            //   else
            //   {
            //      Log.Write("SaveOrder Критическая ошибка очереди",EventLogEntryType.Error, MAIN_ERROR_CODE + 702);
            //  }
           // return false;
        }
    }
}
