using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Util;
using System.Threading;

namespace FSerialization
{

    
    //класс задания выполняющегося на Сервере
    [DataContract]
    public class PartAggregateServerJob: AggCorobBaseInfo, IBaseJob
    {
        private OrderMeta meta = new OrderMeta();
        private ReaderWriterLockSlim _jobSync = new ReaderWriterLockSlim();

        
        [DataContract]
        public class DefectiveCodeSrv : DefectiveCode
        {
            public DefectiveCodeSrv(string idOp, string boxCode):base(idOp, boxCode)
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

                if ((meta.name == "" || meta.name == null) && order1C != null)
                    meta.name = "Cерия: " + order1C?.lotNo + "\n" + name + "\n" + DateTime.Now.ToString("dd MMMM HH:mm");
                else if (meta.name != "")
#pragma warning disable CS0642
                   ;
#pragma warning restore CS0642
                else
                    meta.name = "ошибка создания имени ";


                if (JobState == JobStates.InWork)
                {
                    if (readyBoxes?.Count > 0)
                    {
                        if(readyBoxes[0].state != NumberState.Доступен)
                            meta.state = JobIcon.JobInWork;
                        else if(brackBox?.Count > 0 )
                            meta.state = JobIcon.JobInWork;
                        else if (meta.state == JobIcon.JobInWork)
                            meta.state = JobIcon.Default;
                    }
                   
                }

                meta.id = id;
                meta.type = 0;

                //if (readyBoxes?.Count > 0)
                //{
                //    if(readyBoxes[0].state != NumberState.Доступен)
                //        meta.state = JobIcon.JobInWork;
                //}

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

                //если короб не находится в состоянии Verify или в состоянии NumberState.Забракован проверен сблросить флаг allverify 
                if ((b.state != NumberState.Верифицирован) && (b.state != NumberState.Забракован)) 
                    allVerify = false;
            }

            //если все  номера обработаны выставить признак что пора отправлять
            if (allVerify)
            {
                //уведомить о закрытии задания
                if(JobState != JobStates.CloseAndAwaitSend)
                    JobState = JobStates.WaitSend;

                LineAggregateJob lag = CreateNewLineAggregateJob("000000000000000000");
                lag.printBoxLabel = false;
                lag.JobState = JobStates.CloseAndAwaitSend;
                return lag;
            }

            //если задание не завершено выбрать все короба нахрдяшиеся в работе и сформировать сообщение 
            List<PartAggSrvBoxNumber> prBox = readyBoxes.FindAll((x) => x.state == NumberState.Собирается);
            if (prBox?.Count > 0) {
                LineAggregateJob lag = CreateNewLineAggregateJob("000000000000000000");
                lag.JobState = JobStates.InWorkNoNum;
                lag.printBoxLabel = false;
                lag.msg = "Свободные номера закончились. Задание ожидает завершения.\nСобирается коробов: "+prBox.Count.ToString();
                return lag;
            }

            return null;
        }
        public void ResetWorkBox()
        {
            //найти свободный номер короба
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Собирается)
                    b.state = NumberState.Доступен;
            }

            SafeToDisk();
        }
        public string RemoveBoxAndMarkAsBad(LineAggregateJob lj)
        {
            //найти свободный номер короба
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber == lj.checkedNumber )
                {
                    b.state = NumberState.Забракован;
                    b.productNumbers.Clear();
                    SafeToDisk();
                    Log.Write("Мастер id:" + lj.operatorId + ", удалил из отчета задания id:" + lj.id + " аггегированный короб:" + lj.checkedNumber);
                    return "";
                }
            }

           
            return "Короб " + lj.checkedNumber + " не найден в задании!";
        }
        public LineAggregateJobInfo GetWorckInfo()
        {
            LineAggregateJobInfo r = new LineAggregateJobInfo();
             //найти свободный номер короба
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.state == NumberState.Собирается)
                    r.BoxInWorck.Add(b);
            }
            return r;
        }

        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend {
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
            return SendReports(url, user, pass, partOfList, false, reguestTimeOut,repeat);
            
        }
        public string SendReports(string url, string user, string pass, bool partOfList,bool sendEmpty, int reguestTimeOut, bool repeat)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                PartAggregateReport r = CreateReportA();
                JobState = JobStates.SendInProgress;

                //проверить не пустой ли отчет?
                r.partOfList = partOfList;
                sendEmpty = true;

                //не проверять на пустой отчет
                if (sendEmpty)
                    OrderaArray.Add(r);
                else {
                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
                        OrderaArray.Add(r);
                }

                //выполнить отправку всех отчетов из массива
                while (OrderaArray.Count > 0)
                {
                    PartAggregateReport sr = OrderaArray.First();
                    string metod = partOfList ? "POST" : "PUT";
                    result = WebUtil.SendReport<PartAggregateReport>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"),id, reguestTimeOut);
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
        public string GetFuncName() { return "Агрегация"; }
        #endregion


        public event OrderAcceptedEventHandler OrderAcceptedEvent; // событие загрузки отчета.

        [DataMember]
        public PartAggregate1СOrder order1C;

       
        [DataMember]
        public List<DefectiveCodeSrv> brackBox = new List<DefectiveCodeSrv>();

        [DataMember]
        public DateTime startTime;
        [DataMember]
        public List<PartAggSrvBoxNumber> readyBoxes = new List<PartAggSrvBoxNumber>(); //массив обработанных номеров


        public PartAggSrvBoxNumber cBox = new PartAggSrvBoxNumber(""); //текущий обрабатываемый короб

        [DataMember]
        private List<PartAggregateReport> OrderaArray = new List<PartAggregateReport>(); //массив отчетов для отправки

        public PartAggregateServerJob() : base()
        {
            JobState = JobStates.Empty;
            meta.state = JobIcon.Default;
            jobType = typeof(PartAggregateServerJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            PartAggregate1СOrder o = order as PartAggregate1СOrder;
            if (o == null)
                return "Wrong object type. Need PartAggregate1СOrder";

            string ErrorReason = "";
            try
            {

                //создать задачу свервера 
                order1C = o;
                id = o.id;
                lotNo = o.lotNo;
                GTIN = o.gtin;
                ExpDate = o.ExpDate;
                formatExpDate = o.formatExpDate;
                //addProdInfo = o.addProdInfo;

                numLabelAtBox = o.numLabelAtBox;
                numLayersInBox = o.numLayersInBox;
                numРacksInBox = o.numРacksInBox;
                prefixBoxCode = o.prefixBoxCode;


                // pl.urlLabelBoxTemplate = "http://"+serverUrl+ "//GetFile//" + pl.id + "//Box.tmpl";

                startTime = DateTime.Now;

                boxLabelFields.AddRange(o.boxLabelFields);
                JobState = JobStates.InWork;// false;

                foreach (string s in o.boxNumbers)
                    readyBoxes.Add(new PartAggSrvBoxNumber(s));


                //перекинуть слеши из пути  если надо
                o.urlLabelBoxTemplate = o.urlLabelBoxTemplate.Replace("\u005c", "\u002f");
                //сохранить и загрузить шаблоны
                string fileName = System.IO.Path.GetDirectoryName(
                       System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

                //загрузить файл шаблона коробки
                if (!WebUtil.DownLoadFile(o.urlLabelBoxTemplate, user, pass,fileName, "Box.tmpl"))
                    return "Ошибка загрузки шаблона. url: " + o.urlLabelBoxTemplate;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }


            return ErrorReason;

        }
        public string AcceptOrderToWork(PartAggregate1СOrder o)
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
            formatExpDate = o.formatExpDate;
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
            //jobArray.Add(pl);
            //SaveOrder();

            if (OrderAcceptedEvent != null)
                OrderAcceptedEvent.Invoke(this);

            return ErrorReason;
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
                    string s = Archive.SerializeJSon<PartAggregateServerJob>(this);
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
        public static PartAggregateServerJob RestoreOrder()
        {
            try
            {
                string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\tmpOrder";

                if (System.IO.File.Exists(fileName))
                {
                    using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
                    {
                        string s = tmpFile.ReadToEnd();
                        tmpFile.Close();
                        tmpFile.Dispose();
                        PartAggregateServerJob result =  Archive.DeserializeJSon<PartAggregateServerJob>(s);
                        if (result == null)
                            throw new Exception("Не возможно восстановить имеющееся задание! Возможно файл поврежден!");

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return new PartAggregateServerJob();
        }
     
        public bool RemoveFromArray(PartAggregateReport r)
        {
            return OrderaArray.Remove(r);
        }
        private PartAggregateReport CreateReportA()
        {
            PartAggregateReport r = new PartAggregateReport();
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
                    r.defectiveCodes.Add(new DefectiveCode(b.id,b.number));
                    b.state = NumberState.VerifyAndPlaceToReport;
                }
            }

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

        //проверить короб на корректность. если все ок вернуть ок. если нет сгенерировать исключение с ошибкой
        public bool VerifyBox(PartAggSrvBoxNumber b)
        {
            if (b == null)
                throw new Exception("Ошибка запроса.Невозможно распознать данные короба");

            bool bIn = false;
            StringBuilder codeData = new StringBuilder();
            codeData.Append(";err01");// В коробе присутствуют\nуже обработанные номера\n");//"Повтор номеров:\n");

            StringBuilder brData = new StringBuilder();
            brData.Append(";В коробе присутствуют\nотбракованные номера\n");

            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                //проверка состояния короба
                if (b.boxNumber == br.boxNumber && br.state != NumberState.Собирается)
                    throw new Exception("Короб не может быть собран. Закройте задание сбросив короб и начните заново!");

                //поисk повторов номеров пачек в собранном
                if(br.state == NumberState.Верифицирован || br.state == NumberState.VerifyAndPlaceToReport) {
                    bIn = false;
                    foreach (string packCode in b.productNumbers)
                    {
                        if (br.IsAlreadyInBox(packCode))
                        {
                            if (!bIn)
                            {
                                bIn = true;
                                codeData.Append(";"+br.boxNumber + ":\n");
                            }
                            codeData.Append(packCode + " ");
                        }
                    }
                }
            }


            if (codeData.Length > 6)
                throw new Exception(codeData.ToString());

            //проерить в браке
            foreach (string packCode in b.productNumbers)
            {
                if (brackBox.Find((x)=>x.number == packCode) != null)
                    brData.Append(packCode + "\n");
            }

            if (brData.Length > 45)
                throw new Exception(brData.ToString());

            return true;
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
            Log.Write("Оператор id:" + idOperator + " забраковал код:" + code);
            SafeToDisk();
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

        public bool IsJObComplit()
        {
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
            pl.order1C = new PartAggregate1СOrder();
            pl.order1C.gtin = GTIN;
            pl.order1C.ExpDate = ExpDate;
            pl.order1C.formatExpDate = formatExpDate;
            // pl.addProdInfo = addProdInfo;

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

        public PartAggregate1СOrder GetJobHelp()
        {
            PartAggregate1СOrder order1C = new PartAggregate1СOrder();
            order1C.gtin = GTIN;
            order1C.ExpDate = ExpDate;
            order1C.lotNo = lotNo;
            // pl.addProdInfo = addProdInfo;

            order1C.numLabelAtBox = numLabelAtBox;
            order1C.numLayersInBox = numLayersInBox;
            order1C.numРacksInBox = numРacksInBox;
            order1C.prefixBoxCode = prefixBoxCode;
            order1C.urlLabelBoxTemplate = urlLabelBoxTemplate;
            //printBoxLabel = true;
            //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
            order1C.boxLabelFields.AddRange(boxLabelFields);

            return order1C;// CreateNewLineAggregateJob("");
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
                    Log.Write("Оператор id:" + jb.id + "верифицировал короб:" + b.boxNumber);
                    break;
                }
            }

            //проверить завершение задание
            bool verifyAll = true;
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if ((b.state != NumberState.Верифицирован)&& (b.state != NumberState.Забракован))
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
            string number = ld.SerialNumber;
            //проверить GTIN ее и тнвэд
            if (ld.GTIN != GTIN) 
                return false;
            

            //проверить доп поля если они есть
            if (ld.Charge_Number_Lot != null)
            {
                if (ld.Charge_Number_Lot != lotNo)
                    return false;
            }

            if (ld.ExpiryDate_JJMMDD != null)
            {
                if (ld.ExpiryDate_JJMMDD != ExpDate)
                    return false;
            }

            return true;
        }
        /// <summary>
        /// проеряет номер продукта на повтор
        /// </summary>
        /// <param name="prNum"></param>
        /// <returns></returns>
        public PartAggSrvBoxNumber ProductNumAlreadyVerify(string prNum)
        {
            
            if (prNum == null)
                return null;

            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.IsAlreadyInBox(prNum))
                    return b;  
            }
            return null;
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
            if (!VerifyProductNum(code))
                return CodeState.Missing;

            Util.GsLabelData ld = new Util.GsLabelData(code);
            if (ProductNumAlreadyVerify(ld.SerialNumber) != null)
                return CodeState.Verify;

            //проверить не входит ли код в массив отбракованных
            if (ProductNumAlreadyDefected(ld.SerialNumber))
                return CodeState.Bad;

            return CodeState.New;
        }

    }

    #region Old
    
//     //класс задания выполняющегося на Сервере
//    [DataContract]
//    public class PartAggregateServerJob: AggCorobBaseInfo, IBaseJob
//    {
//        private OrderMeta meta = new OrderMeta();
//        private ReaderWriterLockSlim _jobSync = new ReaderWriterLockSlim();

        
//        [DataContract]
//        public class DefectiveCodeSrv : DefectiveCode
//        {
//            public DefectiveCodeSrv(string idOp, string boxCode):base(idOp, boxCode)
//            {
//                state = NumberState.Верифицирован;
//            }
//            [DataMember]
//            public NumberState state { get; set; }
//        }
//        #region Реализация интерфейса BaseJob

//        [DataMember]
//        public OrderMeta JobMeta
//        {
//            get
//            {
//                string name = "";
//                //обновить данные считано\осталось
//                foreach (LabelField lf in boxLabelFields)
//                {
//                    if (lf.FieldName == "#productName#")
//                        name = lf.FieldData;
//                }

//                if ((meta.name == "" || meta.name == null) && order1C != null)
//                    meta.name = "Cерия: " + order1C?.lotNo + "\n" + name + "\n" + DateTime.Now.ToString("dd MMMM HH:mm");
//                else if (meta.name != "")
//                    ;
//                else
//                    meta.name = "ошибка создания имени ";

//                meta.id = id;
//                meta.type = 0;

//                //if (readyBoxes?.Count > 0)
//                //{
//                //    if(readyBoxes[0].state != NumberState.Доступен)
//                //        meta.state = JobIcon.JobInWork;
//                //}

//                return meta;
//            }
//            set { meta = value; }
//        }

//        [DataMember]
//        public JobStates JobState { get; set; }
//        public bool JobIsAwaible
//        {
//            get
//            {
//                if (JobState == JobStates.Complited)
//                    return false;

//               // if (JobState == JobState.CloseAndAwaitSend)
//               //     return false;

//               // if (JobState == JobState.WaitSend)
//               //     return false;

//                return true;
//            }
//        }

//        public object GetTsdJob()
//        {
//            bool allVerify = true;
//            //найти свободный номер короба
//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if (b.state == NumberState.Доступен)
//                {
//                    //номер найден сформировать задание
//                    LineAggregateJob lag = CreateNewLineAggregateJob(b.boxNumber);
//                    b.state = NumberState.Собирается;
//                    //отметить задание как поступившее в работу
//                    JobState = JobStates.InWorck;
//                    SafeToDisk();
//                    return lag;
//                }

//                //если короб числится отправленным игнорировать
//                if (b.state == NumberState.VerifyAndPlaceToReport)
//                    continue;

//                //если короб не находится в состоянии Verify проверен сблросить флаг allverify
//                if (b.state != NumberState.Верифицирован)
//                    allVerify = false;
//            }
//            //если все  номера обработаны выставить признак что пора отправлять
//            if (allVerify)
//            {
//                //уведомить о закрытии задания
//                if(JobState != JobStates.CloseAndAwaitSend)
//                    JobState = JobStates.WaitSend;

//                LineAggregateJob lag = CreateNewLineAggregateJob("000000000000000000");
//                lag.printBoxLabel = false;
//                lag.JobState = JobStates.CloseAndAwaitSend;
//                return lag;
//            }
            

//            return null;
//        }
//        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
//        public bool WaitSend {
//            get
//            {
//                if (JobState == JobStates.SendInProgress)
//                    return false;

//                if (JobState == JobStates.WaitSend)
//                    return true;

//                if (JobState == JobStates.CloseAndAwaitSend)
//                    return true;

//                if (OrderaArray.Count > 0)
//                    return true;
//                else
//                    return false;
//            }
//        }

//        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }

//        public string SendReports(string url, string user, string pass, bool partOfList)
//        {
//            return SendReports(url, user, pass, partOfList, false);
            
//        }
//        public string SendReports(string url, string user, string pass, bool partOfList,bool sendEmpty)
//        {
//            string result = "Нет данных для отчета";

//            if (JobState == JobStates.SendInProgress)
//                return "Отправка уже идет";

//            try
//            {
//                JobState = JobStates.SendInProgress;
//                //создать отчет
//                PartAggregateReport r = CreateReportA();
//                JobState = JobStates.SendInProgress;

//                //проверить не пустой ли отчет?
//                r.partOfList = partOfList;

//                //не проверять на пустой отчет
//                if (sendEmpty)
//                    OrderaArray.Add(r);
//                else {
//                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
//                        OrderaArray.Add(r);
//                }

//                //выполнить отправку всех отчетов из массива
//                while (OrderaArray.Count > 0)
//                {
//                    PartAggregateReport sr = OrderaArray.First();
//                    string metod = partOfList ? "POST" : "PUT";
//                    result = WebUtil.SendReport<PartAggregateReport>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));
//                    if (result != "")
//                        return result;

//                    OrderaArray.Remove(sr);
//                }
//            }
//            catch (Exception ex)
//            {
//                Log.Write(ex.Message);
//                return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
//            }
//            finally
//            {
//                if (result != "")
//                    JobMeta.state = JobIcon.ErrorSended;

//                //выставить состояние ожидание отправки но без закрытия задания целиком
//                JobState = JobStates.WaitSend;
//            }
//            return result;
//        }
//        public object GetReport() { throw new NotImplementedException(); }
//        public string GetFuncName() { return "Агрегация"; }
//        #endregion


//        public event OrderAcceptedEventHandler OrderAcceptedEvent; // событие загрузки отчета.

//        [DataMember]
//        public PartAggregate1СOrder order1C;

       
//        [DataMember]
//        public List<DefectiveCodeSrv> brackBox = new List<DefectiveCodeSrv>();

//        [DataMember]
//        public DateTime startTime;
//        [DataMember]
//        public List<PartAggSrvBoxNumber> readyBoxes = new List<PartAggSrvBoxNumber>(); //массив обработанных номеров


//        public PartAggSrvBoxNumber cBox = new PartAggSrvBoxNumber(""); //текущий обрабатываемый короб

//        [DataMember]
//        private List<PartAggregateReport> OrderaArray = new List<PartAggregateReport>(); //массив отчетов для отправки

//        public PartAggregateServerJob() : base()
//        {
//            JobState = JobStates.Empty;
//            meta.state = JobIcon.Default;
//            jobType = typeof(PartAggregateServerJob);
//        }
//        public override string InitJob<T>(T order, string user, string pass)
//        {
//            PartAggregate1СOrder o = order as PartAggregate1СOrder;
//            if (o == null)
//                return "Wrong object type. Need PartAggregate1СOrder";

//            string ErrorReason = "";
//            try
//            {

//                //создать задачу свервера 
//                order1C = o;
//                id = o.id;
//                lotNo = o.lotNo;
//                gtin = o.gtin;
//                ExpDate = o.ExpDate;
//                //addProdInfo = o.addProdInfo;

//                numLabelAtBox = o.numLabelAtBox;
//                numLayersInBox = o.numLayersInBox;
//                numРacksInBox = o.numРacksInBox;
//                prefixBoxCode = o.prefixBoxCode;


//                // pl.urlLabelBoxTemplate = "http://"+serverUrl+ "//GetFile//" + pl.id + "//Box.tmpl";

//                startTime = DateTime.Now;

//                boxLabelFields.AddRange(o.boxLabelFields);
//                JobState = JobStates.InWorck;// false;

//                foreach (string s in o.boxNumbers)
//                    readyBoxes.Add(new PartAggSrvBoxNumber(s));


//                //перекинуть слеши из пути  если надо
//                o.urlLabelBoxTemplate = o.urlLabelBoxTemplate.Replace("\u005c", "\u002f");
//                //сохранить и загрузить шаблоны
//                string fileName = System.IO.Path.GetDirectoryName(
//                       System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

//                //загрузить файл шаблона коробки
//                if (!WebUtil.DownLoadFile(o.urlLabelBoxTemplate, user, pass,fileName, "Box.tmpl"))
//                    return "Ошибка загрузки шаблона. url: " + o.urlLabelBoxTemplate;

//            }
//            catch (Exception ex)
//            {
//                return ex.Message;
//            }


//            return ErrorReason;

//        }
//        public string AcceptOrderToWork(PartAggregate1СOrder o)
//        {
//            string ErrorReason = "";
//            if (JobState != JobStates.Empty)
//                return "Сервис не может принять задание. Так как другое задание находится в работе";
//            //if (jobArray == null)
//            //    jobArray = new List<PartAggregateServerJob>();

//            //jobArray.Clear();

//            //создать задачу свервера
//            order1C = o;
//            // Sotex.Serialization.PartAggregateServerJob pl = new Sotex.Serialization.PartAggregateServerJob();
//            id = o.id;
//            lotNo = o.lotNo;
//            gtin = o.gtin;
//            ExpDate = o.ExpDate;
//            //addProdInfo = o.addProdInfo;

//            numLabelAtBox = o.numLabelAtBox;
//            numLayersInBox = o.numLayersInBox;
//            numРacksInBox = o.numРacksInBox;
//            prefixBoxCode = o.prefixBoxCode;
//            urlLabelBoxTemplate = "здесь линк на шаблон на сервере этом . а не 1с";

//            startTime = DateTime.Now;

//            boxLabelFields.AddRange(o.boxLabelFields);


//            foreach (string s in o.boxNumbers)
//                readyBoxes.Add(new FSerialization.PartAggSrvBoxNumber(s));

//            JobState = JobStates.New;
//            //jobArray.Add(pl);
//            //SaveOrder();

//            if (OrderAcceptedEvent != null)
//                OrderAcceptedEvent.Invoke(this);

//            return ErrorReason;
//        }

//        public bool SaveOrder()
//        {
//            try
//            {
//                string fileName = System.IO.Path.GetDirectoryName(
//                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

//                if (!System.IO.Directory.Exists(fileName))
//                    System.IO.Directory.CreateDirectory(fileName);

//                string copy1FileName = fileName + "tmpOrder1";
//                //если существует резервная копия2 удалить ее
//                if (System.IO.File.Exists(copy1FileName))
//                    System.IO.File.Delete(copy1FileName);

//                string copyFileName = fileName + "tmpOrder";
//                //если существует резервная копия переименовать ее
//                if (System.IO.File.Exists(copyFileName))
//                    System.IO.File.Move(copyFileName, copy1FileName);

//                fileName += "tmpOrder";
//                using (System.IO.TextWriter tmpFile = new System.IO.StreamWriter(fileName, false))
//                {
//                    string s = Archive.SerializeJSon<PartAggregateServerJob>(this);
//                    tmpFile.Write(s);
//                    tmpFile.Close();
//                    tmpFile.Dispose();
//                }

//                return true;

//            }
//            catch (Exception ex)
//            {
//                ex.ToString();
//                //Log.Write("Ошибка сохранения резервной копии!: " + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 701);
//            }
//            finally
//            {
//                //  orderSaveSync.ExitWriteLock();
//            }
//            //  }
//            //   else
//            //   {
//            //      Log.Write("SaveOrder Критическая ошибка очереди",EventLogEntryType.Error, MAIN_ERROR_CODE + 702);
//            //  }
//            return false;
//        }

//        public bool DeleteOrder()
//        {
//            try
//            {
//                string fileName = System.IO.Path.GetDirectoryName(
//                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

//                if (!System.IO.Directory.Exists(fileName))
//                    System.IO.Directory.CreateDirectory(fileName);

//                string copy1FileName = fileName + "tmpOrder";
//                //если существует  удалить ее
//                if (System.IO.File.Exists(copy1FileName))
//                    System.IO.File.Delete(copy1FileName);
      
//                return true;

//            }
//            catch (Exception ex)
//            {
//                ex.ToString();
//                //Log.Write("Ошибка сохранения резервной копии!: " + ex.Message,EventLogEntryType.Error, MAIN_ERROR_CODE + 701);
//            }
//            finally
//            {
//                //  orderSaveSync.ExitWriteLock();
//            }

//            return false;
//        }
//        public static PartAggregateServerJob RestoreOrder()
//        {
//            try
//            {
//                string fileName = System.IO.Path.GetDirectoryName(
//                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\tmpOrder";

//                if (System.IO.File.Exists(fileName))
//                {
//                    using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
//                    {
//                        string s = tmpFile.ReadToEnd();
//                        tmpFile.Close();
//                        tmpFile.Dispose();
//                        PartAggregateServerJob result =  Archive.DeserializeJSon<PartAggregateServerJob>(s);
//                        if (result == null)
//                            throw new Exception("Не возможно восстановить имеющееся задание! Возможно файл поврежден!");

//                        return result;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                ex.ToString();
//            }

//            return new PartAggregateServerJob();
//        }

       
//        public bool RemoveFromArray(PartAggregateReport r)
//        {
//            return OrderaArray.Remove(r);
//        }
//        private PartAggregateReport CreateReportA()
//        {
//            PartAggregateReport r = new PartAggregateReport();
//            r.id = id;
//            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
//            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");

//            //добавить обработанные коды
//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if (b.state == NumberState.Верифицирован)
//                {
//                    r.readyBox.Add(new ReadyBox(b));
//                    b.state = NumberState.VerifyAndPlaceToReport;
//                }
//            }

//            //добавить забракованные коды
//            foreach (DefectiveCodeSrv b in brackBox)
//            {
//                if (b.state == NumberState.Верифицирован)
//                {
//                    r.defectiveCodes.Add(new DefectiveCode(b.id,b.number));
//                    b.state = NumberState.VerifyAndPlaceToReport;
//                }
//            }

//            //пометить задание как отработанное
//            JobState = JobStates.WaitSend;
//            return r;
//        }
//        public bool AddVerifyBox(PartAggSrvBoxNumber b)
//        {
//            //найти  номер короба
//            foreach (PartAggSrvBoxNumber br in readyBoxes)
//            {
//                if (b.boxNumber == br.boxNumber)
//                {
//                    br.state = NumberState.Верифицирован;
//                    br.id = b.id;
//                    br.boxTime = b.boxTime;
//                    br.productNumbers.AddRange(b.productNumbers);
//                    return true;
//                }
//            }
//            return false;
//        }

//        //маркирует верифицированные данные как отосланные
//        public bool MarkAllReadyDataSended()
//        {
//            // удалить из задания отосланные данные
//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if (b.state == NumberState.Верифицирован)
//                    b.state = NumberState.VerifyAndPlaceToReport;
//            }

//            //очистить массив отбракованных
//            brackBox.Clear();

//            return true;
//        }
//        public void AddDefectCode(string code, string idOperator)
//        {
//            DefectiveCodeSrv dc = new DefectiveCodeSrv(idOperator, code);
//            brackBox.Add(dc);
//        }
//        public PartAggSrvBoxNumber GetNextBox()
//        {
//            //найти свободный номер короба
//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if (b.state == NumberState.Доступен)
//                {
//                    b.state = NumberState.Собирается;
//                    return b.Clone();
//                }
//            }
//            return null;
//        }

//        public bool IsJObComplit()
//        {
//            //задание не найдено проверить возможно все закрыто и надо отправлять отчет
//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if ((b.state == NumberState.Доступен) ||
//                    (b.state == NumberState.Собирается))
//                    return false;
//            }
//            //все коробки получили статус verify отправляем отчет
//            return true;
//        } 
//        /*
//        public bool JobIsComplited()
//        {
//            if (JobState == JobState.Complited)
//                return true;

//            foreach (PartAggSrvBoxNumber b in readyBoxes)
//            {
//                if (b.state == NumberState.New)
//                    return false;
//            }
//            return true;
//        }*/
//    private LineAggregateJob CreateNewLineAggregateJob(string boxNum)
//    {
//        LineAggregateJob pl = new LineAggregateJob();
//        pl.id = id;
//        pl.lotNo = lotNo;
//        pl.gtin = gtin;
//        pl.ExpDate = ExpDate;
//        // pl.addProdInfo = addProdInfo;

//        pl.numLabelAtBox = numLabelAtBox;
//        pl.numLayersInBox = numLayersInBox;
//        pl.numРacksInBox = numРacksInBox;
//        pl.prefixBoxCode = prefixBoxCode;
//        pl.urlLabelBoxTemplate = urlLabelBoxTemplate;
//        pl.printBoxLabel = true;
//        //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
//        pl.boxLabelFields.AddRange(boxLabelFields);


//        pl.selectedBox = new PartAggSrvBoxNumber(boxNum);
//        return pl;
//    }

//    public LineAggregateJob GetJobHelp()
//    {
//        return CreateNewLineAggregateJob("");
//    }
//    public bool BoxComplited(PartAggSrvBoxNumber jb)
//    {
//        foreach (PartAggSrvBoxNumber b in readyBoxes)
//        {
//            if (b.boxNumber == jb.boxNumber)
//            {
//                //сбросить короб
//                b.productNumbers.Clear();
//                //добавить даннные
//                b.state = NumberState.Верифицирован;
//                b.productNumbers.AddRange(jb.productNumbers);
//                b.id = jb.id;
//                b.boxTime = jb.boxTime;
//            }
//        }

//        //проверить завершение задание
//        bool verifyAll = true;
//        foreach (PartAggSrvBoxNumber b in readyBoxes)
//        {
//            if (b.state != NumberState.Верифицирован)
//                verifyAll = false;
//        }

//        if (verifyAll)
//            JobState = JobStates.WaitSend;

//        return true;
//    }



//    public bool VerifyProductNum(string code)
//    {
//        //проверить GTIN итд
//        Util.GsLabelData ld = new Util.GsLabelData(code);
//        string number = ld.SerialNumber;
//        //проверить GTIN ее и тнвэд
//        if (ld.EAN_NumberOfTradingUnit != gtin)
//            return false;


//        //проверить доп поля если они есть
//        if (ld.Charge_Number_Lot != null)
//        {
//            if (ld.Charge_Number_Lot != lotNo)
//                return false;
//        }

//        if (ld.ExpiryDate_JJMMDD != null)
//        {
//            if (ld.ExpiryDate_JJMMDD != ExpDate)
//                return false;
//        }

//        return true;
//    }
//    /// <summary>
//    /// проеряет номер продукта на повтор
//    /// </summary>
//    /// <param name="prNum"></param>
//    /// <returns></returns>
//    public bool ProductNumAlreadyVerify(string prNum)
//    {
//        if (prNum == null)
//            return false;

//        foreach (PartAggSrvBoxNumber b in readyBoxes)
//        {
//            if (b.IsAlreadyInBox(prNum))
//                return true;
//        }
//        return false;
//    }
//    /// <summary>
//    /// проеряет номер отбракованного продукта на повтор
//    /// </summary>
//    /// <param name="prNum"></param>
//    /// <returns></returns>
//    public bool ProductNumAlreadyDefected(string prNum)
//    {
//        if (prNum == null)
//            return false;

//        foreach (DefectiveCode b in brackBox)
//        {
//            if (b.number == prNum)
//                return true;
//        }
//        return false;
//    }
//    public bool BoxCancel(PartAggSrvBoxNumber jb)
//    {
//        foreach (PartAggSrvBoxNumber b in readyBoxes)
//        {
//            if (b.boxNumber == jb.boxNumber)
//            {
//                b.state = NumberState.Доступен;
//                b.productNumbers.Clear();
//            }
//        }
//        return true;
//    }
//    public bool NotFullBoxAvaible(PartAggSrvBoxNumber jb)
//    {
//        foreach (PartAggSrvBoxNumber b in readyBoxes)
//        {

//            if (b.boxNumber != jb.boxNumber && b.state == NumberState.Собирается)
//                return false; ;

//        }
//        return true;
//    }
//    //проверяет соответствует ли код параметрам задания 
//    // обработан ли он и не находится ли он в массиве браке
//    public CodeState CheckCode(string code)
//    {
//        if (!VerifyProductNum(code))
//            return CodeState.Missing;

//        Util.GsLabelData ld = new Util.GsLabelData(code);
//        if (ProductNumAlreadyVerify(ld.SerialNumber))
//            return CodeState.Verify;

//        //проверить не входит ли код в массив отбракованных
//        if (ProductNumAlreadyDefected(ld.SerialNumber))
//            return CodeState.Bad;

//        return CodeState.New;
//    }

//}
     
    #endregion
}
