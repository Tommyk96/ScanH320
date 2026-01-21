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
    public class FullPartAggServerJob : AggCorobBaseInfo, IBaseJob
    {
        private OrderMeta meta = new OrderMeta();
        private ReaderWriterLockSlim _jobSync = new ReaderWriterLockSlim();

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
                        if (readyBoxes[0].state != NumberState.Доступен)
                            meta.state = JobIcon.JobInWork;
                        else if (brackBox?.Count > 0)
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
        //[DataMember]
        //private string 


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
            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.state == NumberState.Доступен)
                {
                    //номер найден сформировать задание
                    FullPartAggTsdJob lag = CreateNewLineAggregateJob(b.boxNumber);
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
                if ((b.state != NumberState.VerifyAndPlaceToPalete) && (b.state != NumberState.Забракован))
                    allVerify = false;
            }

            //если все  номера обработаны выставить признак что пора отправлять
            if (allVerify)
            {
                //уведомить о закрытии задания
                if (JobState != JobStates.CloseAndAwaitSend)
                    JobState = JobStates.WaitSend;

                FullPartAggTsdJob lag = CreateNewLineAggregateJob("000000000000000000");
                lag.printBoxLabel = false;
                lag.JobState = JobStates.BoxesReadyWaitPalete;
                return lag;
            }
            else
            {
                //уведомить об отсутствии номеров коробов
                //уведомить о закрытии задания
                if (JobState != JobStates.BoxesReadyWaitPalete)
                    JobState = JobStates.BoxesReadyWaitPalete;

                FullPartAggTsdJob lag = CreateNewLineAggregateJob("");
                lag.printBoxLabel = false;
                lag.JobState = JobStates.BoxesReadyWaitPalete;
                return lag;
            }

            ////проверить есть ли доступные номера палет
            ////найти палеты в работе
            //FullPartAggPallete pal = readyPalets.FirstOrDefault(x => x.State == NumberState.Доступен);
            //if (pal == default)
            //{
            //    //если задание не завершено выбрать все короба нахрдяшиеся в работе и сформировать сообщение 
            //    List<FullPartAggPallete> prBox = readyPalets.FindAll((x) => x.State == NumberState.Собирается);
            //    if (prBox?.Count > 0)
            //    {
            //        FullPartAggTsdJob lag = CreateNewLineAggregateJob("000000000000000000");
            //        lag.JobState = JobStates.InWorkNoNum;//JobStates.BoxesReadyWaitPalete
            //        lag.printBoxLabel = false;
            //        lag.msg = "Свободные номера палет и коробов закончились. Задание ожидает завершения.\nСобирается коробов: " + prBox.Count.ToString();
            //        return lag;
            //    }
            //}

            //return null;
        }
        public void ResetWorkBox()
        {
            //найти свободный номер короба
            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.state == NumberState.Собирается)
                    b.state = NumberState.Доступен;
            }

            SafeToDisk();
        }
        public string RemoveBoxAndMarkAsBad(FullPartAggTsdJob lj)
        {
            //найти свободный номер короба
            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.boxNumber == lj.checkedNumber)
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
        public FullNumLineAggregateJobInfo GetWorckInfo()
        {
            FullNumLineAggregateJobInfo r = new FullNumLineAggregateJobInfo();
            //найти  номер короба в работе
            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.state == NumberState.Собирается)
                    r.BoxInWorck.Add(b);
            }

            r.BoxAvailable = readyBoxes.FindAll(x => x.state == NumberState.Доступен).Count;
            r.BoxVerify = readyBoxes.FindAll(x => x.state == NumberState.Верифицирован).Count;


            List<FullPartAggPallete> pal = readyPalets.FindAll(x => x.State == NumberState.Собирается);
            if (pal?.Count > 0)
                r.PalletInWorck.AddRange(pal);

            r.PalleteAvailable = readyPalets.FindAll(x => x.State == NumberState.Доступен).Count;
            return r;
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
            return SendReports(url, user, pass, partOfList, false, reguestTimeOut,repeat);

        }
        private const string NoData = "Нет данных для отчета";
        public string SendReports(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut, bool repeat)
        {
            string result = NoData;

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;

                //если идет повтор отправки не создавать отчет заново
                if (!repeat && OrderaArray.Count == 0)
                {
                    //создать отчет
                    FullPartAggReport r = CreateReportA();
                    JobState = JobStates.SendInProgress;

                    //проверить не пустой ли отчет?
                    r.partOfList = partOfList;
                    sendEmpty = true;

                    //не проверять на пустой отчет
                    if (sendEmpty)
                        OrderaArray.Add(r);
                    else
                    {
                        if ((r.Items.Count > 0) || (r.defectiveCodes.Count > 0))
                            OrderaArray.Add(r);
                    }
                }

                //выполнить отправку всех отчетов из массива
                while (OrderaArray.Count > 0)
                {
                    FullPartAggReport sr = OrderaArray.First();
                    string metod = sr.partOfList ? "POST" : "PUT";
                    result = WebUtil.SendReport<FullPartAggReport>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"), id, reguestTimeOut);
                    if (result != "")
                        return result;

                    OrderaArray.Remove(sr);
                    partOfList = sr.partOfList;
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
            }
            finally
            {
                if (result == "" && partOfList == true)
                {
                    result = "continue_work";
                    JobState = JobStates.InWork;
                }
                else if(result == NoData)
                {
                    JobMeta.state = JobIcon.JobInWork;
                    JobState = JobStates.InWork;
                }else if (result != "")
                    JobMeta.state = JobIcon.ErrorSended;
                else //выставить состояние ожидание отправки но без закрытия задания целиком
                    JobState = JobStates.WaitSend;  
            }
            return result;
        }
        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Агрегация"; }
        #endregion

        //public event OrderAcceptedEventHandler OrderAcceptedEvent; // событие загрузки отчета.

        [DataMember]
        public FullPartAgg1СOrder order1C;

        [DataMember]
        public List<DefectiveCodeSrv> brackBox = new List<DefectiveCodeSrv>();

        [DataMember]
        public DateTime startTime;
        [DataMember]
        public List<FullSerializeBox> readyBoxes = new List<FullSerializeBox>(); //массив обработанных номеров
        [DataMember]
        public List<FullPartAggPallete> readyPalets = new List<FullPartAggPallete>(); //массив обработанных номеров палет

        public FullSerializeBox cBox = new FullSerializeBox(""); //текущий обрабатываемый короб

        [DataMember]
        private List<FullPartAggReport> OrderaArray = new List<FullPartAggReport>(); //массив отчетов для отправки

        public FullPartAggServerJob() : base()
        {
            JobState = JobStates.Empty;
            meta.state = JobIcon.Default;
            jobType = typeof(FullPartAggServerJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            FullPartAgg1СOrder o = order as FullPartAgg1СOrder;
            if (o == null)
                return "Wrong object type. Need FullPartAgg1СOrder";

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
                numРacksInBox = o.numPacksInBox;
                //prefixBoxCode = o.prefixBoxCode;
               

                // pl.urlLabelBoxTemplate = "http://"+serverUrl+ "//GetFile//" + pl.id + "//Box.tmpl";

                startTime = DateTime.Now;

                boxLabelFields.AddRange(o.boxLabelFields);
                JobState = JobStates.InWork;// false;

                if (o.boxNumbers?.Count > 0)
                {
                    foreach (string s in o.boxNumbers)
                        readyBoxes.Add(new FullSerializeBox(s));
                }

                if (o.palletNumbers?.Count > 0)
                {
                    for (int i = 0; i < o.palletNumbers.Count; i++)
                        readyPalets.Add(new FSerialization.FullPartAggPallete(i + 1, o.palletNumbers[i]));
                }

                //перекинуть слеши из пути  если надо
                o.urlLabelBoxTemplate = o.urlLabelBoxTemplate.Replace("\u005c", "\u002f");
                //перекинуть слеши из пути  если надо
                o.urlLabelPalletTemplate = o.urlLabelPalletTemplate.Replace("\u005c", "\u002f");

                //сохранить и загрузить шаблоны
                string fileName = System.IO.Path.GetDirectoryName(
                       System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

                //загрузить файл шаблона коробки
                if (!WebUtil.DownLoadFile(o.urlLabelBoxTemplate, user, pass, fileName, "Box.tmpl"))
                    return "Ошибка загрузки шаблона. url: " + o.urlLabelBoxTemplate;

                //загрузить файл шаблона палеты
                if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "Pallete.tmpl"))
                    return "Ошибка загрузки шаблона. url: " + o.urlLabelBoxTemplate;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }


            return ErrorReason;

        }
        //private string AcceptOrderToWork(FullPartAgg1СOrder o)
        //{
        //    string ErrorReason = "";
        //    if (JobState != JobStates.Empty)
        //        return "Сервис не может принять задание. Так как другое задание находится в работе";

        //    //создать задачу свервера
        //    order1C = o;
        //    // Sotex.Serialization.PartAggregateServerJob pl = new Sotex.Serialization.PartAggregateServerJob();
        //    id = o.id;
        //    lotNo = o.lotNo;
        //    gtin = o.gtin;
        //    ExpDate = o.ExpDate;
        //    formatExpDate = o.formatExpDate;
        //    //addProdInfo = o.addProdInfo;

        //    numLabelAtBox = o.numLabelAtBox;
        //    numLayersInBox = o.numLayersInBox;
        //    numРacksInBox = o.numРacksInBox;
        //    prefixBoxCode = o.prefixBoxCode;
        //    urlLabelBoxTemplate = "здесь линк на шаблон на сервере этом . а не 1с";

        //    startTime = DateTime.Now;

        //    boxLabelFields.AddRange(o.boxLabelFields);


        //    foreach (string s in o.boxNumbers)
        //        readyBoxes.Add(new FSerialization.PartAggSrvBoxNumber(s));

        //    for(int i=0;i<o.palletNumbers.Count;i++)
        //        readyPalets.Add(new FSerialization.FullPartAggPallete(i+1,o.palletNumbers[i]));

        //    JobState = JobStates.New;
        //    //jobArray.Add(pl);
        //    //SaveOrder();

        //    if (OrderAcceptedEvent != null)
        //        OrderAcceptedEvent.Invoke(this);

        //    return ErrorReason;
        //}

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
                    string s = Archive.SerializeJSon<FullPartAggServerJob>(this);
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
        public static FullPartAggServerJob RestoreOrder()
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
                        FullPartAggServerJob result = Archive.DeserializeJSon<FullPartAggServerJob>(s);
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

            return new FullPartAggServerJob();
        }

        public bool RemoveFromArray(FullPartAggReport r)
        {
            return OrderaArray.Remove(r);
        }
        private FullPartAggReport CreateReportA()
        {
            FullPartAggReport r = new FullPartAggReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:sszz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");

            //добавить обработанные коды
            foreach (FullPartAggPallete b in readyPalets)
            {
                if (b.State == NumberState.Верифицирован)
                {
                    UnitItemStrong rPal = new UnitItemStrong(b.Id, (int)SelectedItem.ItemType.Паллета, b.Num);
                    //добавить в палету все ее короба
                    foreach (FullSerializeBox preBox in readyBoxes)
                    {
                        if (b.boxNumbers.Exists(x => x == preBox.boxNumber))
                        {
                            UnitItemStrong rBox = new UnitItemStrong(preBox.id, (int)SelectedItem.ItemType.Короб, preBox.boxNumber);
                            foreach (ItemNum str in preBox.productNumbers)
                                rBox.items.Add(new UnitItemStrong(preBox.id, (int)SelectedItem.ItemType.Упаковка, str.Sn));

                            rPal.items.Add(rBox);
                        }
                    }

                    r.Items.Add(rPal);
                    b.State = NumberState.VerifyAndPlaceToReport;
                    r.DataIsSet = true;
                }
            }

            //добавить в отчет верифицированные короба без палет
            foreach (FullSerializeBox rb in readyBoxes)
            {
                if (rb.state == NumberState.Верифицирован)
                {
                    UnitItemStrong rBox = new UnitItemStrong(rb.id, (int)SelectedItem.ItemType.Короб, rb.boxNumber);
                    foreach (ItemNum str in rb.productNumbers)
                        rBox.items.Add(new UnitItemStrong(rb.id, (int)SelectedItem.ItemType.Упаковка, str.Sn));

                    r.Items.Add(rBox);
                    rb.state = NumberState.VerifyAndPlaceToReport;
                    r.DataIsSet = true;
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

            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
        }

        private FullPartAggReport CreateReportB()
        {
            FullPartAggReport r = new FullPartAggReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:sszz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");

            //добавить обработанные коды
            //foreach (PartAggSrvBoxNumber b in readyBoxes)
            //{
            //    if (b.state == NumberState.Верифицирован)
            //    {
            //        r.readyBox.Add(new ReadyBox(b));
            //        b.state = NumberState.VerifyAndPlaceToReport;
            //    }
            //}

            //добавить обработанные коды
            foreach (FullPartAggPallete b in readyPalets)
            {
                if (b.State == NumberState.Верифицирован)
                {
                    UnitItemStrong rPal = new UnitItemStrong(b.Id, (int)SelectedItem.ItemType.Паллета, b.Num);
                    //добавить в палету все ее короба
                    //List<PartAggSrvBoxNumber> preBoxes = readyBoxes.FindAll(x => x.PalId == b.PalId);
                    //foreach (PartAggSrvBoxNumber readyBox in readyBoxes)
                    //{
                    foreach (FullSerializeBox preBox in readyBoxes)
                    {
                        if (b.boxNumbers.Exists(x => x == preBox.boxNumber))
                        {
                            UnitItemStrong rBox = new UnitItemStrong(preBox.id, (int)SelectedItem.ItemType.Короб, preBox.boxNumber);
                            foreach (ItemNum str in preBox.productNumbers)
                                rBox.items.Add(new UnitItemStrong(preBox.id, (int)SelectedItem.ItemType.Упаковка, str.FullNum));

                            rPal.items.Add(rBox);
                        }
                    }
                    //}

                    r.Items.Add(rPal);
                    b.State = NumberState.VerifyAndPlaceToReport;
                    r.DataIsSet = true;
                }
            }

            //добавить в отчет верифицированные короба без палет
            foreach (FullSerializeBox rb in readyBoxes)
            {
                if (rb.state == NumberState.Верифицирован)
                {
                    UnitItemStrong rBox = new UnitItemStrong(rb.id, (int)SelectedItem.ItemType.Короб, rb.boxNumber);
                    foreach (ItemNum str in rb.productNumbers)
                        rBox.items.Add(new UnitItemStrong(rb.id, (int)SelectedItem.ItemType.Упаковка, str.FullNum));

                    r.Items.Add(rBox);
                    rb.state = NumberState.VerifyAndPlaceToReport;
                    r.DataIsSet = true;
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

            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
        }

        public bool AddVerifyBox(FullSerializeBox b)
        {
            //найти  номер короба
            foreach (FullSerializeBox br in readyBoxes)
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
        public BoxCheckresult VerifyBox(FullSerializeBox b,bool removeDublicate)
        {
            //if (b == null)
            //    throw new Exception("Ошибка запроса.Невозможно распознать данные короба");

            //bool bIn = false;
            //StringBuilder codeData = new StringBuilder();
            //codeData.Append(";err01");// В коробе присутствуют\nуже обработанные номера\n");//"Повтор номеров:\n");

            //StringBuilder brData = new StringBuilder();
            //brData.Append(";В коробе присутствуют\nотбракованные номера\n");

            BoxCheckresult result = new BoxCheckresult();
            if (b == null)
            {
                result.msg = "Ошибка запроса.Невозможно распознать данные короба";
                return result;// throw new Exception("Ошибка запроса.Невозможно распознать данные короба");
            }
            bool bIn = false;
            result.msg = "err01";

            //найти  номер короба
            foreach (FullSerializeBox br in readyBoxes)
            {
                //проверка состояния короба
                if (b.boxNumber == br.boxNumber && br.state != NumberState.Собирается)
                {
                    result.msg = "Короб не может быть собран. Закройте задание сбросив короб и начните заново!";
                    return result;// throw new Exception("Короб не может быть собран. Закройте задание сбросив короб и начните заново!");
                }
                

                //поисk повторов номеров пачек в собранном
                if (br.state == NumberState.Верифицирован || br.state == NumberState.VerifyAndPlaceToReport || br.state == NumberState.VerifyAndPlaceToPalete)
                {
                    bIn = false;
                    Box foundBox = new Box() { Number = b.boxNumber };

                    foreach (ItemNum packCode in b.productNumbers)
                    {
                        if (br.IsAlreadyInBox(packCode.Sn))
                        {
                            if (!bIn)
                            {
                                bIn = true;
                                //codeData.Append(";" + br.boxNumber + ":\n");

                                if (removeDublicate)
                                {
                                    br.state = NumberState.Забракован;
                                    Log.Write($"Короб { br.boxNumber} забракован при повторной проверке");
                                }

                            }
                            foundBox.packNumbers.Add(packCode.Sn);
                            //codeData.Append(packCode + " ");
                        }
                    }
                    if (foundBox.packNumbers.Count > 0)
                        result.items.Add(foundBox);
                }
            }

            //removeDublicate
            if (result.items.Count > 0)
                return result;

            //проерить в браке
            foreach (ItemNum packCode in b.productNumbers)
            {
                if (brackBox.Find((x) => x.number == packCode.Sn) != null)
                    result.DefectCodes.Add(packCode.Sn);// brData.Append(packCode + "\n");
            }

            if (result.DefectCodes.Count > 0)
            {
                result.msg = "В коробе присутствуют отбракованные номера: ";
                foreach (string dc in result.DefectCodes)
                    result.msg += $"{dc} ";
                return result;
            }
            

            //if (codeData.Length > 6)
            //    throw new Exception(codeData.ToString());

            ////проерить в браке
            //foreach (string packCode in b.productNumbers)
            //{
            //    if (brackBox.Find((x) => x.number == packCode) != null)
            //        brData.Append(packCode + "\n");
            //}

            //if (brData.Length > 45)
            //    throw new Exception(brData.ToString());

            //вернуть все ОК!
            return new BoxCheckresult() { verify = true };
        }


        //маркирует верифицированные данные как отосланные
        public bool MarkAllReadyDataSended()
        {
            // удалить из задания отосланные данные
            foreach (FullSerializeBox b in readyBoxes)
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
        public FullSerializeBox GetNextBox()
        {
            //найти свободный номер короба
            foreach (FullSerializeBox b in readyBoxes)
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
            foreach (FullSerializeBox b in readyBoxes)
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
        private FullPartAggTsdJob CreateNewLineAggregateJob(string boxNum)
        {
            FullPartAggTsdJob pl = new FullPartAggTsdJob();
            pl.id = id;
            pl.lotNo = lotNo;
            pl.order1C = new FullPartAgg1СOrder();
            pl.order1C.gtin = GTIN;
            pl.order1C.ExpDate = ExpDate;
            pl.order1C.formatExpDate = formatExpDate;
            // pl.addProdInfo = addProdInfo;

            pl.order1C.numLabelAtBox = numLabelAtBox;
            pl.order1C.numLayersInBox = numLayersInBox;
            pl.order1C.numPacksInBox = numРacksInBox;
            //pl.order1C.prefixBoxCode = prefixBoxCode;
            pl.order1C.urlLabelBoxTemplate = urlLabelBoxTemplate;
            pl.printBoxLabel = true;
            //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
            pl.order1C.boxLabelFields.AddRange(boxLabelFields);


            pl.selectedBox = new FullSerializeBox(boxNum);
            return pl;
        }
        public FullPartAggTsdJob CreateNewLinePalAggregateJob(string tsdId)
        {
            //FullPartPalAggTsdJob pl = new FullPartPalAggTsdJob();
            FullPartAggTsdJob pl = new FullPartAggTsdJob();
            pl.id = id;
            pl.lotNo = lotNo;
            pl.order1C = new FullPartAgg1СOrder();
            pl.order1C.gtin = GTIN;
            pl.order1C.ExpDate = ExpDate;
            pl.order1C.formatExpDate = formatExpDate;
            // pl.addProdInfo = addProdInfo;

            pl.order1C.numLabelAtBox = numLabelAtBox;
            pl.order1C.numLayersInBox = numLayersInBox;
            pl.order1C.numPacksInBox = numРacksInBox;
            pl.order1C.numBoxInPallet = order1C.numBoxInPallet;
            //pl.order1C.prefixBoxCode = prefixBoxCode;
            pl.order1C.urlLabelBoxTemplate = urlLabelBoxTemplate;
            pl.printBoxLabel = true;
            //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
            //pl.order1C.boxLabelFields.AddRange(boxLabelFields);
            pl.order1C.palletLabelFields.AddRange(order1C.palletLabelFields);

            FullPartAggPallete pal = readyPalets.FirstOrDefault(x => x.State == NumberState.Доступен);

            if (pal == null)//не получили номер. возможно они кончились? 
            {
                pl.JobState = JobStates.InWorkNoNum;
                //pl.msg = "Выделенный пул номеров палет закончился\nЗавершите задание.";
                //проверить есть ли доступные номера палет
           
              //FullPartAggPallete pal = readyPalets.FirstOrDefault(x => x.State == NumberState.Доступен);
              // .. if (pal == default)
              //  {
                //если задание не завершено выбрать все короба нахрдяшиеся в работе и сформировать сообщение 
                List<FullSerializeBox> prBox = readyBoxes.FindAll((x) => x.state == NumberState.Собирается);
                List<FullSerializeBox> prverBox = readyBoxes.FindAll((x) => x.state == NumberState.Верифицирован);
                //если задание не завершено выбрать все короба нахрдяшиеся в работе и сформировать сообщение 
                List<FullPartAggPallete> prPal = readyPalets.FindAll((x) => x.State == NumberState.Собирается);
                //if (prBox?.Count > 0)
               // {
                    //FullPartAggTsdJob lag = CreateNewLineAggregateJob("000000000000000000");
                    pl.palNum = "";
                    pl.JobState = JobStates.InWorkNoNum;//JobStates.BoxesReadyWaitPalete
                    pl.printBoxLabel = false;
                    pl.msg = $"Свободные номера палет и коробов закончились. Задание ожидает завершения.\n\nСобирается коробов:{prBox.Count}\nСобирается палет:{prPal.Count}\nОжидает агрегацию в палету:{prverBox.Count}";
                    return pl;
               // }
               // }

            }
            else
            {
                pl.palNum = pal.Num;
                pal.State = NumberState.Собирается;
                pal.tsdId = tsdId;
                SaveOrder();
            }

            pl.boxNumbers.AddRange(from rb in readyBoxes
                                   where rb.state == NumberState.Верифицирован
                                   select rb.boxNumber);//new FullPartPalAggBox() { num = rb.boxNumber });
            return pl;
        }
        
        public FullPartAgg1СOrder GetJobHelp()
        {
            FullPartAgg1СOrder order1C = new FullPartAgg1СOrder();
            order1C.gtin = GTIN;
            order1C.ExpDate = ExpDate;
            order1C.lotNo = lotNo;
            // pl.addProdInfo = addProdInfo;

            order1C.numLabelAtBox = numLabelAtBox;
            order1C.numLayersInBox = numLayersInBox;
            order1C.numPacksInBox = numРacksInBox;
            //order1C.prefixBoxCode = prefixBoxCode;
            order1C.urlLabelBoxTemplate = urlLabelBoxTemplate;
            //printBoxLabel = true;
            //pl.serverUrl = "здесь поставить линк на сервис!"+id+ "idLineAggJob/";
            order1C.boxLabelFields.AddRange(boxLabelFields);

            return order1C;// CreateNewLineAggregateJob("");
        }
        public bool BoxComplited(FullSerializeBox jb)
        {
            foreach (FullSerializeBox b in readyBoxes)
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
                    Log.Write("Оператор id:" + jb.id + " верифицировал короб:" + b.boxNumber);
                    break;
                }
            }

            //проверить завершение задание
            bool verifyAll = true;
            foreach (FullSerializeBox b in readyBoxes)
            {
                if ((b.state != NumberState.Верифицирован) && (b.state != NumberState.Забракован))
                    verifyAll = false;
            }

            if (verifyAll)
                JobState = JobStates.BoxesReadyWaitPalete;

            return true;
        }
        public bool CloseAllEmptyBox()
        {
            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.state == NumberState.Доступен)
                    b.state = NumberState.Забракован;
            }

                //    if (b.boxNumber == jb.boxNumber)
                //    {
                //        //сбросить короб
                //        b.productNumbers.Clear();
                //        //добавить даннные
                //        b.state = NumberState.Верифицирован;
                //        b.productNumbers.AddRange(jb.productNumbers);
                //        b.id = jb.id;
                //        b.boxTime = jb.boxTime;
                //        Log.Write("Оператор id:" + jb.id + " верифицировал короб:" + b.boxNumber);
                //        break;
                //    }
                //}

                ////проверить завершение задание
                //bool verifyAll = true;
                //foreach (PartAggSrvBoxNumber b in readyBoxes)
                //{
                //    if ((b.state != NumberState.Верифицирован) && (b.state != NumberState.Забракован))
                //        verifyAll = false;
                //}

                //if (verifyAll)
                JobState = JobStates.BoxesReadyWaitPalete;

            return true;
        }
        public bool PalleteComplited(FullSerializeBox jb)
        {
            foreach (FullPartAggPallete b in readyPalets)
            {
                if (b.Num == jb.boxNumber)
                {
                    //сбросить короб
                    if (b.boxNumbers != null)
                        b.boxNumbers.Clear();
                    else
                        b.boxNumbers = new List<string>();
                    //добавить даннные
                    b.State = NumberState.Верифицирован;
                    
                    foreach(ItemNum itNum in jb.productNumbers)
                    b.boxNumbers.Add(itNum.Sn);

                    b.Id = jb.id;
                    b.boxTime = jb.boxTime;
                    //проставить статусы в списке коробов
                    foreach (string bn in b.boxNumbers)
                    {
                        foreach (FullSerializeBox rb in readyBoxes)
                        {
                            if (rb.boxNumber == bn)
                            {
                                rb.state = NumberState.VerifyAndPlaceToPalete;
                                break;
                            }
                        }
                    }

                    Log.Write($"Оператор id:{jb.id} верифицировал палету:{b.Num}");
                    break;
                }
            }

            //проверить завершение задание
            bool verifyAll = true;
            foreach (FullPartAggPallete b in readyPalets)
            {
                if ((b.State != NumberState.Верифицирован) && (b.State != NumberState.Забракован))
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
        public FullSerializeBox ProductNumAlreadyVerify(string prNum)
        {

            if (prNum == null)
                return null;

            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.state != NumberState.Верифицирован)
                    continue;

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
        public bool BoxCancel(FullSerializeBox jb)
        {
            if (jb == null)
                return false;

            foreach (FullSerializeBox b in readyBoxes)
            {
                if (b.boxNumber == jb.boxNumber)
                {
                    b.state = NumberState.Доступен;
                    b.productNumbers.Clear();
                }
            }
            return true;
        }
        public bool PalleteCancel(FullSerializeBox jb)
        {
            if (jb == null)
                return false;

            FullPartAggPallete pal  = readyPalets.FirstOrDefault(x=> x.Num == jb.boxNumber);
            if (pal != default(FullPartAggPallete))
            {
                pal.State = NumberState.Доступен;
                pal.boxNumbers.Clear();
            }
            return true;
        }
        
        public bool NotFullBoxAvaible(FullSerializeBox jb)
        {
            foreach (FullSerializeBox b in readyBoxes)
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
}
