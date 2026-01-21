using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Util;


namespace Peripherals
{
    public class PrinterBox
    {
        static public bool Print(string commandString, string ip, int port)
        {
            System.Diagnostics.Stopwatch w = new ();
            try
            {
                ////////////////////////////////////////
                System.IO.TextWriter streamWriterOut = null;
                //создать директорию отчётов если её нет
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\lastPrint.txt";
                //
                try
                {
                    //удалить предыдущий отчет если он какимто чудом есть
                    System.IO.File.Delete(path);
                    streamWriterOut = new System.IO.StreamWriter(path, true, System.Text.Encoding.Default);

                    streamWriterOut.WriteLine(commandString);
                }
                catch (Exception ex)
                {
                    string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
                    Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                }
                finally
                {
                    if (streamWriterOut != null)
                        streamWriterOut.Close();
                }
                ////////////////////////////////////////
                w.Start();
                //using (TcpClient connection = new TcpClientWithTimeout(ip, port, 1000).Connect())
                using (TcpClient connection = new TcpClient())
                {

                    connection.Connect(ip, port);
                    if (!connection.Connected)
                        return false;

                    connection.SendTimeout = 1000; // 10 second timeout on the send
                    NetworkStream stream = connection.GetStream();

                    // Send 10 bytes
                    byte[] to_send = Encoding.UTF8.GetBytes(commandString);
                    // { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xa };
                    stream.Write(to_send, 0, to_send.Length);

                   

                    // Disconnect nicely
                    stream.Close(); // workaround for a .net bug: http://support.microsoft.com/kb/821625
                    connection.Close();
                    //connection.Dispose();
                    return true;
                }
            }
            catch (ArgumentNullException ex)
            {
                Log.Write("Критическая ошибка Print." + ex.Message,EventLogEntryType.Error, 1201);
                return false;
            }
            catch (SocketException ex)
            {
                Log.Write("Критическая ошибка Print." + ex.Message,EventLogEntryType.Error, 1202);
                return false;
            }
            finally
            {
                Log.Write("Время обмена с принтером" + w.Elapsed.ToString());
            }
        }
        static public bool Print(byte[] data, string ip, int port)
        {
            System.Diagnostics.Stopwatch w = new ();
            try
            {
                ////////////////////////////////////////
                System.IO.BinaryWriter streamWriterOut = null;
                //создать директорию отчётов если её нет
                string path = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\lastPrint.txt";
                //
                try
                {
                    //удалить предыдущий отчет если он какимто чудом есть
                    System.IO.File.Delete(path);
                    using (streamWriterOut = new System.IO.BinaryWriter(File.Open(path, FileMode.Create)))
                    {
                        if (data != null)
                            streamWriterOut.Write(data);
                    }
                }
                catch (Exception ex)
                {
                    string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
                    Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                }
                finally
                {
                    if (streamWriterOut != null)
                        streamWriterOut.Close();
                }
                ////////////////////////////////////////
                w.Start();
                //using (TcpClient connection = new TcpClientWithTimeout(ip, port, 1000).Connect())
                using (TcpClient connection = new TcpClient())
                {

                    //connection.Connect(ip, port);
                    //if(!connection.Connected)
                    //    return false;
                    var result = connection.BeginConnect(ip, port, null, null);

                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(2000));

                    if (!success)
                    {
                        //return false; 
                        throw new Exception($"Невозможно соеденится с принтером {ip}:{port}");
                    }

                    // we have connected
                    connection.EndConnect(result);

                    connection.SendTimeout = 1000; // 10 second timeout on the send
                    NetworkStream stream = connection.GetStream();

                    System.Threading.Thread.Sleep(200);
                    // Send 10 bytes
                   // byte[] to_send = Encoding.UTF8.GetBytes(commandString);
                    // { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xa };
                    stream.Write(data, 0, data.Length);


                    System.Threading.Thread.Sleep(800);
                    // Disconnect nicely
                    stream.Close(); // workaround for a .net bug: http://support.microsoft.com/kb/821625
                    connection.Close();
                    //connection.Dispose();
                    return true;
                }
            }
            catch (ArgumentNullException ex)
            {
                Log.Write("Критическая ошибка аргументов Print." + ex.ToString(),EventLogEntryType.Error, 1201);
                throw ;
                //return false;
            }
            catch (SocketException ex)
            {
                Log.Write("Критическая ошибка сети Print." + ex.ToString(),EventLogEntryType.Error, 1202);
                throw ;
                //return false;
            }
            finally
            {
                Log.Write("Время обмена с принтером" + w.Elapsed.ToString());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandString"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        static public bool SendTemplateToPrinter( string ip, int port)
        {
            System.Diagnostics.Stopwatch w = new ();
            string sData="";
            try
            {
                ////////////////////////////////////////
                //System.IO.TextWriter streamWriterOut = null;
                //создать директорию отчётов если её нет
                string fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl";
                //
                try
                {
                    using (TextReader tr = new StreamReader(fileName)) {
                        sData = tr.ReadToEnd();
                        tr.Close();
                    }                      
                }
                catch (Exception ex)
                {
                    string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, fileName);
                    Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                } 
                ////////////////////////////////////////
                w.Start();
                //using (TcpClient connection = new TcpClientWithTimeout(ip, port, 1000).Connect())
                using (TcpClient connection = new TcpClient())
                {

                    connection.Connect(ip, port);
                    if (!connection.Connected)
                        return false;

                    connection.SendTimeout = 1000; // 10 second timeout on the send
                    NetworkStream stream = connection.GetStream();

                    // Send 10 bytes
                    byte[] to_send = Encoding.UTF8.GetBytes(sData);
                    
                    stream.Write(to_send, 0, to_send.Length);


                    // Disconnect nicely
                    stream.Close(); // workaround for a .net bug: http://support.microsoft.com/kb/821625
                    connection.Close();

                    //L2DataService.Program.systemState.PrinterBox = (int)DeviceState.good;
                    return true;
                }
            }
            catch (ArgumentNullException ex)
            {
                //L2DataService.Program.systemState.PrinterBox = (int)DeviceState.bad;
                Log.Write("Критическая ошибка Print." + ex.Message,EventLogEntryType.Error, 1201);
                return false;
            }
            catch (SocketException ex)
            {
                //L2DataService.Program.systemState.PrinterBox = (int)DeviceState.bad;
                Log.Write("Критическая ошибка Print." + ex.Message,EventLogEntryType.Error, 1202);
                return false;
            }
            catch(TimeoutException ex)
            {
                ex.ToString();
                //L2DataService.Program.systemState.PrinterBox = (int)DeviceState.unknown;
                throw new Exception("Не удалось соеденится с принтером групповых этикеток: "+ ip+":"+port.ToString() + " за отведённое время.");
                //return false;
            }
            finally
            {
                Log.Write("Время обмена с принтером" + w.Elapsed.ToString());
                w.Stop();
            }
        }
    }

}
