
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Util;


namespace FSerialization.PrintUtil
{
    public class LineData
    {
        public enum LineDataType
        {
            Unknow=0,
            ProdName,
            Serial,
            GTIN,
            Expdate,
            Quantity,
            Parsed
        }
        public class LineTemplate
        {
            public byte[] data;
            public LineDataType type;
            public int tabRow;
            public LineTemplate() { }
            public LineTemplate(byte[] _d, LineDataType _t)
            {
                data = _d;
                type = _t;
            }
        }

        public LineDataType tp;
        public byte[] data;

        public string DplFieldType;
        public string Rotation;
        public int RowPosition;
        public int ColumnPosition;
        public int RowNum;

        public int tmplPosStart = 0;
        public int tmplLen = 0;
        public LineData() { }
        public LineData(byte[] _data, List<LineTemplate> templates)
        {
            data = new byte[_data.Length];
            Array.Copy(_data, data, data.Length);
            //если размер строки меньше 20 нерасматривать ее хз что там нам не интересно
            if (_data.Length < 20)
                return;
            //попробовать распознать данные в поле 
            string str = System.Text.Encoding.Default.GetString(_data);
            Rotation = str.Substring(0, 1);
            //если DplFieldType имеет любое значение кроме 0-9 не рассматривать строку
            DplFieldType = str.Substring(1, 1);
            try {
                int t = Convert.ToInt32(DplFieldType);
                if (t < 0 || t > 9)
                    return;

                RowPosition = Convert.ToInt32(str.Substring(7, 4));
                ColumnPosition = Convert.ToInt32(str.Substring(11, 4));


            } catch { return; }
            /*
            0-9 Font
            A-T Bar code with human readable text.
            a-z Bar code without human readable text.
            Wxx Bar code/Font expansion
            X Line, box, polygon, circle
            Y Image
             */

            //поиск всех полей определенных в массиве шаблона
            foreach (LineTemplate tmpl in templates)
            {
              
                int i1 = BoyerMoore.PatternSearch(tmpl.data, _data);
                if (i1 > 0)
                {
                    tmplPosStart = i1;
                    tmplLen = tmpl.data.Length;
                    tp = tmpl.type;
                    return;
                }
            }

        }

        public void SetValue(string _value)
        {
            //выделить память для массива результата
            List<byte> result = new List<byte>(data);
            List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(_value));
            result.RemoveRange(tmplPosStart, tmplLen);
            result.InsertRange(tmplPosStart, replData);
            data = result.ToArray();

            tp = LineDataType.Parsed;
        }
    }

    public class PrinterBox
    {
        static public bool Print(string commandString, string ip, int port)
        {

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

                }
                finally
                {
                    if (streamWriterOut != null)
                        streamWriterOut.Close();
                }
                ////////////////////////////////////////

                // передать на печать по TCP\IP
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
                Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                s.Connect(ipe);

                if (!s.Connected)
                {
                    string WriteStr = String.Format("Невозможно установить связь с принтером:{0} порт:{1}", ip, port.ToString());

                }


                if (s == null)
                    return false;

                Byte[] bytesSent = Encoding.UTF8.GetBytes(commandString);
                int result = s.Send(bytesSent, bytesSent.Length, 0);

                //задержка чтоб принтер пришел в себя
                System.Threading.Thread.Sleep(800);

                s.Shutdown(SocketShutdown.Both);
                s.Close();
            }
            catch (ArgumentNullException)
            {

                return false;
            }
            catch (SocketException)
            {

                return false;
            }
            finally
            {

            }
            return true;
        }
        static public bool Print(byte[] data, string ip, int port)
        {
            //Stopwatch w = new Stopwatch();

            try
            {
                ////////////////////////////////////////
                System.IO.BinaryWriter streamWriterOut = null;
                //создать директорию отчётов если её нет
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\lastPrint.txt";
                //
                try
                {
                    //удалить предыдущий отчет если он какимто чудом есть
                    System.IO.File.Delete(path);
                    streamWriterOut = new System.IO.BinaryWriter(File.Open(path, FileMode.Create));
                    if (data != null)
                        streamWriterOut.Write(data);
                }
                catch (Exception ex)
                {
                    string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
                    //Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
                }
                finally
                {
                    if (streamWriterOut != null)
                        streamWriterOut.Close();
                }
                ////////////////////////////////////////
                ////////////////////////////////////////

                // передать на печать по TCP\IP
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
                Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                s.Connect(ipe);

                if (!s.Connected)
                {
                    string WriteStr = String.Format("Невозможно установить связь с принтером:{0} порт:{1}", ip, port.ToString());

                }


                if (s == null)
                    return false;

                //Byte[] bytesSent = Encoding.UTF8.GetBytes(commandString);
                int result = s.Send(data, data.Length, 0);

                //задержка чтоб принтер пришел в себя
                System.Threading.Thread.Sleep(800);

                s.Shutdown(SocketShutdown.Both);
                s.Close();
                return true;
            }
            catch (ArgumentNullException)
            {

                return false;
            }
            catch (SocketException)
            {

                return false;
            }
            finally
            {
                // MessageBox.Show("Время обмена с принтером" + w.Elapsed.ToString());

            }
        }

        static public byte[] CreateTemplateDataMax(string number, string numРacksInBox, string lotNo, string expDate, string _stacker, List<FSerialization.LabelField> boxLabelFields, int labelCount, string createDate, string _gtin)
        {

            //_stacker = Program.currentUser.Name;
            string sscc18 = "";
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Box.tmpl";

            // работа с шаблоном
            try
            {
                //string path = System.IO.Path.GetDirectoryName(
                //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;

                if (boxLabelFields == null)
                    throw new Exception("boxLabelFields = null ?! Массив полей пуст!");

                //создать паттерны 
                byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
                byte[] gtin = Encoding.UTF8.GetBytes("#GTIN#");
                byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
                byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
                //byte[] barcodeSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");
                byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
                byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера
                byte[] barcodeAlternativeSSCC18 = Encoding.UTF8.GetBytes("123456789012345678");//
                byte[] manufacturingDate = Encoding.UTF8.GetBytes("#MDATE#");

                byte[] labelBarcodeSSCC18v2 = Encoding.UTF8.GetBytes("(00)000000000000000000");
                byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");

                byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
                byte[] stacker = Encoding.UTF8.GetBytes("#ST#");
                byte[] labelPrintCount = Encoding.UTF8.GetBytes("Q0001");


                /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
                   byte[] productType = Encoding.UTF8.GetBytes("#productType#");
                   byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
                   byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
                   byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




                //массив результата
                List<byte> result = null;

                if (File.Exists(path))
                {
                    // загрузить шаблон для печати
                    byte[] source = File0.ReadAllBytes(path);
                    if (source == null)
                        throw new Exception("Ошибка - нет файла шаблона.");

                    //выделить память для массива результата
                    result = new List<byte>(source);
                    //поиск всех полей определенных в массиве шаблона
                    foreach (FSerialization.LabelField field in boxLabelFields)
                    {
                        byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
                        int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
                        if (i1 > 0)
                        {
                            List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
                            result.RemoveRange(i1, key.Length);
                            result.InsertRange(i1, data);
                        }
                    }
                    //

                    int i = BoyerMoore.PatternSearch(labelPrintCount, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes("Q" + labelCount.ToString("0000")));
                        result.RemoveRange(i, labelPrintCount.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(gtin, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(_gtin));
                        result.RemoveRange(i, gtin.Length);
                        result.InsertRange(i, replData);
                    }
                    //
                    i = BoyerMoore.PatternSearch(today, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
                        //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
                        result.RemoveRange(i, today.Length);
                        result.InsertRange(i, replData);
                    }
                    i = BoyerMoore.PatternSearch(quantity, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
                        result.RemoveRange(i, quantity.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(stacker, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
                            "--"));//Program.currentUser.Name));
                        result.RemoveRange(i, stacker.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(manufacturingDate, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(createDate));// DateTime.Now.ToString("dd.MM.yyyy")));
                        //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
                        result.RemoveRange(i, manufacturingDate.Length);
                        result.InsertRange(i, replData);
                    }

                    //
                    i = BoyerMoore.PatternSearch(partNum, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
                        result.RemoveRange(i, partNum.Length);
                        result.InsertRange(i, replData);
                    }
                    //
                    i = BoyerMoore.PatternSearch(expdate, result.ToArray());
                    if (i > 0)
                    {
                        //string day = expDate.Substring(4, 2);
                        //string mon = expDate.Substring(2, 2);
                        //string year = "20" + expDate.Substring(0, 2);
                        //string date =  mon + "." + year;

                        //string year = "20" + expDate.Substring(0, 2);
                        //string date = mon + expDate.Substring(0, 2); //"." + year;


                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(expDate));
                        result.RemoveRange(i, expdate.Length);
                        result.InsertRange(i, replData);
                    }
                    //поиск шаблона для печати кода ссцц18
                    i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
                    if (i > 0)
                    {
                        sscc18 = "(00)" + number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, labelBarcodeSSCC18.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(labelBarcodeSSCC18v2, result.ToArray());
                    if (i > 0)
                    {
                        sscc18 = "(00)" + number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, labelBarcodeSSCC18v2.Length);
                        result.InsertRange(i, replData);
                    }
                    //создать номер коробки в соответствии с выбранным типом кода
                    //сформировать префикс и суфикс для этикетки коробки
                    //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
                    i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
                    if (i > -1)
                    {
                        sscc18 = "00" + number;
                        //sscc18 = number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
                        result.InsertRange(i, replData);
                    }
                    else
                    {
                        i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
                        if (i > -1)
                        {
                            sscc18 = "00" + number;
                            //sscc18 = number;
                            List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                            result.RemoveRange(i, barcodeBartenderSSCC18.Length);
                            result.InsertRange(i, replData);
                        }
                    }

                    //альтернативный код ссцц18  barcodeAlternativeSSCC18
                    i = BoyerMoore.PatternSearch(barcodeAlternativeSSCC18, result.ToArray());
                    if (i > -1)
                    {
                        sscc18 = number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, barcodeAlternativeSSCC18.Length);
                        result.InsertRange(i, replData);
                    }

                    // послать на печать
                    return result.ToArray();
                }

            }
            catch (Exception ex)
            {
                Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);
                //RsMt.Base.MessageBoxEx.Show("Ощибка работы с шаблоном для печати.\nШаблон:" + path);
            }
            return null;
        }
        static public byte[] CreateTemplateDataMaxOld(string number, string numРacksInBox, string lotNo, string expDate, string _stacker, List<FSerialization.LabelField> boxLabelFields)
        {


            string sscc18 = "";
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\box.tmpl";

            // работа с шаблоном
            try
            {
                //string path = System.IO.Path.GetDirectoryName(
                //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;



                //создать паттерны 
                byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
                byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
                byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
                byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
                byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера
                byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");
                byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
                byte[] stacker = Encoding.UTF8.GetBytes("#ST#");

                /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
                   byte[] productType = Encoding.UTF8.GetBytes("#productType#");
                   byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
                   byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
                   byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




                //массив результата
                List<byte> result = null;

                if (File.Exists(path))
                {
                    // загрузить шаблон для печати
                    byte[] source = File0.ReadAllBytes(path);
                    if (source == null)
                        throw new Exception("Ошибка - нет файла шаблона.");

                    //выделить память для массива результата
                    result = new List<byte>(source);
                    //поиск всех полей определенных в массиве шаблона
                    foreach (FSerialization.LabelField field in boxLabelFields)
                    {
                        byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
                        int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
                        if (i1 > 0)
                        {
                            List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
                            result.RemoveRange(i1, key.Length);
                            result.InsertRange(i1, data);
                        }
                    }
                    #region Даты 
                    //Сегодня
                    int i = BoyerMoore.PatternSearch(today, result.ToArray());
                    if (i > 0)
                    {
                        //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
                        result.RemoveRange(i, today.Length);
                        result.InsertRange(i, replData);
                    }
                    //Срок годности
                    i = BoyerMoore.PatternSearch(expdate, result.ToArray());
                    if (i > 0)
                    {
                        string day = expDate.Substring(4, 2);
                        string mon = expDate.Substring(2, 2);
                        //string year = "20" + expDate.Substring(0, 2);
                        //string date =  mon + "." + year;

                        //string year = "20" + expDate.Substring(0, 2);
                        string date = mon + expDate.Substring(0, 2); //"." + year;


                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(date));
                        result.RemoveRange(i, expdate.Length);
                        result.InsertRange(i, replData);
                    }
                    #endregion

                    i = BoyerMoore.PatternSearch(quantity, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
                        result.RemoveRange(i, quantity.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(stacker, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
                            _stacker));
                        result.RemoveRange(i, stacker.Length);
                        result.InsertRange(i, replData);
                    }

                    //
                    i = BoyerMoore.PatternSearch(partNum, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
                        result.RemoveRange(i, partNum.Length);
                        result.InsertRange(i, replData);
                    }


                    i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
                    if (i > 0)
                    {
                        sscc18 = "(00)" + number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, labelBarcodeSSCC18.Length);
                        result.InsertRange(i, replData);
                    }

                    //создать номер коробки в соответствии с выбранным типом кода
                    //сформировать префикс и суфикс для этикетки коробки
                    //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
                    i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
                    if (i > -1)
                    {
                        sscc18 = "00" + number;
                        //sscc18 = number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
                        result.InsertRange(i, replData);
                    }
                    else
                    {
                        i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
                        if (i > -1)
                        {
                            sscc18 = "00" + number;
                            //sscc18 = number;
                            List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                            result.RemoveRange(i, barcodeBartenderSSCC18.Length);
                            result.InsertRange(i, replData);
                        }
                    }

                    // послать на печать
                    return result.ToArray();
                }

            }
            catch (Exception ex)
            {
                Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);
                //RsMt.Base.MessageBoxEx.Show("Ощибка работы с шаблоном для печати.\nШаблон:" + path);
            }
            return null;
        }
        #region CreateFor MultiProd 
        static public List<LineData> SplitArrayToLineData(byte[] indate)
        {
            //создать набор шаблонов
            List<LineData.LineTemplate> templates = new List<LineData.LineTemplate>();
            templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#NAMEx#"), LineData.LineDataType.ProdName));
            templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#PARTNUMx#"), LineData.LineDataType.Serial));
            templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#GTINx#"), LineData.LineDataType.GTIN));
            templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#EXPDATEx#"), LineData.LineDataType.Expdate));
            templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#Qx#"), LineData.LineDataType.Quantity));

            //0D0A
            int posStart = 0;
            List<LineData> result = new List<LineData>();
            for (int i = 1; i < indate.Length; i++)
            {
                //выделить часть
                if (indate[i] == 0x0a && indate[i - 1] == 0x0d)
                {
                    byte[] p = new byte[(i + 1) - posStart];
                    Array.Copy(indate, posStart, p, 0, p.Length);
                    LineData nl = new LineData(p, templates);
                    if (nl.tp != LineData.LineDataType.Unknow)
                    {
                        nl.RowNum = result.FindAll(l => l.tp == nl.tp).Count;
                    }
                    else
                        nl.RowNum = -1;

                    result.Add(nl);
                    posStart = i + 1;
                }
            }

            return result;
        }
        static public byte[] CreateTable(byte[] inputData, List<FSerialization.PaleteLadelData.ProductInfo> products)
        {
            byte[] wrData = null;

            //0D0A
            try
            {
                //разбить на строки
                List<LineData> linesL = SplitArrayToLineData(inputData);

                //найти координаты таблицы
                int rowIndex = 0;

                //заполнить таблицу
                foreach (FSerialization.PaleteLadelData.ProductInfo prd in products)
                {
                    List<LineData> line = linesL.FindAll(ln => ln.RowNum == rowIndex);
                    foreach (LineData ln in line)
                    {
                        switch (ln.tp)
                        {
                            case LineData.LineDataType.Expdate:
                                String dFormat = "yyMMdd";
                                DateTime _expDate = DateTime.ParseExact(prd.expDate, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                                ln.SetValue(_expDate.ToString("MM.yyyy",System.Globalization.CultureInfo.InvariantCulture));
                                break;
                            case LineData.LineDataType.GTIN:
                                ln.SetValue(prd.gtin);
                                break;
                            case LineData.LineDataType.ProdName:
                                ln.SetValue(prd.name);
                                break;
                            case LineData.LineDataType.Quantity:
                                ln.SetValue(prd.quantity.ToString());
                                break;
                            case LineData.LineDataType.Serial:
                                ln.SetValue(prd.lotNo);
                                break;
                        }

                    }
                    rowIndex++;
                }

                //сложить обратно в массив этикетки
                //посчитать общий объем и убрать все не задействованные строки в таблице
                linesL.RemoveAll(ln => (ln.tp != LineData.LineDataType.Parsed && ln.tp != LineData.LineDataType.Unknow));
                int a = 0;
                foreach (LineData line in linesL)
                    a += line.data.Length;


                wrData = new byte[a];
                int pos = 0;
                foreach (LineData line in linesL)
                {
                    Array.Copy(line.data, 0, wrData, pos, line.data.Length);
                    pos += line.data.Length;
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return wrData;

        }
        static public byte[] CreateTemplateDataMax(List<FSerialization.PaleteLadelData.ProductInfo> pld, string number,
            string numРacksInBox, string lotNo, string expDate, string _stacker, int labelCount, string createDate)
        {

            //_stacker = Program.currentUser.Name;
            string sscc18 = "";
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Box.tmpl";

            // работа с шаблоном
            try
            {
                //string path = System.IO.Path.GetDirectoryName(
                //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;

                //if (boxLabelFields == null)
                //    throw new Exception("boxLabelFields = null ?! Массив полей пуст!");

                //создать паттерны 
                byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
                byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
                byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
                byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
                byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера
                byte[] manufacturingDate = Encoding.UTF8.GetBytes("#MDATE#");
                byte[] barcodeAlternativeSSCC18 = Encoding.UTF8.GetBytes("123456789012345678");//


                byte[] labelBarcodeSSCC18v2 = Encoding.UTF8.GetBytes("(00)000000000000000000");
                byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");

                byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
                byte[] stacker = Encoding.UTF8.GetBytes("#ST#");
                byte[] labelPrintCount = Encoding.UTF8.GetBytes("Q0001");

                /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
                   byte[] productType = Encoding.UTF8.GetBytes("#productType#");
                   byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
                   byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
                   byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




                //массив результата
                List<byte> result = null;

                if (File.Exists(path))
                {
                    // загрузить шаблон для печати
                    byte[] source = File0.ReadAllBytes(path);
                    if (source == null)
                        throw new Exception("Ошибка - нет файла шаблона.");
                    //создать таблицу
                    source = CreateTable(source, pld);

                    //выделить память для массива результата
                    result = new List<byte>(source);
                    //поиск всех полей определенных в массиве шаблона
                    /* foreach (FSerialization.LabelField field in boxLabelFields)
                     {
                         byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
                         int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
                         if (i1 > 0)
                         {
                             List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
                             result.RemoveRange(i1, key.Length);
                             result.InsertRange(i1, data);
                         }
                     }*/
                    //

                    int i = BoyerMoore.PatternSearch(labelPrintCount, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes("Q" + labelCount.ToString("0000")));
                        result.RemoveRange(i, labelPrintCount.Length);
                        result.InsertRange(i, replData);
                    }
                    i = BoyerMoore.PatternSearch(manufacturingDate, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(createDate));// DateTime.Now.ToString("dd.MM.yyyy")));
                        //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
                        result.RemoveRange(i, manufacturingDate.Length);
                        result.InsertRange(i, replData);
                    }
                    //
                    i = BoyerMoore.PatternSearch(today, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
                        //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
                        result.RemoveRange(i, today.Length);
                        result.InsertRange(i, replData);
                    }
                    i = BoyerMoore.PatternSearch(quantity, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
                        result.RemoveRange(i, quantity.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(stacker, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
                            "--"));//Program.currentUser.Name));
                        result.RemoveRange(i, stacker.Length);
                        result.InsertRange(i, replData);
                    }

                    //
                    i = BoyerMoore.PatternSearch(partNum, result.ToArray());
                    if (i > 0)
                    {
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
                        result.RemoveRange(i, partNum.Length);
                        result.InsertRange(i, replData);
                    }
                    //
                    i = BoyerMoore.PatternSearch(expdate, result.ToArray());
                    if (i > 0)
                    {
                        //string day = expDate.Substring(4, 2);
                        //string mon = expDate.Substring(2, 2);
                        //string year = "20" + expDate.Substring(0, 2);
                        //string date =  mon + "." + year;

                        //string year = "20" + expDate.Substring(0, 2);
                        //string date = mon + expDate.Substring(0, 2); //"." + year;


                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(expDate));
                        result.RemoveRange(i, expdate.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
                    if (i > 0)
                    {
                        sscc18 = "(00)" + number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, labelBarcodeSSCC18.Length);
                        result.InsertRange(i, replData);
                    }

                    i = BoyerMoore.PatternSearch(labelBarcodeSSCC18v2, result.ToArray());
                    if (i > 0)
                    {
                        sscc18 = "(00)" + number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, labelBarcodeSSCC18v2.Length);
                        result.InsertRange(i, replData);
                    }

                    //создать номер коробки в соответствии с выбранным типом кода
                    //сформировать префикс и суфикс для этикетки коробки
                    //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
                    i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
                    if (i > -1)
                    {
                        sscc18 = "00" + number;
                        //sscc18 = number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
                        result.InsertRange(i, replData);
                    }
                    else
                    {
                        i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
                        if (i > -1)
                        {
                            sscc18 = "00" + number;
                            //sscc18 = number;
                            List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                            result.RemoveRange(i, barcodeBartenderSSCC18.Length);
                            result.InsertRange(i, replData);
                        }
                    }

                    //альтернативный код ссцц18  barcodeAlternativeSSCC18
                    i = BoyerMoore.PatternSearch(barcodeAlternativeSSCC18, result.ToArray());
                    if (i > -1)
                    {
                        sscc18 = number;
                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
                        result.RemoveRange(i, barcodeAlternativeSSCC18.Length);
                        result.InsertRange(i, replData);
                    }

                    // послать на печать
                    return result.ToArray();
                }




            }
            catch (Exception ex)
            {
                Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);
                //RsMt.Base.MessageBoxEx.Show("Ощибка работы с шаблоном для печати.\nШаблон:" + path);
            }
            return null;
        }
        #endregion
    }
    #region Old
    //public class PrinterBox
    //{
    //    static public byte[]  CreateTable(byte[] inputData,List<Shipping1СOrder.Product> products)
    //    {
    //        byte[] wrData = null;

    //        //1911SA2 0350 0025 P012P012#NameX#
    //        //1911SA2 0350 0220 P012P012#SerX#
    //        //1911SA2 0311 0025 P012P012#gtinX#
    //        //1911SA2 0311 0109 P012P012Упаковок
    //        //1911SA2 0311 0264 P012P012#ExpdateX#
    //        //1911SA2 0311 0187 P012P012#QX#

    //        //0D0A
    //        try
    //        {
    //            //разбить на строки
    //            List<LineData> linesL = SplitArrayToLineData(inputData);

    //            //найти координаты таблицы
    //            int rowIndex = 0;

    //            //заполнить таблицу
    //            foreach (Shipping1СOrder.Product prd in products)
    //            {
    //                List<LineData> line = linesL.FindAll(ln=>ln.RowNum == rowIndex);
    //                foreach (LineData ln in line)
    //                {
    //                    switch (ln.tp)
    //                    {
    //                        case LineData.LineDataType.Expdate:
    //                            ln.SetValue(DateTime.Now.ToString("dd.MM.yyyy"));
    //                            break;
    //                        case LineData.LineDataType.GTIN:
    //                            ln.SetValue(prd.gtin);
    //                            break;
    //                        case LineData.LineDataType.ProdName:
    //                            ln.SetValue(prd.productName);
    //                            break;
    //                        case LineData.LineDataType.Quantity:
    //                            ln.SetValue(prd.quantity.ToString());
    //                            break;
    //                        case LineData.LineDataType.Serial:
    //                            ln.SetValue(prd.lotNo);
    //                            break;
    //                    }

    //                }
    //                rowIndex++;
    //            }

    //            //сложить обратно в массив этикетки
    //            //посчитать общий объем и убрать все не задействованные строки в таблице
    //            linesL.RemoveAll(ln => (ln.tp != LineData.LineDataType.Parsed && ln.tp != LineData.LineDataType.Unknow));
    //            int a = 0;
    //            foreach (LineData line in linesL)
    //                a += line.data.Length;


    //            wrData = new byte[a];
    //            int pos = 0;
    //            foreach (LineData line in linesL)
    //            {
    //                Array.Copy(line.data, 0, wrData, pos,line.data.Length);
    //                pos += line.data.Length;
    //            }
    //        }
    //        catch(Exception ex)
    //        {
    //            ex.ToString();
    //        }
    //        return wrData;

    //    }
    //    static public List<LineData> SplitArrayToLineData(byte[] indate)
    //    {
    //        //создать набор шаблонов
    //        List<LineData.LineTemplate> templates = new List<LineData.LineTemplate>();
    //        templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#NAMEx#"), LineData.LineDataType.ProdName));
    //        templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#PARTNUMx#"), LineData.LineDataType.Serial));
    //        templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#GTINx#"), LineData.LineDataType.GTIN));
    //        templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#EXPDATEx#"), LineData.LineDataType.Expdate));
    //        templates.Add(new LineData.LineTemplate(Encoding.UTF8.GetBytes("#Qx#"), LineData.LineDataType.Quantity));

    //        //0D0A
    //        int posStart = 0;
    //        List<LineData> result = new List<LineData>();
    //        for (int i = 1; i < indate.Length; i++)
    //        {
    //            //выделить часть
    //            if (indate[i] == 0x0a && indate[i - 1] == 0x0d)
    //            {
    //                byte[] p = new byte[(i + 1) - posStart];
    //                Array.Copy(indate, posStart, p, 0, p.Length);
    //                LineData nl = new LineData(p, templates);
    //                if (nl.tp != LineData.LineDataType.Unknow)
    //                {
    //                    nl.RowNum = result.FindAll(l => l.tp == nl.tp).Count;
    //                }
    //                else
    //                    nl.RowNum = -1;

    //                result.Add(nl);
    //                posStart = i + 1;
    //            }
    //        }

    //        return result;
    //    }
    //    static public List<byte[]> SplitArray(byte[] indate)
    //    {
    //        //0D0A
    //        int posStart = 0;
    //        List<byte[]> result = new List<byte[]>();
    //        for (int i = 1; i < indate.Length; i++)
    //        {
    //            //выделить часть
    //            if (indate[i] == 0x0a && indate[i-1] == 0x0d)
    //            {
    //                byte[] p = new byte[(i+1)-posStart];
    //                Array.Copy(indate, posStart, p,0, p.Length);
    //                result.Add(p);
    //                posStart = i+1;

    //            }
    //        }

    //        return result;
    //    }
    //    static public bool Print(string commandString, string ip, int port)
    //    {

    //        try
    //        {
    //            ////////////////////////////////////////
    //            System.IO.TextWriter streamWriterOut = null;
    //            //создать директорию отчётов если её нет
    //            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\lastPrint.txt";
    //            //
    //            try
    //            {
    //                //удалить предыдущий отчет если он какимто чудом есть
    //                System.IO.File.Delete(path);
    //                streamWriterOut = new System.IO.StreamWriter(path, true, System.Text.Encoding.Default);

    //                streamWriterOut.WriteLine(commandString);
    //            }
    //            catch (Exception ex)
    //            {
    //                string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);

    //            }
    //            finally
    //            {
    //                if (streamWriterOut != null)
    //                    streamWriterOut.Close();
    //            }
    //            ////////////////////////////////////////

    //            // передать на печать по TCP\IP
    //            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
    //            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    //            s.Connect(ipe);

    //            if (!s.Connected)
    //            {
    //                string WriteStr = String.Format("Невозможно установить связь с принтером:{0} порт:{1}", ip, port.ToString());

    //            }


    //            if (s == null)
    //                return false;

    //            Byte[] bytesSent = Encoding.UTF8.GetBytes(commandString);
    //            int result = s.Send(bytesSent, bytesSent.Length, 0);

    //            s.Shutdown(SocketShutdown.Both);
    //            s.Close();
    //        }
    //        catch (ArgumentNullException )
    //        {

    //            return false;
    //        }
    //        catch (SocketException )
    //        {

    //            return false;
    //        }
    //        finally
    //        {

    //        }
    //        return true;
    //    }
    //    static public bool Print(byte[] data, string ip, int port)
    //    {
    //        //Stopwatch w = new Stopwatch();

    //        try
    //        {
    //            ////////////////////////////////////////
    //            System.IO.BinaryWriter streamWriterOut = null;
    //            //создать директорию отчётов если её нет
    //            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\lastPrint.txt";
    //            //
    //            try
    //            {
    //                //удалить предыдущий отчет если он какимто чудом есть
    //                System.IO.File.Delete(path);
    //                streamWriterOut = new System.IO.BinaryWriter(File.Open(path, FileMode.Create));
    //                if (data != null)
    //                    streamWriterOut.Write(data);
    //            }
    //            catch (Exception ex)
    //            {
    //                string WriteStr = String.Format("Ошибка работы с файлом {0}.Убедитесь в наличии доступа к этому файлу: {1} .", ex.Message, path);
    //                //Log.Write("Ошибка директории" + WriteStr,EventLogEntryType.Error, 1201);
    //            }
    //            finally
    //            {
    //                if (streamWriterOut != null)
    //                    streamWriterOut.Close();
    //            }
    //            ////////////////////////////////////////
    //            ////////////////////////////////////////

    //            // передать на печать по TCP\IP
    //            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
    //            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    //            s.Connect(ipe);

    //            if (!s.Connected)
    //            {
    //                string WriteStr = String.Format("Невозможно установить связь с принтером:{0} порт:{1}", ip, port.ToString());

    //            }


    //            if (s == null)
    //                return false;

    //            //Byte[] bytesSent = Encoding.UTF8.GetBytes(commandString);
    //            int result = s.Send(data, data.Length, 0);

    //            s.Shutdown(SocketShutdown.Both);
    //            s.Close();
    //            return true;
    //        }
    //        catch (ArgumentNullException )
    //        {

    //            return false;
    //        }
    //        catch (SocketException )
    //        {

    //            return false;
    //        }
    //        finally
    //        {
    //            // MessageBox.Show("Время обмена с принтером" + w.Elapsed.ToString());

    //        }
    //    }
    //    static public byte[] CreateTemplateDataMax(string number, string numРacksInBox, string lotNo, string expDate, string _stacker, List<FSerialization.LabelField> boxLabelFields, int labelCount)
    //    {

    //        //_stacker = Program.currentUser.Name;
    //        string sscc18 = "";
    //        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Box.tmpl";

    //        // работа с шаблоном
    //        try
    //        {
    //            //string path = System.IO.Path.GetDirectoryName(
    //            //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;

    //            if (boxLabelFields == null)
    //                throw new Exception("boxLabelFields = null ?! Массив полей пуст!");

    //            //создать паттерны 
    //            byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
    //            byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
    //            byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
    //            byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
    //            byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера

    //            byte[] labelBarcodeSSCC18v2 = Encoding.UTF8.GetBytes("(00)000000000000000000");
    //            byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");

    //            byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
    //            byte[] stacker = Encoding.UTF8.GetBytes("#ST#");
    //            byte[] labelPrintCount = Encoding.UTF8.GetBytes("Q0001");

    //            /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
    //               byte[] productType = Encoding.UTF8.GetBytes("#productType#");
    //               byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
    //               byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
    //               byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




    //            //массив результата
    //            List<byte> result = null;

    //            if (File.Exists(path))
    //            {
    //                // загрузить шаблон для печати
    //                byte[] source = File0.ReadAllBytes(path);
    //                if (source == null)
    //                    throw new Exception("Ошибка - нет файла шаблона.");

    //                //выделить память для массива результата
    //                result = new List<byte>(source);
    //                //поиск всех полей определенных в массиве шаблона
    //                foreach (FSerialization.LabelField field in boxLabelFields)
    //                {
    //                    byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
    //                    int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
    //                    if (i1 > 0)
    //                    {
    //                        List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
    //                        result.RemoveRange(i1, key.Length);
    //                        result.InsertRange(i1, data);
    //                    }
    //                }
    //                //

    //                int i = BoyerMoore.PatternSearch(labelPrintCount, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes("Q" + labelCount.ToString("0000")));
    //                    result.RemoveRange(i, labelPrintCount.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                //
    //                i = BoyerMoore.PatternSearch(today, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
    //                    //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
    //                    result.RemoveRange(i, today.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                i = BoyerMoore.PatternSearch(quantity, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
    //                    result.RemoveRange(i, quantity.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(stacker, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
    //                        "--"));//Program.currentUser.Name));
    //                    result.RemoveRange(i, stacker.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                //
    //                i = BoyerMoore.PatternSearch(partNum, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
    //                    result.RemoveRange(i, partNum.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //
    //                i = BoyerMoore.PatternSearch(expdate, result.ToArray());
    //                if (i > 0)
    //                {
    //                    //string day = expDate.Substring(4, 2);
    //                    //string mon = expDate.Substring(2, 2);
    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    //string date =  mon + "." + year;

    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    //string date = mon + expDate.Substring(0, 2); //"." + year;


    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(expDate));
    //                    result.RemoveRange(i, expdate.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
    //                if (i > 0)
    //                {
    //                    sscc18 = "(00)" + number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, labelBarcodeSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(labelBarcodeSSCC18v2, result.ToArray());
    //                if (i > 0)
    //                {
    //                    sscc18 = "(00)" + number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, labelBarcodeSSCC18v2.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //создать номер коробки в соответствии с выбранным типом кода
    //                //сформировать префикс и суфикс для этикетки коробки
    //                //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
    //                i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
    //                if (i > -1)
    //                {
    //                    sscc18 = "00" + number;
    //                    //sscc18 = number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                else
    //                {
    //                    i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
    //                    if (i > -1)
    //                    {
    //                        sscc18 = "00" + number;
    //                        //sscc18 = number;
    //                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                        result.RemoveRange(i, barcodeBartenderSSCC18.Length);
    //                        result.InsertRange(i, replData);
    //                    }
    //                }

    //                // послать на печать
    //                return result.ToArray();
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);
    //            //RsMt.Base.MessageBoxEx.Show("Ощибка работы с шаблоном для печати.\nШаблон:" + path);
    //        }
    //        return null;
    //    }
    //    static public byte[] CreateTemplateDataMax(List<FSerialization.PaleteLadelData.ProductInfo> pld, string number, string numРacksInBox, string lotNo, string expDate, string _stacker, List<FSerialization.LabelField> boxLabelFields, int labelCount)
    //    {

    //        //_stacker = Program.currentUser.Name;
    //        string sscc18 = "";
    //        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\Box.tmpl";

    //        // работа с шаблоном
    //        try
    //        {
    //            //string path = System.IO.Path.GetDirectoryName(
    //            //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;

    //            if (boxLabelFields == null)
    //                throw new Exception("boxLabelFields = null ?! Массив полей пуст!");

    //            //создать паттерны 
    //            byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
    //            byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
    //            byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
    //            byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
    //            byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера

    //            byte[] labelBarcodeSSCC18v2 = Encoding.UTF8.GetBytes("(00)000000000000000000");
    //            byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");

    //            byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
    //            byte[] stacker = Encoding.UTF8.GetBytes("#ST#");
    //            byte[] labelPrintCount = Encoding.UTF8.GetBytes("Q0001");

    //            /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
    //               byte[] productType = Encoding.UTF8.GetBytes("#productType#");
    //               byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
    //               byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
    //               byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




    //            //массив результата
    //            List<byte> result = null;

    //            if (File.Exists(path))
    //            {
    //                // загрузить шаблон для печати
    //                byte[] source = File0.ReadAllBytes(path);
    //                if (source == null)
    //                    throw new Exception("Ошибка - нет файла шаблона.");

    //                //выделить память для массива результата
    //                result = new List<byte>(source);
    //                //поиск всех полей определенных в массиве шаблона
    //                foreach (FSerialization.LabelField field in boxLabelFields)
    //                {
    //                    byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
    //                    int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
    //                    if (i1 > 0)
    //                    {
    //                        List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
    //                        result.RemoveRange(i1, key.Length);
    //                        result.InsertRange(i1, data);
    //                    }
    //                }
    //                //

    //                int i = BoyerMoore.PatternSearch(labelPrintCount, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes("Q" + labelCount.ToString("0000")));
    //                    result.RemoveRange(i, labelPrintCount.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                //
    //                i = BoyerMoore.PatternSearch(today, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
    //                    //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
    //                    result.RemoveRange(i, today.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                i = BoyerMoore.PatternSearch(quantity, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
    //                    result.RemoveRange(i, quantity.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(stacker, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
    //                        "--"));//Program.currentUser.Name));
    //                    result.RemoveRange(i, stacker.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                //
    //                i = BoyerMoore.PatternSearch(partNum, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
    //                    result.RemoveRange(i, partNum.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //
    //                i = BoyerMoore.PatternSearch(expdate, result.ToArray());
    //                if (i > 0)
    //                {
    //                    //string day = expDate.Substring(4, 2);
    //                    //string mon = expDate.Substring(2, 2);
    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    //string date =  mon + "." + year;

    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    //string date = mon + expDate.Substring(0, 2); //"." + year;


    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(expDate));
    //                    result.RemoveRange(i, expdate.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
    //                if (i > 0)
    //                {
    //                    sscc18 = "(00)" + number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, labelBarcodeSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(labelBarcodeSSCC18v2, result.ToArray());
    //                if (i > 0)
    //                {
    //                    sscc18 = "(00)" + number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, labelBarcodeSSCC18v2.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //создать номер коробки в соответствии с выбранным типом кода
    //                //сформировать префикс и суфикс для этикетки коробки
    //                //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
    //                i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
    //                if (i > -1)
    //                {
    //                    sscc18 = "00" + number;
    //                    //sscc18 = number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                else
    //                {
    //                    i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
    //                    if (i > -1)
    //                    {
    //                        sscc18 = "00" + number;
    //                        //sscc18 = number;
    //                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                        result.RemoveRange(i, barcodeBartenderSSCC18.Length);
    //                        result.InsertRange(i, replData);
    //                    }
    //                }

    //                // послать на печать
    //                return result.ToArray();
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);
    //            //RsMt.Base.MessageBoxEx.Show("Ощибка работы с шаблоном для печати.\nШаблон:" + path);
    //        }
    //        return null;
    //    }
    //    static public byte[] CreateTemplateDataMaxOld(string number, string numРacksInBox, string lotNo, string expDate, string _stacker, List<FSerialization.LabelField> boxLabelFields)
    //    {


    //        string sscc18 = "";
    //        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\box.tmpl";

    //        // работа с шаблоном
    //        try
    //        {
    //            //string path = System.IO.Path.GetDirectoryName(
    //            //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\Box.tmpl"; ;



    //            //создать паттерны 
    //            byte[] today = Encoding.UTF8.GetBytes("#TODAY#");
    //            byte[] partNum = Encoding.UTF8.GetBytes("#PARTNUM#");
    //            byte[] expdate = Encoding.UTF8.GetBytes("#EXPDATE#");
    //            byte[] barcodeNiceLabelSSCC18 = Encoding.UTF8.GetBytes("00000000000000000000");//20 нОлей ето баркод с nicelabel
    //            byte[] barcodeBartenderSSCC18 = Encoding.UTF8.GetBytes("0000000000000000000");//19 нОлей ето баркод с бартендера
    //            byte[] labelBarcodeSSCC18 = Encoding.UTF8.GetBytes("#SSCC18#");
    //            byte[] quantity = Encoding.UTF8.GetBytes("#Q#");
    //            byte[] stacker = Encoding.UTF8.GetBytes("#ST#");

    //            /*   byte[] productName = Encoding.UTF8.GetBytes("#productName#");
    //               byte[] productType = Encoding.UTF8.GetBytes("#productType#");
    //               byte[] pharmGroup = Encoding.UTF8.GetBytes("#pharmGroup#");
    //               byte[] appointment = Encoding.UTF8.GetBytes("#appointment#");
    //               byte[] productStorage = Encoding.UTF8.GetBytes("#productStorage#");*/




    //            //массив результата
    //            List<byte> result = null;

    //            if (File.Exists(path))
    //            {
    //                // загрузить шаблон для печати
    //                byte[] source = File0.ReadAllBytes(path);
    //                if (source == null)
    //                    throw new Exception("Ошибка - нет файла шаблона.");

    //                //выделить память для массива результата
    //                result = new List<byte>(source);
    //                //поиск всех полей определенных в массиве шаблона
    //                foreach (FSerialization.LabelField field in boxLabelFields)
    //                {
    //                    byte[] key = Encoding.UTF8.GetBytes(field.FieldName);
    //                    int i1 = BoyerMoore.PatternSearch(key, result.ToArray());
    //                    if (i1 > 0)
    //                    {
    //                        List<byte> data = new List<byte>(Encoding.UTF8.GetBytes(field.FieldData));
    //                        result.RemoveRange(i1, key.Length);
    //                        result.InsertRange(i1, data);
    //                    }
    //                }
    //                #region Даты 
    //                //Сегодня
    //                int i = BoyerMoore.PatternSearch(today, result.ToArray());
    //                if (i > 0)
    //                {
    //                    //List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy")));
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM.yyyy")));
    //                    result.RemoveRange(i, today.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //Срок годности
    //                i = BoyerMoore.PatternSearch(expdate, result.ToArray());
    //                if (i > 0)
    //                {
    //                    string day = expDate.Substring(4, 2);
    //                    string mon = expDate.Substring(2, 2);
    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    //string date =  mon + "." + year;

    //                    //string year = "20" + expDate.Substring(0, 2);
    //                    string date = mon + expDate.Substring(0, 2); //"." + year;


    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(date));
    //                    result.RemoveRange(i, expdate.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                #endregion

    //                i = BoyerMoore.PatternSearch(quantity, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(numРacksInBox.ToString()));
    //                    result.RemoveRange(i, quantity.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                i = BoyerMoore.PatternSearch(stacker, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(
    //                        _stacker));
    //                    result.RemoveRange(i, stacker.Length);
    //                    result.InsertRange(i, replData);
    //                }

    //                //
    //                i = BoyerMoore.PatternSearch(partNum, result.ToArray());
    //                if (i > 0)
    //                {
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(lotNo));
    //                    result.RemoveRange(i, partNum.Length);
    //                    result.InsertRange(i, replData);
    //                }


    //                i = BoyerMoore.PatternSearch(labelBarcodeSSCC18, result.ToArray());
    //                if (i > 0)
    //                {
    //                    sscc18 = "(00)" + number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, labelBarcodeSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                //создать номер коробки в соответствии с выбранным типом кода
    //                //сформировать префикс и суфикс для этикетки коробки
    //                //если выбран префик 1 значит должен использоватся стандарт GS1-SSCC-18
    //                i = BoyerMoore.PatternSearch(barcodeNiceLabelSSCC18, result.ToArray());
    //                if (i > -1)
    //                {
    //                    sscc18 = "00" + number;
    //                    //sscc18 = number;
    //                    List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                    result.RemoveRange(i, barcodeNiceLabelSSCC18.Length);
    //                    result.InsertRange(i, replData);
    //                }
    //                else
    //                {
    //                    i = BoyerMoore.PatternSearch(barcodeBartenderSSCC18, result.ToArray());
    //                    if (i > -1)
    //                    {
    //                        sscc18 = "00" + number;
    //                        //sscc18 = number;
    //                        List<byte> replData = new List<byte>(Encoding.UTF8.GetBytes(sscc18));
    //                        result.RemoveRange(i, barcodeBartenderSSCC18.Length);
    //                        result.InsertRange(i, replData);
    //                    }
    //                }

    //                // послать на печать
    //                return result.ToArray();
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Write("Ошибка шаблона: " + ex.Message);//, EventLogEntryType.Error, 1126);

    //        }
    //        return null;
    //    }
    //}
    #endregion
    class File0
    {
        public static byte[] ReadAllBytes(string fullFilePath)
        {
            // this method is limited to 2^32 byte files (4.2 GB)

            FileStream fs = System.IO.File.OpenRead(fullFilePath);
            try
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                return bytes;
            }
            finally
            {
                fs.Close();
            }

        }

    }
}
