using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgrBox
{
    public class PlcMsgUtilite
    {
        private byte[] comStoreBuffer1 = new byte[102048];
        private int comStoreBuffer1DateSize = 0;
        //private static long counter = 0;



        /// <summary>
        /// собирает посылку с ком порта в одну строку
        /// неверные данные отбрасывает
        /// 
        /// если стартовый символ равен 0 то считается что старта нет. тоесть любой символ будет стартовым
        /// </summary>
        /// <param name="rcvData"></param>
        /// <param name="rcvSize"></param>
        /// <returns></returns>
        public string GetMessAtStartOld(byte[] rcvData, int rcvSize, byte StartChar, byte StopChar)
        {
            int startIndex = 0;
            bool startFound = false;
            int endIndex = 0;
            bool endFound = false;
            string badStrdata = "";

            //byte[] chars = Encoding.UTF8.GetBytes("+-");
            // byte[] chars = new byte[2] { 2, 3 };


            byte[] chars = new byte[2] { StartChar, StopChar };

            try
            {
                //если посылка пуста обработать уже имеющейся буфер
                if (rcvData != null)
                {
                    //сложить предыдущий буфер и вновь поступивший 
                    Array.Copy(rcvData, 0, comStoreBuffer1, comStoreBuffer1DateSize, rcvSize);
                    comStoreBuffer1DateSize += rcvSize;
                }
                else if (comStoreBuffer1DateSize == 0)
                    return "";


                //найти старт посылки
                foreach (byte b in comStoreBuffer1)
                {
                    //если chars[0] == 0 то считать любой символ началом пакета
                    if ((b == chars[0]) || (chars[0] == 0))
                    {
                        startFound = true;
                        break;
                    }
                    startIndex++;
                }
                //если старт не найден сбросить посылку как мусор
                if (!startFound)
                {
                    comStoreBuffer1DateSize = 0;
                    Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                    return "";
                }

                //найти стоп посылки
                foreach (byte b in comStoreBuffer1)
                {
                    if (b == chars[1])
                    {
                        endFound = true;
                        break;
                    }
                    endIndex++;
                }
                //если стоп не найден выйти
                if (!endFound)
                {
                    //если размер пакета  превышает допустимый сбросить посылку как мусор
                    if (comStoreBuffer1DateSize >= 80)
                    {
                        comStoreBuffer1DateSize = 0;
                        Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                        return "";
                    }

                    return "";
                }
                //убедится что сначал старт потом стоп :)
                //иначе сбросить все до старта и вернуть реинит!
                if (endIndex < startIndex)
                {
                    //выделить потерянные данные
                    byte[] badData = new byte[startIndex];
                    Array.Copy(comStoreBuffer1, 0, badData, 0, startIndex);
                    badStrdata = "BADDATA;" + Encoding.ASCII.GetString(badData, 0, badData.Length);

                    //пересчитать новый размер данных
                    comStoreBuffer1DateSize = comStoreBuffer1DateSize - startIndex;
                    Array.Copy(comStoreBuffer1, startIndex, comStoreBuffer1, 0, comStoreBuffer1DateSize);
                    //стереть данные после переноса
                    Array.Clear(comStoreBuffer1, comStoreBuffer1DateSize, comStoreBuffer1.Length - comStoreBuffer1DateSize);
                    return badStrdata;
                }

                //выделить сообщение 
                int messSize = (endIndex - startIndex) - 1;
                byte[] mess;

                if (chars[0] == 0)
                {
                    mess = new byte[messSize + 1];
                    Array.Copy(comStoreBuffer1, startIndex, mess, 0, messSize + 1);
                }
                else
                {
                    mess = new byte[messSize];
                    Array.Copy(comStoreBuffer1, startIndex + 1, mess, 0, messSize);
                }

                string strdata = Encoding.ASCII.GetString(mess, 0, mess.Length);
                //string strdata = Encoding.Unicode.GetString(mess, 0, mess.Length);

                //проверить остатки если остались сохранить
                if ((endIndex + 1) < comStoreBuffer1DateSize)
                {
                    //пересчитать новый размер данных
                    //без последнего элемента так как он конец предыдущей посылки
                    comStoreBuffer1DateSize = comStoreBuffer1DateSize - (endIndex + 1);
                    Array.Copy(comStoreBuffer1, (endIndex + 1), comStoreBuffer1, 0, comStoreBuffer1DateSize);
                    //стереть данные после переноса
                    Array.Clear(comStoreBuffer1, comStoreBuffer1DateSize, comStoreBuffer1.Length - comStoreBuffer1DateSize);

                }
                else
                {
                    comStoreBuffer1DateSize = 0;
                    Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                }

                return strdata;
            }
            catch (Exception ex)
            {
                ex.ToString();
                //сбросить в лог ошибок данные 
                //выделить потерянные данные
                byte[] badData = new byte[startIndex];
                Array.Copy(comStoreBuffer1, 0, badData, 0, startIndex);
                badStrdata = "BADDATA;" + Encoding.ASCII.GetString(rcvData, 0, rcvData.Length);//+"RECIVEDATA";
                //стереть данные 
                Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                comStoreBuffer1DateSize = 0;
            }
            return badStrdata;

        }

        public static string GetMessAtStart(byte[] rcvData, int rcvSize, byte StartChar, byte StopChar)
        {
            int startIndex = 0;
            bool startFound = false;
            int endIndex = 0;
            bool endFound = false;
            string badStrdata = "";
            byte[] comStoreBuffer1 = new byte[102];
            int comStoreBuffer1DateSize = 0;
            //byte[] chars = Encoding.UTF8.GetBytes("+-");
            // byte[] chars = new byte[2] { 2, 3 };


            byte[] chars = new byte[2] { StartChar, StopChar };

            try
            {
                //если посылка пуста обработать уже имеющейся буфер
                if (rcvData != null)
                {
                    //сложить предыдущий буфер и вновь поступивший 
                    Array.Copy(rcvData, 0, comStoreBuffer1, comStoreBuffer1DateSize, rcvSize);
                    comStoreBuffer1DateSize += rcvSize;
                }
                else if (comStoreBuffer1DateSize == 0)
                    return "";


                //найти старт посылки
                foreach (byte b in comStoreBuffer1)
                {
                    //если chars[0] == 0 то считать любой символ началом пакета
                    if ((b == chars[0]) || (chars[0] == 0))
                    {
                        startFound = true;
                        break;
                    }
                    startIndex++;
                }
                //если старт не найден сбросить посылку как мусор
                if (!startFound)
                {
                    comStoreBuffer1DateSize = 0;
                    Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                    return "";
                }

                //найти стоп посылки
                foreach (byte b in comStoreBuffer1)
                {
                    if (b == chars[1])
                    {
                        endFound = true;
                        break;
                    }
                    endIndex++;
                }
                //если стоп не найден выйти
                if (!endFound)
                {
                    //если размер пакета  превышает допустимый сбросить посылку как мусор
                    if (comStoreBuffer1DateSize >= 80)
                    {
                        comStoreBuffer1DateSize = 0;
                        Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                        return "";
                    }

                    return "";
                }
                //убедится что сначал старт потом стоп :)
                //иначе сбросить все до старта и вернуть реинит!
                if (endIndex < startIndex)
                {
                    //выделить потерянные данные
                    byte[] badData = new byte[startIndex];
                    Array.Copy(comStoreBuffer1, 0, badData, 0, startIndex);
                    badStrdata = "BADDATA;" + Encoding.ASCII.GetString(badData, 0, badData.Length);

                    //пересчитать новый размер данных
                    comStoreBuffer1DateSize = comStoreBuffer1DateSize - startIndex;
                    Array.Copy(comStoreBuffer1, startIndex, comStoreBuffer1, 0, comStoreBuffer1DateSize);
                    //стереть данные после переноса
                    Array.Clear(comStoreBuffer1, comStoreBuffer1DateSize, comStoreBuffer1.Length - comStoreBuffer1DateSize);
                    return badStrdata;
                }

                //выделить сообщение 
                int messSize = (endIndex - startIndex) - 1;
                byte[] mess;

                if (chars[0] == 0)
                {
                    mess = new byte[messSize + 1];
                    Array.Copy(comStoreBuffer1, startIndex, mess, 0, messSize + 1);
                }
                else
                {
                    mess = new byte[messSize];
                    Array.Copy(comStoreBuffer1, startIndex + 1, mess, 0, messSize);
                }

                string strdata = Encoding.ASCII.GetString(mess, 0, mess.Length);
                //string strdata = Encoding.Unicode.GetString(mess, 0, mess.Length);

                //проверить остатки если остались сохранить
                if ((endIndex + 1) < comStoreBuffer1DateSize)
                {
                    //пересчитать новый размер данных
                    //без последнего элемента так как он конец предыдущей посылки
                    comStoreBuffer1DateSize = comStoreBuffer1DateSize - (endIndex + 1);
                    Array.Copy(comStoreBuffer1, (endIndex + 1), comStoreBuffer1, 0, comStoreBuffer1DateSize);
                    //стереть данные после переноса
                    Array.Clear(comStoreBuffer1, comStoreBuffer1DateSize, comStoreBuffer1.Length - comStoreBuffer1DateSize);

                }
                else
                {
                    comStoreBuffer1DateSize = 0;
                    Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                }

                return strdata;
            }
            catch (Exception ex)
            {
                ex.ToString();
                //сбросить в лог ошибок данные 
                //выделить потерянные данные
                byte[] badData = new byte[startIndex];
                Array.Copy(comStoreBuffer1, 0, badData, 0, startIndex);
                badStrdata = "BADDATA;" + Encoding.ASCII.GetString(rcvData, 0, rcvData.Length);//+"RECIVEDATA";
                //стереть данные 
                Array.Clear(comStoreBuffer1, 0, comStoreBuffer1.Length);
                comStoreBuffer1DateSize = 0;
            }
            return badStrdata;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool IsShowMessage(string message, char DelimetrChar)
        {
            string[] data = message.Split(DelimetrChar);
            //проверка корректности пакета
            if (data.Length != 4)
                return true;

            //
            try
            {
                decimal dec = 0;
                for (int i = 1; i < 4; i++)
                {
                    dec = Convert.ToDecimal(data[i]);
                    if (dec == 0)
                        return true;

                    if (dec > 9999)
                        return true;
                }
            }
            catch
            {
                return true;
            }
            return false;
        }
    }
}
