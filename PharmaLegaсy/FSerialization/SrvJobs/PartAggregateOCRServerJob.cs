using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Util;
using System.Collections.ObjectModel;
using System.Threading;

namespace FSerialization
{
    [DataContract]
    public class PartAggregateOCRServerJob  : AggCorobBaseInfo, IBaseJob
    {
        private static ReaderWriterLockSlim readyBoxSync = new ReaderWriterLockSlim();

        private OrderMeta meta = new OrderMeta();
       // private ReaderWriterLockSlim _jobSync = new ReaderWriterLockSlim();

        [DataContract]
        public class DefectiveCodeSrv : DefectiveCode
        {
            public DefectiveCodeSrv(string idOp, string boxCode) : base(idOp, boxCode)
            {
                state = NumberState.Верифицирован;
            }
            [DataMember]
            public NumberState state { get; set; }
        }
        #region Реализация интерфейса BaseJob
        [DataMember]
        public OrderMeta JobMeta
        {
            get
            {
                string name = "";
                //обновить данные считано\осталось
                foreach (LabelField lf in boxLabelFields)
                {
                    if (lf.FieldName == "#productName#")
                        name = lf.FieldData;
                }

                meta.name = "Серия:" + lotNo + "\r" + name;
                meta.id = id;
                meta.type = 0;
                // meta.state = 0;
                return meta;
            }
            set { meta = value; }
        }

        [DataMember]
        public JobStates JobState { get; set; }
        public bool JobIsAwaible
        {
            get
            {
                if (JobState == JobStates.Complited)
                    return false;

                // if (JobState == JobState.CloseAndAwaitSend)
                //     return false;

                // if (JobState == JobState.WaitSend)
                //     return false;

                return true;
            }
        }

        public object GetTsdJob()
        {
            bool allVerify = true;
            //найти свободный номер короба
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Доступен)
                {
                    //номер найден сформировать задание
                    LineAggregateJob lag = CreateNewLineAggregateJob(b.boxNumber);
                    b.state = NumberState.Собирается;
                    //отметить задание как поступившее в работу
                    JobState = JobStates.InWork;
                    SafeToDisk();
                    return lag;
                }

                //если короб числится отправленным игнорировать
                if (b.state == NumberState.VerifyAndPlaceToReport)
                    continue;

                //если короб не находится в состоянии Verify проверен сблросить флаг allverify
                if (b.state != NumberState.Верифицирован)
                    allVerify = false;
            }
            //если все  номера обработаны выставить признак что пора отправлять
            if (allVerify)
            {
                //уведомить о закрытии задания
                if (JobState != JobStates.CloseAndAwaitSend)
                    JobState = JobStates.WaitSend;

                LineAggregateJob lag = CreateNewLineAggregateJob("000000000000000000");
                lag.printBoxLabel = false;
                lag.JobState = JobStates.CloseAndAwaitSend;
                return lag;
            }


            return null;
        }
        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.SendInProgress)
                    return false;

                if (JobState == JobStates.WaitSend)
                    return true;

                if (JobState == JobStates.CloseAndAwaitSend)
                    return true;

                if (OrderaArray.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }

        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat)
        {
            return SendReports(url, user, pass, partOfList, false, reguestTimeOut);

        }
         
        public string SendReports(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                BiotikiReport r = CreateReportB();
                JobState = JobStates.SendInProgress;

                //проверить не пустой ли отчет?
                //r.partOfList = partOfList;

                //не проверять на пустой отчет
                if (sendEmpty)
                    OrderaArray.Add(r);
                else
                {
                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
                        OrderaArray.Add(r);
                }

                //выполнить отправку всех отчетов из массива
                while (OrderaArray.Count > 0)
                {
                    BiotikiReport sr = OrderaArray.First();
                    string metod = partOfList ? "POST" : "PUT";
                    result = WebUtil.SendReport<BiotikiReport>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"),id, reguestTimeOut);
                    if (result != "")
                        return result;

                    OrderaArray.Remove(sr);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
            }
            finally
            {
                if (result != "")
                    JobMeta.state = JobIcon.ErrorSended;

                //выставить состояние ожидание отправки но без закрытия задания целиком
                JobState = JobStates.WaitSend;
            }
            return result;
        }

        public async Task<string> SendReportsAsync(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut, CancellationToken token)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                BiotikiReport r = CreateReportB();
                JobState = JobStates.SendInProgress;

                //проверить не пустой ли отчет?
                //r.partOfList = partOfList;

                //не проверять на пустой отчет
                if (sendEmpty)
                    OrderaArray.Add(r);
                else
                {
                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
                        OrderaArray.Add(r);
                }

                //выполнить отправку всех отчетов из массива
                while (OrderaArray.Count > 0)
                {
                    BiotikiReport sr = OrderaArray.First();
                    string metod = partOfList ? "POST" : "PUT";
                    result = await WebUtil.SendReportAsync<BiotikiReport>(url, user, pass, metod, sr, 
                        "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"), id, reguestTimeOut, token);
                    if (result != "")
                        return result;

                    OrderaArray.Remove(sr);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
            }
            finally
            {
                if (result != "")
                    JobMeta.state = JobIcon.ErrorSended;

                //выставить состояние ожидание отправки но без закрытия задания целиком
                JobState = JobStates.WaitSend;
            }
            return result;
        }

        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Агрегация OSR"; }
        #endregion
        //public override Type GetJobType() { return typeof(LineAggregateJob); }

        //используется в агрегации камерой как признак что задание в работе!
        //[DataMember]
        //public bool jobComplited = true;

        public event OrderAcceptedEventHandler OrderAcceptedEvent; // событие загрузки отчета.

        //[DataMember] 
        public NewPartAggregate1СOrder order1C;

        [DataMember]
        public List<DefectiveCodeSrv> brackBox = new List<DefectiveCodeSrv>();

        [DataMember]
        public List<Sampled> sampleCodes = new List<Sampled>();

        [DataMember]
        public List<Operator> mastersArray = new List<Operator>();

        [DataMember]
        public DateTime startTime;
        [DataMember]
        public List<PartAggSrvBoxNumber> readyBoxes = new List<PartAggSrvBoxNumber>(); //массив обработанных номеров

        //[DataMember]
        public BoxWithLayersOld cBox = null;// new BoxWithLayers("",1,1); //текущий обрабатываемый короб
        //[DataMember]
        public Queue<BoxWithLayersOld> boxQueue = new Queue<BoxWithLayersOld>();

        //public BoxWithLayers lBox = null;// new BoxWithLayers("",1,1); //текущий обрабатываемый короб на левом сканере
        //public BoxWithLayers rBox = null;// new BoxWithLayers("",1,1); //текущий обрабатываемый короб на правом сканере

        public bool CurentBoxLeft;
        [DataMember]
        public string AppendBoxNum;
        [DataMember]
        public string DeletedPackNum;

        [DataMember]
        public List<BiotikiReport> OrderaArray = new List<BiotikiReport>(); //массив отчетов для отправки

        public PartAggregateOCRServerJob() : base()
        {
            JobState = JobStates.Empty;
            meta.state = JobIcon.Default;
            jobType = typeof(PartAggregateOCRServerJob);
        }

        public string AcceptOrderToWork(NewPartAggregate1СOrder o)
        {
            string ErrorReason = "";
            if (JobState != JobStates.Empty)
                return "Сервис не может принять задание. Так как другое задание находится в работе";
            //if (jobArray == null)
            //    jobArray = new List<PartAggregateServerJob>();

            //jobArray.Clear();

            //создать задачу свервера
            order1C = o;
            // Sotex.Serialization.PartAggregateServerJob pl = new Sotex.Serialization.PartAggregateServerJob();
            id = o.id;
            lotNo = o.lotNo;
            GTIN = o.gtin;
            ExpDate = o.ExpDate;
            Date = o.Date;
            //addProdInfo = o.addProdInfo;

            numLabelAtBox = o.numLabelAtBox;
            numLayersInBox = o.numLayersInBox;
            numРacksInBox = o.numРacksInBox;
            prefixBoxCode = o.prefixBoxCode;
            urlLabelBoxTemplate = "здесь линк на шаблон на сервере этом . а не 1с";

            startTime = DateTime.Now;

            boxLabelFields.AddRange(o.boxLabelFields);


            foreach (string s in o.boxNumbers)
                readyBoxes.Add(new FSerialization.PartAggSrvBoxNumber(s));

            JobState = JobStates.New;

            //сохранить задание 
            Save1сOrder();

            if (OrderAcceptedEvent != null)
                OrderAcceptedEvent.Invoke(this);

            return ErrorReason;
        }
       
        public bool Save1сOrder()
        {
            try
            {
                string fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);

                string copy1FileName = fileName + "1сOrder";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);


                using (System.IO.TextWriter tmpFile = new System.IO.StreamWriter(copy1FileName, false))
                {
                    string s = Archive.SerializeJSon<NewPartAggregate1СOrder>(order1C);
                    tmpFile.Write(s);
                    tmpFile.Close();
                    tmpFile.Dispose();
                }

                return true;

            }
            catch (Exception ex)
            {
                ex.ToString();
                //Log.Write("Ошибка сохранения резервной копии!: " + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 701);
            }
            finally
            {
                //  orderSaveSync.ExitWriteLock();
            }
            return false;
        }

        public bool SaveOrder()
        {
            try
            {
                string fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);

                string copy1FileName = fileName + "tmpOrder1";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

                string copyFileName = fileName + "tmpOrder";
                //если существует резервная копия переименовать ее
                if (System.IO.File.Exists(copyFileName))
                    System.IO.File.Move(copyFileName, copy1FileName);

                fileName += "tmpOrder";
                using (System.IO.TextWriter tmpFile = new System.IO.StreamWriter(fileName, false))
                {
                    string s = Archive.SerializeJSon<PartAggregateOCRServerJob>(this);
                    tmpFile.Write(s);
                    tmpFile.Close();
                    tmpFile.Dispose();
                }

                return true;

            }
            catch (Exception ex)
            {
                ex.ToString();
                //Log.Write("Ошибка сохранения резервной копии!: " + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 701);
            }
            finally
            {
                //  orderSaveSync.ExitWriteLock();
            }
            //  }
            //   else
            //   {
            //      Log.Write("SaveOrder Критическая ошибка очереди",EventLogEntryType.Error, MAIN_ERROR_CODE + 702);
            //  }
            return false;
        }
        public bool DeleteOrder()
        {
            try
            {
                string fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);

                string copy1FileName = fileName + "tmpOrder";
                //если существует  удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

                copy1FileName = fileName + "tmpOrder1";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

                copy1FileName = fileName + "1сOrder";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

                copy1FileName = fileName + "Box.tmpl";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

               

                return true;

            }
            catch (Exception ex)
            {
                ex.ToString();
                //Log.Write("Ошибка сохранения резервной копии!: " + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 701);
            }
            finally
            {
                //  orderSaveSync.ExitWriteLock();
            }

            return false;
        }
        public static PartAggregateOCRServerJob RestoreOrder()
        {
            try
            {
                PartAggregateOCRServerJob result = null;

                string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\tmpOrder";

                if (System.IO.File.Exists(fileName))
                {
                    using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
                    {
                        string s = tmpFile.ReadToEnd();
                        tmpFile.Close();
                        tmpFile.Dispose();
                        result = Archive.DeserializeJSon<PartAggregateOCRServerJob>(s);
                        if (result == null)
                            throw new Exception("Не возможно загрузить имеющееся задание! Возможно файл tmpOrder поврежден!");

                    }
                }
                //загрузить данные по заданию
                fileName = System.IO.Path.GetDirectoryName(
                  System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\1сOrder";

                if (System.IO.File.Exists(fileName))
                {
                    using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
                    {
                        string s = tmpFile.ReadToEnd();
                        tmpFile.Close();
                        tmpFile.Dispose();
                        result.order1C = Archive.DeserializeJSon<NewPartAggregate1СOrder>(s);
                        if (result.order1C == null)
                            throw new Exception("Не возможно загрузить имеющееся задание! Возможно файл 1сOrder поврежден!");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return new PartAggregateOCRServerJob();
        }

        #region Работа с коробами
        //очистить боч и промаркировать его номер как плохой
        public bool WasteBox(string BoxNum)
        {
            if (readyBoxSync.TryEnterWriteLock(200))
            {
                try
                {
                    //поиск в очереди
                    /*
                    //проверить в коробах очереди
                    foreach (BoxWithLayers l in boxQueue)
                    {
                        if (l.Number == BoxNum)
                        {
                             boxQueue = new Queue<BoxWithLayers>(boxQueue.Where(s => s.Number != BoxNum));
                            break;
                        }
                    }*/
                    //удалить из очереди
                    boxQueue = new Queue<BoxWithLayersOld>(boxQueue.Where(s => s.Number != BoxNum));


                    //пометить в очереди как брак
                    foreach (PartAggSrvBoxNumber b in readyBoxes)
                    {
                        if (b.boxNumber == BoxNum)
                        {
                            b.state = NumberState.Забракован;
                            b.productNumbers.Clear();
                            return true;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message, EventLogEntryType.Error, 1123);
                    return false;
                }
                finally
                {
                    readyBoxSync.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("Критическая ошибка очереди WasteBox", EventLogEntryType.Error,  31);
            }
            return false;
        }
        #endregion

        public bool RemoveFromArray(BiotikiReport r)
        {
            return OrderaArray.Remove(r);
        }
        
        public BiotikiReport CreateReportB()
        {
            BiotikiReport r = new BiotikiReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:sszz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");

            //добавить обработанные коды
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Верифицирован)
                {
                    //проверить что короб не пуст. если пуст не добавлять!!! и сгенерить в лог ошибку!!
                    if (b.productNumbers?.Count > 0)
                    {
                        r.readyBox.Add(new ReadyBox(b));
                    }
                    else
                        Log.Write($"r.err id:{id} Пустой короб в отчете!! b:{b.boxNumber}");

                    b.state = NumberState.VerifyAndPlaceToReport;             
                }
            }

            //добавить забракованные коды
            foreach (DefectiveCodeSrv b in brackBox)
            {
                if (b.state == NumberState.Верифицирован)
                {
                    r.defectiveCodes.Add(new DefectiveCode(b.id, b.number));
                    b.state = NumberState.VerifyAndPlaceToReport;
                }
            }

            //закрыть сессию последнего мастера
            if (mastersArray?.Count > 0)
            {
                mastersArray[mastersArray.Count - 1].endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
            }
            else
            {
                //сгенерировать ошибку по хорошему
            }

            r.operators.AddRange(mastersArray);

            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
        }
        private PartAggregateOSRReport CreateReportA()
        {
            PartAggregateOSRReport r = new PartAggregateOSRReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");

            //добавить обработанные коды
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Верифицирован)
                {
                    r.readyBox.Add(new ReadyBox(b));
                    b.state = NumberState.VerifyAndPlaceToReport;
                }
            }

            //добавить забракованные коды
            foreach (DefectiveCodeSrv b in brackBox)
            {
                if (b.state == NumberState.Верифицирован)
                {
                    r.defectiveCodes.Add(new DefectiveCode(b.id, b.number));
                    b.state = NumberState.VerifyAndPlaceToReport;
                }
            }

            //добавить семплы / на данный момент заточено
            //под 1 отправку не под части!
            r.sampledCodes.AddRange(sampleCodes);
           
            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
        }
        public bool AddVerifyBox(PartAggSrvBoxNumber b)
        {
            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                if (b.boxNumber == br.boxNumber)
                {
                    br.state = NumberState.Верифицирован;
                    br.id = b.id;
                    br.boxTime = b.boxTime;
                    br.productNumbers.AddRange(b.productNumbers);
                    return true;
                }
            }
            return false;
        }
        public bool AddVerifyBoxWithFullNum(BoxWithLayersOld b, string userId)
        {
            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                if (b.Number == br.boxNumber)
                {
                    br.state = NumberState.Верифицирован;
                    br.id = userId;
                    br.boxTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                    foreach (LayerItem li in b.Numbers)
                        br.productNumbers.Add(li.fn);
                    return true;
                }
            }
            return false;
        }
        public bool AddVerifyBox(BoxWithLayersOld b,string userId)
        {
            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                if (b.Number == br.boxNumber)
                {
                    br.state = NumberState.Верифицирован;
                    br.id = userId;
                    br.boxTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                    foreach (LayerItem li in b.Numbers)
                        br.productNumbers.Add(li.number);
                    return true;
                }
            }
            return false;
        }
        public int  GetVerifyBoxCount()
        {
            if (readyBoxes == null)
                return 0;

            return readyBoxes.Count(x => x.state == NumberState.Верифицирован); 
        }

        public bool AddNuberToBox(string _serialNumber, string BoxNum,string fullNumber)
        {
            //поиск в текушем 
            if (BoxNum == cBox.Number)
            {
                cBox.Numbers.Add(new LayerItem(_serialNumber,0, fullNumber));
                return true;
            }

            //поиск в очереди
            //проверить в коробах очереди
            foreach (BoxWithLayersOld l in boxQueue)
            {
                if (l.Number == BoxNum)
                {
                    l.Numbers.Add(new LayerItem(_serialNumber, 0, fullNumber));
                    return true;
                }
            }


            //поиск в уже обработанных коробах
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == BoxNum)
                {
                    b.productNumbers.Add(_serialNumber);
                    return true;
                }
            }

            return false;
        }
        public bool ReplaceNumInBox(string BoxNum,string _oldNum,string _newNum,string fullNumber )
        {
            //поиск в текушем 
            if (BoxNum == cBox.Number)
            {
                if(cBox.Numbers.RemoveAll(item => item.number == _oldNum) < 1)
                    return false;

                cBox.Numbers.Add(new LayerItem(_newNum, 0, fullNumber));
                return true;
            }

            //поиск в очереди
            //проверить в коробах очереди
            foreach (BoxWithLayersOld l in boxQueue)
            {
                if (l.Number == BoxNum)
                {
                    if (l.Numbers.RemoveAll(item => item.number == _oldNum) < 1)
                        return false;

                    l.Numbers.Add(new LayerItem(_newNum, 0, fullNumber));
                    return true;
                }
            }
            return false;
        }
        
        //маркирует верифицированные данные как отосланные
        public bool MarkAllReadyDataSended()
        {
            // удалить из задания отосланные данные
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Верифицирован)
                    b.state = NumberState.VerifyAndPlaceToReport;
            }

            //очистить массив отбракованных
            brackBox.Clear();

            return true;
        }
        public void AddDefectCode(string code, string idOperator)
        {
            DefectiveCodeSrv dc = new DefectiveCodeSrv(idOperator, code);
            brackBox.Add(dc);
        }
        public void AddSampleCode(string code, string idOperator)
        {
            Sampled dc = new Sampled(code, idOperator);
            sampleCodes.Add(dc);
        }
        public bool IsAlreadyInBrack(string code)
        {
            foreach (DefectiveCodeSrv d in brackBox)
            {
                if (d.number == code)
                    return true;
            }
            return false;
        }
        public bool IsAlreadyInCurrentBoxes(string number, out string boxNum)
        {
            boxNum = "";

            if (number == null)
                return false;

            if (number == "")
                return false;

            //проверить в текущем боксе
            foreach (LayerItem l in cBox.Numbers)
            {
                if (l.number == number)
                {
                    boxNum = cBox.Number;
                    return true;
                }
            }

            //проверить в коробах очереди
            foreach (BoxWithLayersOld l in boxQueue)
            {
                if (l.IsAlreadyInBox(number))
                {
                    boxNum = l.Number;
                    return true;
                }
            }
            return false;
        }

        public bool IsAlreadyInProcessedBox(string number, out string boxNum)
        {
            boxNum = "";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                //проверить в обработанных коробах
                foreach (PartAggSrvBoxNumber b in readyBoxes)
                {
                    if (b.state == NumberState.Верифицирован)
                    {
                        if (b.IsAlreadyInBox(number))
                        {
                            boxNum = b.boxNumber;
                           // Log.Write("foreach IsAlreadyInProcessedBox: " + sw.Elapsed);
                            return true;
                        }
                        //r.readyBox.Add(new ReadyBox(b));
                        //b.state = NuberState.VerifyAndPlaceToReport;
                    }
                }

                ////
                //sw.Restart();
                // Display all Blogs from the database
               // var query1 = from b in readyBoxes
               //              where b.boxNumber == fdNum
               //              orderby b.number
               //              select b;
               // Console.WriteLine("dbset+linq num step3.1   " + sw.Elapsed);


            }
            finally
            {
               // Log.Write("IsAlreadyInProcessedBox: " + sw.Elapsed);
                sw.Stop();
            }
            return false;
        }

        public bool IsProcessedBox(string boxNum)
        {
            //boxNum = "";
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            try
            {
                return readyBoxes.Exists(x => (x.state == NumberState.Верифицирован && x.boxNumber == boxNum));

                ////проверить в обработанных коробах
                //foreach (PartAggSrvBoxNumber b in readyBoxes)
                //{
                //    if (b.state == NumberState.Верифицирован)
                //    {
                //        if (b.IsAlreadyInBox(number))
                //        {
                //            boxNum = b.boxNumber;
                //            // Log.Write("foreach IsAlreadyInProcessedBox: " + sw.Elapsed);
                //            return true;
                //        }
                //        //r.readyBox.Add(new ReadyBox(b));
                //        //b.state = NuberState.VerifyAndPlaceToReport;
                //    }
                //}

             
            }
            finally
            {
                // Log.Write("IsAlreadyInProcessedBox: " + sw.Elapsed);
                //sw.Stop();
            }
            //return false;
        }

        public PartAggSrvBoxNumber GetNextBox()
        {
            //найти свободный номер короба
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Доступен)
                {
                    b.state = NumberState.Собирается;
                    return b.Clone();
                }
            }
            return null;
        }

        public BoxWithLayersOld GetNextBoxWithLayers(out int ost)
        {
            ost = -1;
            //найти свободный номер короба
            for (int i=0;i< readyBoxes.Count;i++)
            {
                PartAggSrvBoxNumber b = readyBoxes[i];

                if (b.state == NumberState.Доступен)
                {
                    b.state = NumberState.Собирается;
                    ost = readyBoxes.Count - i;
                    return new BoxWithLayersOld(b.boxNumber, order1C.numLayersInBox, order1C.numРacksInBox);
                }
                else if (b.state == NumberState.Собирается)
                {
                    //если мы нашли короб который собирается. проверить есть ли он в очереди на сборку
                    //возможно он остался в этом состоянии после перезагрузки и его можно использовать заново
                    if (boxQueue?.Count < 1)
                    {
                        ost = readyBoxes.Count - i;
                        return new BoxWithLayersOld(b.boxNumber, order1C.numLayersInBox, order1C.numРacksInBox);
                    }
                }
            }

            //если номеров коробов нет проверить номера в состоянии собирается.
            //возможно после перезапуска остался какойто номер собираемый и теперь он свободен
            
            return null;
        }

        public bool IsJObComplit()
        {
            if (JobState == JobStates.Complited)
                return true;

            //задание не найдено проверить возможно все закрыто и надо отправлять отчет
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if ((b.state == NumberState.Доступен) ||
                    (b.state == NumberState.Собирается))
                    return false;
            }
            //все коробки получили статус verify отправляем отчет
            return true;
        }
        /*
        public bool JobIsComplited()
        {
            if (JobState == JobState.Complited)
                return true;

            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.New)
                    return false;
            }
            return true;
        }*/
        private LineAggregateJob CreateNewLineAggregateJob(string boxNum)
        {
            LineAggregateJob pl = new LineAggregateJob();
            pl.id = id;
            pl.lotNo = lotNo;
            pl.order1C.gtin = GTIN;
            pl.order1C.ExpDate = ExpDate;
            //pl.addProdInfo = addProdInfo;

            pl.order1C.numLabelAtBox = numLabelAtBox;
            pl.order1C.numLayersInBox = numLayersInBox;
            pl.order1C.numРacksInBox = numРacksInBox;
            pl.order1C.prefixBoxCode = prefixBoxCode;
            pl.order1C.urlLabelBoxTemplate = urlLabelBoxTemplate;
            pl.printBoxLabel = true;
            //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
            pl.order1C.boxLabelFields.AddRange(boxLabelFields);


            pl.selectedBox = new PartAggSrvBoxNumber(boxNum);
            return pl;
        }

        public LineAggregateJob GetJobHelp()
        {
            return CreateNewLineAggregateJob("");
        }
        public bool BoxComplited(PartAggSrvBoxNumber jb)
        {
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == jb.boxNumber)
                {
                    //сбросить короб
                    b.productNumbers.Clear();
                    //добавить даннные
                    b.state = NumberState.Верифицирован;
                    b.productNumbers.AddRange(jb.productNumbers);
                    b.id = jb.id;
                    b.boxTime = jb.boxTime;
                }
            }

            //проверить завершение задание
            bool verifyAll = true;
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state != NumberState.Верифицирован)
                    verifyAll = false;
            }

            if (verifyAll)
                JobState = JobStates.WaitSend;

            return true;
        }
        public bool VerifyProductNum(string code)
        {
            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(code);
            if (VerifyProductNum(ld) == "")
                return true;

            return false;
        }
        public string VerifyProductNum(Util.GsLabelData ld)
        {
            if (ld == null)
                return "Ошибка распознавания номера";

            if(ld.SerialNumber == null)
                return "Ошибка распознавания серийного номера";

            if (string.IsNullOrEmpty(ld.field93Sign))
            {
                if (ld.field91Key == null)
                    return "Ошибка распознавания номера сертификата";

                if (ld.CryptoHash == null)
                    return "Ошибка распознавания крипто подписи";
            }

            if (order1C == null)
                return "Невозможно верифицировать номер "+ ld.SerialNumber + ". Нет задания.";

            //проверить GTIN 
            if (ld.GTIN != order1C.gtin)
                return "Продукт другого GTIN!";//""Пачка другого препарата!";

            //проверить доп поля если они есть
            //if (ld.PruductIdentificationOfProducer != null)
            //{
            //    if (order1C?.addProdInfo.Length > 0)
            //    {
            //        if (ld?.PruductIdentificationOfProducer != order1C?.addProdInfo)
            //            return "ТНВЭД не совпадает!";
            //    }
            //}

            

            //проверить доп поля если они есть
            if (ld.Charge_Number_Lot != null)
            {
                if (order1C.lotNo != "")
                {
                    if (ld.Charge_Number_Lot != order1C.lotNo)
                        return "Пачка другой серии!";
                }
            }

            if (ld.ExpiryDate_JJMMDD != null)
            {
                if (order1C.ExpDate != "")
                {
                    if (ld.ExpiryDate_JJMMDD != order1C.ExpDate)
                        return "Не совпадает срок годности!";
                }
            }


            return "";
        }

        /// <summary>
        /// проеряет номер продукта на повтор
        /// </summary>
        /// <param name="prNum"></param>
        /// <returns></returns>
        public bool ProductNumAlreadyVerify(string prNum)
        {
            if (prNum == null)
                return false;

            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.IsAlreadyInBox(prNum))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// проеряет номер продукта на повтор
        /// и возвращает номер короба в котором числится пачка
        /// </summary>
        /// <param name="prNum"></param>
        /// <returns></returns>
        public bool ProductNumAlreadyVerify(string prNum, out string boxNum,bool remove = false)
        {
            boxNum = "";
            if (prNum == null)
                return false;

            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.IsAlreadyInBox(prNum))
                {
                    boxNum = b.boxNumber;
                    if(remove)
                        b.productNumbers.Remove(prNum);

                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// проеряет номер отбракованного продукта на повтор
        /// </summary>
        /// <param name="prNum"></param>
        /// <returns></returns>
        public bool ProductNumAlreadyDefected(string prNum)
        {
            if (prNum == null)
                return false;

            foreach (DefectiveCode b in brackBox)
            {
                if (b.number == prNum)
                    return true;
            }
            return false;
        }
        public bool ProductNumAlreadySampled(string prNum)
        {
            if (prNum == null)
                return false;

            foreach (Sampled b in sampleCodes)
            {
                if (b.number == prNum)
                    return true;
            }
            return false;
        }
        
        public bool BoxCancel(PartAggSrvBoxNumber jb)
        {
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == jb.boxNumber)
                {
                    b.state = NumberState.Доступен;
                    b.productNumbers.Clear();
                }
            }
            return true;
        }

        public bool BoxMarckAsBrack(string jb)
        {
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == jb)
                {
                    b.state = NumberState.Забракован;
                    b.productNumbers.Clear();
                }
            }
            return true;
        }

        public int GetPackCountInBox(string num)
        {
            if (cBox?.Number == num)
                return cBox.Numbers.Count;

            //проверка по текущим коробам
            foreach (BoxWithLayersOld c in boxQueue)
            {
                if (c.Number == num)
                    return c.Numbers.Count;
            }

            //
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == num)
                    return b.productNumbers.Count;
            }

            return -1;
        }
        public string GetBoxInfo(string num)
        {
            if(cBox?.Number == num)
                return "Короб входит в в задание и собирается";

            //проверка по текущим коробам
            foreach (BoxWithLayersOld c in boxQueue)
            {
                if(c.Number == num)
                    return "Короб входит в в задание,собран и ожидает верификации";
            }

            //
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == num)
                    return "Короб входит в задание и имеет состояние:  " + b.state.ToString();               
            }
            return "Короб не найден в задании";
        }
        public bool NotFullBoxAvaible(PartAggSrvBoxNumber jb)
        {
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {

                if (b.boxNumber != jb.boxNumber && b.state == NumberState.Собирается)
                    return false; ;

            }
            return true;
        }
        //проверяет соответствует ли код параметрам задания 
        // обработан ли он и не находится ли он в массиве браке
        public CodeState CheckCode(string code)
        {
            string s;
            return CheckCode(code,out s, false);
        }
        public CodeState CheckCode(string code, out string boxNum,bool removeNum)
        {
            boxNum = "";

            if (!VerifyProductNum(code))
                return CodeState.Missing;

            Util.GsLabelData ld = new Util.GsLabelData(code);
            if (ProductNumAlreadyVerify(ld.SerialNumber,out boxNum, removeNum))
                return CodeState.Verify;

            //проверить не входит ли код в массив отбракованных
            if (ProductNumAlreadyDefected(ld.SerialNumber))
                return CodeState.Bad;

            //проверить не входит ли код в массив отбракованных
            if (ProductNumAlreadySampled(ld.SerialNumber))
                return CodeState.Sample;

            //если в задании определена серия проверить в рамках серии
            if (order1C?.productNumbers?.Count > 0)
            {
                if(!order1C.productNumbers.Exists(x=> x== ld.SerialNumber))
                    return CodeState.WrongLot;
            }

            return CodeState.New;
        }
        public CodeState CheckCodeWinthFullNum(string code, out string boxNum, bool removeNum)
        {
            boxNum = "";

            if (!VerifyProductNum(code))
                return CodeState.Missing;

            Util.GsLabelData ld = new Util.GsLabelData(code);
            if (ProductNumAlreadyVerify(code, out boxNum, removeNum))
                return CodeState.Verify;

            //проверить не входит ли код в массив отбракованных
            if (ProductNumAlreadyDefected(ld.SerialNumber))
                return CodeState.Bad;

            //проверить не входит ли код в массив отбракованных
            if (ProductNumAlreadySampled(ld.SerialNumber))
                return CodeState.Sample;

            //если в задании определена серия проверить в рамках серии
            if (order1C?.productNumbers?.Count > 0)
            {
                if (!order1C.productNumbers.Exists(x => x == ld.SerialNumber))
                    return CodeState.WrongLot;
            }

            return CodeState.New;
        }


    }

}
