using AgrBox.http;
using BoxAgr.BLL.Controllers.Interfaces;
using BoxAgr.BLL.Http.Jobs;
using BoxAgr.BLL.Http.Reports;
using BoxAgr.BLL.Http.Reports.Baikal;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.BLL.Models.Matrix;
using FSerialization;
using PharmaLegaсy.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;
using Utilite.Slutsk;

namespace BoxAgr.BLL.Controllers
{
    [DataContract]
    public class JobController : AggCorobBaseInfo, IBaseJob,IJob
    {
        private static ReaderWriterLockSlim readyBoxSync = new ReaderWriterLockSlim();

        private OrderMeta meta = new OrderMeta();
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
            return "Not implement";
        }

        public async Task<string> SendBaikalReport(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut, System.Threading.CancellationToken token)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                BaikalReport r = CreateBaikalReport();
                JobState = JobStates.SendInProgress;


                HttpHelper.RequestTimeout = new TimeSpan(0, 0, 0, 0, reguestTimeOut);
                result = await HttpHelper.SendReport<BaikalReport>(url, user, pass, "POST", r,
                    "AggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff", CultureInfo.InvariantCulture), id, reguestTimeOut, token);

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message).ConfigureAwait(false);
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

        public async Task<string> SendSerishevoReport(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut, System.Threading.CancellationToken token)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                SerishevoReport r = CreateReportSerishevo();
                JobState = JobStates.SendInProgress;


                HttpHelper.RequestTimeout = new TimeSpan(0, 0, 0, 0, reguestTimeOut);
                result = await HttpHelper.SendReport<SerishevoReport>(url, user, pass, "POST", r,
                    "AggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff", CultureInfo.InvariantCulture), id, reguestTimeOut, token);

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message).ConfigureAwait(false);
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

        public async Task<string> SendReportsBiotiki(string url, string user, string pass, bool partOfList, bool sendEmpty, int reguestTimeOut, System.Threading.CancellationToken token)
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
                if (OrderBiotikiArray == null)
                    OrderBiotikiArray = new List<BiotikiReport>();

                //не проверять на пустой отчет
                if (sendEmpty)
                    OrderBiotikiArray.Add(r);
                else
                {
                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
                        OrderBiotikiArray.Add(r);
                }

                //выполнить отправку всех отчетов из массива
                while (OrderBiotikiArray.Count > 0)
                {

                    BiotikiReport sr = OrderBiotikiArray.First();
                    ////сохранить пакет в архив
                    //WebUtil.SafeGzipFile(sr, "ReportArh", "Rep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));

                    string metod = partOfList ? "POST" : "PUT";

                    HttpHelper.RequestTimeout = new TimeSpan(0, 0, 0, 0, reguestTimeOut);
                    result = await HttpHelper.SendReport<BiotikiReport>(url, user, pass, metod, sr,
                        "AggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff", CultureInfo.InvariantCulture), id, reguestTimeOut, token);

                    //result = WebUtil.SendReport<BiotikiReport>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff", CultureInfo.InvariantCulture), id, reguestTimeOut);
                    //if (result != "")
                    //    return result;

                    OrderBiotikiArray.Remove(sr);
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

        public string SendUncReports(string dir, bool sendEmpty, int reguestTimeOut)
        {
            //string result = "Нет данных для отчета";
            string filePath = "";

            if (JobState == JobStates.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
                BiotikiReport r = CreateReportB();
                JobState = JobStates.SendInProgress;

                //проверить отчет на потерю данных
                //if (r.MarkList[0].Units.Count == 0 && readyBoxes.FirstOrDefault(x => x.state == NumberState.Верифицирован) is not null)
                //    throw new ArgumentNullException(nameof(MarkList.Units), "Внимание ошибка создания отчета!\nОтчет не может быть выгружен! Обратитесь к разработчику!");

                //не проверять на пустой отчет
                if (sendEmpty)
                    OrderBiotikiArray.Add(r);
                else
                {
                    if ((r.readyBox.Count > 0) || (r.defectiveCodes.Count > 0))
                        OrderBiotikiArray.Add(r);
                }

                //выполнить отправку всех отчетов из массива
                while (OrderBiotikiArray.Count > 0)
                {

                    BiotikiReport rep = OrderBiotikiArray.First();
                    //MarkList mr = rep.MarkList.FirstOrDefault();
                    //if (mr == default)
                    //    throw new Exception("Нет данных для отправки");

                    //// год - месяц - палет . 
                    ///
                    //string filename = $"{rep.MarkList.DocId}.{DateTime.Now.ToString("dd.mm.ss")}";
                    //string filename = $"{DateTime.Now.ToString("yyyyMMdd")}-{DateTime.Now.ToString("ss.fff")}";
                    string filename = $"{DateTime.Now.ToString("yyyy.MM.dd-HH.mm", CultureInfo.InvariantCulture)}.{GTIN}.{lotNo}";
                    //сохранить пакет в архив
                    WebUtil.SafeGzipFile(r, "ReportsArchiv", filename);

                    string fullPath = $"{dir}/{filename}.json";
                    filePath = fullPath;
                    string json = Archive.SerializeJSon<BiotikiReport>(rep);

                    byte[] data = Encoding.UTF8.GetBytes(json);
                    System.IO.File.WriteAllBytes(fullPath, data);

                    OrderBiotikiArray.Remove(rep);
                    return "";

                   
                }
            }
            catch (ArgumentNullException ex)
            {
                Log.Write(ex.ToString());
                return ex.Message;
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.Write($"path-{dir}\r\n{ex.ToString()}");
                return $"Ошибка отпраки отчета.\nПуть {dir} не найден!";
            }
            catch (Exception ex)
            {
                Log.Write($"path-{filePath}\r\n{ex.ToString()}");
                return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
            }
            finally
            {
                //if (result != "")
                //    JobMeta.state = JobIcon.ErrorSended;

                //выставить состояние ожидание отправки но без закрытия задания целиком
                JobState = JobStates.WaitSend;
            }
            return "";
        }
        public string SendFtpReports(string url, string user, string pass, string dir, bool sendEmpty, int reguestTimeOut)
        {
            //string result = "Нет данных для отчета";
            //string filePath = "";

            //if (JobState == JobStates.SendInProgress)
            //    return "Отправка уже идет";

            //try
            //{
            //    JobState = JobStates.SendInProgress;
            //    //создать отчет
            //    ReportSlutsk r = CreateReportSlutsk();
            //    JobState = JobStates.SendInProgress;

            //    //проверить отчет на потерю данных
            //    if (r.MarkList[0].Units.Count == 0 && readyBoxes.FirstOrDefault(x => x.state == NumberState.Верифицирован) is not null)
            //        throw new ArgumentNullException(nameof(MarkList.Units), "Внимание ошибка создания отчета!\nОтчет не может быть выгружен! Обратитесь к разработчику!");

            //    //не проверять на пустой отчет
            //    if (sendEmpty)
            //        OrderaArray.Add(r);
            //    else
            //    {
            //        if (r.MarkList[0]?.Units.Count > 0)
            //            OrderaArray.Add(r);
            //    }

            //    //выполнить отправку всех отчетов из массива
            //    while (OrderaArray.Count > 0)
            //    {

            //        ReportSlutsk rep = OrderaArray.First();
            //        MarkList mr = rep.MarkList.FirstOrDefault();
            //        if (mr == default)
            //            throw new Exception("Нет данных для отправки");

            //        //// год - месяц - палет . 
            //        ///
            //        //string filename = $"{rep.MarkList.DocId}.{DateTime.Now.ToString("dd.mm.ss")}";
            //        //string filename = $"{DateTime.Now.ToString("yyyyMMdd")}-{DateTime.Now.ToString("ss.fff")}";
            //        string filename = $"{DateTime.Now.ToString("yyyy.MM.dd-HH.mm", CultureInfo.InvariantCulture)}.{mr.Gtin}.{mr.Batch}";
            //        //сохранить пакет в архив
            //        WebUtil.SafeGzipFile(r, "ReportsArchiv", filename);

            //        string fullPath = $"{dir}/{filename}.json";
            //        filePath = fullPath;
            //        string json = Archive.SerializeJSon<ReportSlutsk>(r);


            //        using (var ftp = new FtpClient(url, user, pass))
            //        {
            //            ftp.Connect();


            //            byte[] data = Encoding.UTF8.GetBytes(json);
            //            FtpStatus fd = ftp.Upload(data, fullPath, FtpRemoteExists.Overwrite, true);
            //            if (fd == FtpStatus.Success)
            //                return "";

            //            //// upload a file and ensure the FTP directory is created on the server, verify the file after upload
            //            //await ftp.UploadFileAsync(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, true, FtpVerify.Retry, token: token);

            //        }

            //        //ReportSlutsk sr = OrderaArray.First();
            //        //string metod = partOfList ? "POST" : "PUT";
            //        //result = WebUtil.SendReport<ReportSlutsk>(url, user, pass, metod, sr, "TsdAggRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"), id, reguestTimeOut);
            //        //if (result != "")
            //        //    return result;

            //        OrderaArray.Remove(rep);
            //    }
            //}
            //catch (ArgumentNullException ex)
            //{
            //    Log.WriteConsoleLine(ex.ToString());
            //    return ex.Message;
            //}
            //catch (Exception ex)
            //{
            //    Log.WriteConsoleLine($"path-{filePath}\r\n{ex.Message}\r\n{ex.InnerException?.Message}");
            //    return "Ошибка отпраки отчета. Обратитесь в службу поддержки";
            //}
            //finally
            //{
            //    //if (result != "")
            //    //    JobMeta.state = JobIcon.ErrorSended;

            //    //выставить состояние ожидание отправки но без закрытия задания целиком
            //    JobState = JobStates.WaitSend;
            //}
            return "";
        }

        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Агрегация OSR"; }
        #endregion


        
        public event OrderAcceptedEventHandler? OrderAcceptedEvent; // событие загрузки отчета.

        public NewPartAggregate1СOrder? order1C { get; set; }


        [DataMember]
        public List<DefectiveCodeSrv> brackBox = new List<DefectiveCodeSrv>();

        [DataMember]
        public List<Sampled> sampleCodes = new List<Sampled>();

        [DataMember]
        public List<Operator> mastersArray = new List<Operator>();

        [DataMember]
        public DateTime startTime;

        [DataMember]
        public bool generateBoxNum { get; set; } = false;

        [DataMember]
        public int palletStartNumber { get; set; } = 1;

        [DataMember]
        public List<PartAggSrvBoxNumber> readyBoxes = new List<PartAggSrvBoxNumber>(); //массив обработанных номеров

        [DataMember]
        public List<PartAggSrvBoxNumber> readyPallets = new List<PartAggSrvBoxNumber>(); //массив обработанных номеров

 
        public Queue<BoxWithLayers> boxQueue = new Queue<BoxWithLayers>();

      
        public bool CurentBoxLeft;
        [DataMember]
        public string AppendBoxNum = "";
        [DataMember]
        public string DeletedPackNum = "";


        [DataMember]
        public List<ReportSlutsk> OrderaArray = new List<ReportSlutsk>(); //массив отчетов для отправки

        [DataMember]
        public List<BiotikiReport> OrderBiotikiArray = []; //массив отчетов для отправки

        [DataMember]
        public List<SerishevoReport> OrderSerishevoArray = []; //массив отчетов для отправки

        public JobController() : base()
        {
            JobState = JobStates.Empty;
            meta.state = JobIcon.Default;
            jobType = typeof(JobController);
        }
        public void Clear()
        {
            order1C = null;
            brackBox.Clear();
            mastersArray.Clear();
            startTime = DateTime.Now;
            generateBoxNum = false;
            palletStartNumber = 1;
            numLayersInBox = 0;
            numРacksInBox = 0;
            lotNo = "";
            order1C = new NewPartAggregate1СOrder();
            boxLabelFields.Clear();
            id = "";
            GTIN = "";
            //ExpDate = "";
            //Date = "";

            readyBoxes.Clear();
            readyPallets.Clear();
            boxQueue.Clear();
            CurentBoxLeft = false;
            AppendBoxNum = "";
            DeletedPackNum = "";
            OrderaArray.Clear();
            JobState = JobStates.Empty;
            meta = new OrderMeta();
        }
        public string AcceptOrderToWork(NewPartAggregate1СOrder o, IConfig settings)
        {
            string ErrorReason = "";
            if (JobState != JobStates.Empty)
                return "Сервис не может принять задание. Так как другое задание находится в работе";
            

            //создать задачу свервера
            order1C = o;
            id = o.id;
            lotNo = o.lotNo;
            GTIN = o.gtin;
            ExpDate = o.expDate;
            Date = o.Date;
           //переопределить дату 


            numLabelAtBox = o.numLabelAtBox;
            numLayersInBox = o.numLayersInBox;
            numРacksInBox = o.numРacksInBox;
            numPacksInLayer = o.numРacksInBox / (o.numLayersInBox < 1 ? 1 : o.numLayersInBox);

            prefixBoxCode = o.prefixBoxCode;
            urlLabelBoxTemplate = "здесь линк на шаблон на сервере этом . а не 1с";

            startTime = DateTime.Now;

            boxLabelFields.AddRange(o.boxLabelFields);


            foreach (string s in o.boxNumbers)
                readyBoxes.Add(new FSerialization.PartAggSrvBoxNumber(s));

            //сметнить режим работы в зависимости от типа
            JobState = JobStates.New;

            //обновить данные по матрице
            if (settings.BoxGrid)
            {
                if (BoxMatrixCatalog.GetMatrixAtGtin(o.gtin) is BoxMatrix sm)
                {
                    //settings.NumRows = sm.NumRows;
                    //settings.NumColumns = sm.NumColumns;
                    //settings.BoxHeight = sm.BoxHeight;
                    //settings.BoxWidth = sm.BoxWidth;
                }
            }
            //сохранить задание 
            Save1сOrder();

            if (OrderAcceptedEvent != null)
                OrderAcceptedEvent.Invoke(this);

            return ErrorReason;
        }
        public string AcceptSerializeOrderToWork(object obj, IConfig settings)
        {
            string ErrorReason = "";
            if (JobState != JobStates.Empty)
                return "Сервис не может принять задание. Так как другое задание находится в работе";

            if(obj is not S2Job o)
                return "Сервис не может принять задание. Так как задание не является сериализационным";

            //создать задачу свервера
            order1C = new NewPartAggregate1СOrder()
            {
                id = o.id,
                lotNo = o.lotNo,
                gtin = o.gtin,
                numРacksInBox = o.numPacksInBox,
                numPacksInSeries = o.numPacksInSeries,
                numBoxInPallet = 1,
                numLabelAtBox = 0,
                numLayersInBox = 1,               
                expDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            id = o.id;
            lotNo = o.lotNo;
            GTIN = o.gtin;
            ExpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Date = DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss");
            //переопределить дату 


            numLabelAtBox = 0;
            numLayersInBox = 1;
            numРacksInBox = o.numPacksInBox;
            numPacksInLayer = o.numPacksInBox;

            prefixBoxCode = string.Empty;
            urlLabelBoxTemplate = string.Empty; 

            startTime = DateTime.Now;

            //сметнить режим работы в зависимости от типа
            JobState = JobStates.New;

            //обновить данные по матрице
            if (settings.BoxGrid)
            {
                if (BoxMatrixCatalog.GetMatrixAtGtin(GTIN) is BoxMatrix sm)
                {
                    //settings.NumRows = sm.NumRows;
                    //settings.NumColumns = sm.NumColumns;
                    //settings.BoxHeight = sm.BoxHeight;
                    //settings.BoxWidth = sm.BoxWidth;
                }
            }
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
                    string s = Archive.SerializeJSon<JobController>(this);
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
        protected  bool FileDelete(string copy1FileName)
        {
            try
            {
                //using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                //{
                //    stream.Close();
                //}
                System.IO.File.Delete(copy1FileName);
            }
            catch (Exception)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return false;
            }

            //file is not locked
            return true;
        }
        public bool DeleteOrder()
        {
            string fileName = "";
            try
            {
                fileName = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\";

                if (!System.IO.Directory.Exists(fileName))
                    System.IO.Directory.CreateDirectory(fileName);

               


                

                string copy1FileName = fileName + "tmpOrder";
                //если существует  удалить ее
                if (System.IO.File.Exists(copy1FileName))
                {
                    //если файл залочен то подождать какоето время
                    int d = 10;
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);

                    while (!FileDelete(copy1FileName))
                    {
                        if (d <= 0)
                            break;

                        Thread.Sleep(1000);
                        d--;
                    }
                    Log.Write($"Удаление данных задания. {d}",EventLogEntryType.Error, 10700) ;
                    System.IO.File.Delete(copy1FileName);
                }


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

                copy1FileName = fileName + "Pallet.tmpl";
                //если существует резервная копия2 удалить ее
                if (System.IO.File.Exists(copy1FileName))
                    System.IO.File.Delete(copy1FileName);

                return true;

            }
            catch (ArgumentOutOfRangeException ex)
            {
                Log.Write($"Передан некорректный путь: {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (PlatformNotSupportedException ex)
            {
                Log.Write($"ОС не поддерживается: {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (DirectoryNotFoundException)
            {
                Log.Write($"Папка по заданному адресу не существует: \"{fileName}\"",EventLogEntryType.Error, 10701);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Write($"Нет прав для доступа : {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (SecurityException ex)
            {
                Log.Write($"Ошибка доступа: {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (ArgumentException ex)
            {
                Log.Write($"Ошибка аргумента: {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (PathTooLongException ex)
            {
                Log.Write($"Путь к директории слишком длинный. {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (IOException ex)
            {
                Log.Write($"Ошибка: {ex.Message}",EventLogEntryType.Error, 10701);
            }
            catch (Exception ex)
            {
               // ex.ToString();
                Log.Write("Ошибка удаления данных!: " + ex.ToString(),EventLogEntryType.Error, 10701);
            }
            finally
            {
                //  orderSaveSync.ExitWriteLock();
            }

            return false;
        }
        public static JobController RestoreOrder(IConfig config)
        {
            try
            {
                JobController result = null;

                string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\tmpOrder";

                if (System.IO.File.Exists(fileName))
                {
                    using (System.IO.TextReader tmpFile = new System.IO.StreamReader(fileName))
                    {
                        string s = tmpFile.ReadToEnd();
                        tmpFile.Close();
                        tmpFile.Dispose();
                        result = Archive.DeserializeJSon<JobController>(s);
                        if (result == null)
                            throw new Exception("Не возможно загрузить имеющееся задание! Возможно файл tmpOrder поврежден!");

                    }
                }
                //загрузить данные по заданию
                fileName = System.IO.Path.GetDirectoryName(
                  System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\tmp\\1сOrder";

                if (System.IO.File.Exists(fileName) && result is not null)
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

            return new JobController();
        }


        #region Работа с коробами
        public int GetVerufyQueueSize()
        {
            return boxQueue.Count;
        }
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
                    boxQueue = new Queue<BoxWithLayers>(boxQueue.Where(s => s.Number != BoxNum));

                    //пометить в очереди как брак
                    if(GetReadyBox(BoxNum) is PartAggSrvBoxNumber b)
                    { 

                    //    foreach (PartAggSrvBoxNumber b in readyBoxes)
                    //{
                    //    if (b.boxNumber == BoxNum)
                    //    {
                            b.state = NumberState.Забракован;
                            b.productNumbers.Clear();
                            return true;
                        //}
                    }
                   
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message,EventLogEntryType.Error, 1123);
                    return false;
                }
                finally
                {
                    readyBoxSync.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("Критическая ошибка очереди WasteBox",EventLogEntryType.Error, 31);
            }
            return false;
        }
        #endregion

        public bool RemoveFromArray(ReportSlutsk r)
        {
            return OrderaArray.Remove(r);
        }

        public BaikalReport CreateBaikalReport()
        {

            BaikalReport r = new()
            { id = id, startTime = startTime.ToString("yyyy-MM-ddThh:mm:sszz"), endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz") };

            r.operators = [];



            //добавить обработанные коды
            // Выбираем только часть строк, начиная с 16-го символа, и объединяем их в один список
            //List<string> readyProductNumbers = readyBoxes
            //    .SelectMany(box => box.productNumbers.Select(pn => pn[16..]))
            //    .ToList();

            //// Используем LINQ для выбора уникальных значений
            //List<string> uniqueProductNumbers = readyProductNumbers.Distinct().ToList();

            //проверка на корректность
            //if (readyProductNumbers.Count != uniqueProductNumbers.Count)
            //    Log.Write($"При формировании отчета обнаружены коды продукта с одинаковыми номерами!");

           
            // r.items.AddRange(pallets.Select(x => new BaikalItem(x)));

            string boxTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
            var boxes = readyBoxes.Where(x => x.state == NumberState.Верифицирован).Select(x => ToBaikalItem(x, boxTime)).ToList();
            r.items.AddRange(boxes);

           

            //добавить забракованные коды
            //foreach (DefectiveCodeSrv b in brackBox)
            //{
            //    if (b.state == NumberState.Верифицирован)
            //        r.defectiveCodes.Add(b.number);
            //}

            ////закрыть сессию последнего мастера
            if (mastersArray?.Count > 0)
            {
                mastersArray[mastersArray.Count - 1].endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
            }
            else
            {
                //сгенерировать ошибку по хорошему
            }

            var op = mastersArray?.Select(m => new UserAuthorizationHistotyItem() { id = m.id, endTime = m.endTime }).ToList();
            if (op?.Count > 0)
                r.operators.AddRange(op);


            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
        }

        private BaikalItem ToBaikalItem(PartAggSrvBoxNumber x, string time)
        {
            BaikalItem r = new() { type = 1, num = x.boxNumber, time = time };
            var pn = x.productNumbers.Select(x=> new BaikalItem() { type = 0, num = x }).ToList();
            r.items.AddRange(pn);
            return r;
        }

        public SerishevoReport CreateReportSerishevo()
        {
            SerishevoReport r = new SerishevoReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:sszz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");

            //добавить обработанные коды
            // Выбираем только часть строк, начиная с 16-го символа, и объединяем их в один список
            List<string> readyProductNumbers = readyBoxes
                .SelectMany(box => box.productNumbers.Select(pn => pn[16..]))
                .ToList();

            // Используем LINQ для выбора уникальных значений
            List<string> uniqueProductNumbers = readyProductNumbers.Distinct().ToList();

            //проверка на корректность
            if(readyProductNumbers.Count != uniqueProductNumbers.Count)
                Log.Write($"При формировании отчета обнаружены коды продукта с одинаковыми номерами!");


            r.Packs.AddRange(uniqueProductNumbers);

            //добавить забракованные коды
            foreach (DefectiveCodeSrv b in brackBox)
            {
                if (b.state == NumberState.Верифицирован)
                    r.defectiveCodes.Add(b.number);
            }

            ////закрыть сессию последнего мастера
            //if (mastersArray?.Count > 0)
            //{
            //    mastersArray[mastersArray.Count - 1].endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
            //}
            //else
            //{
            //    //сгенерировать ошибку по хорошему
            //}

            r.operators = [];//.AddRange(mastersArray);

            //пометить задание как отработанное
            JobState = JobStates.WaitSend;
            return r;
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
        public bool AddVerifyBoxWithFullNum(BoxWithLayers b, string userId,bool removeRepit = true)
        {
            //проверка на повторы
            // Объединяем все вложенные списки в один список
            List<string> allCodes = readyBoxes.SelectMany(innerList => innerList.productNumbers).ToList();
            // Находим уникальные значения в newData, которые не содержатся в flattenedData
            //List<string> uniqueNewData = b.Numbers.Except(flattenedData).ToList();

            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                if (b.Number == br.boxNumber)
                {
                    br.state = NumberState.Верифицирован;
                    br.id = userId;
                    br.boxTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");

                    foreach (Unit li in b.Numbers)
                    {
                        //проверяем номер на повтор вторично
                        if(allCodes.FirstOrDefault(x=>x == li.Barcode) is null)
                            br.productNumbers.Add(li.Barcode);
                    }
                    return true;
                }
            }
            return false;
        }
        public bool AddVerifyBox(BoxWithLayers b, string userId)
        {
            //найти  номер короба
            foreach (PartAggSrvBoxNumber br in readyBoxes)
            {
                if (b.Number == br.boxNumber)
                {
                    br.state = NumberState.Верифицирован;
                    br.id = userId;
                    br.boxTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                    foreach (Unit li in b.Numbers)
                        br.productNumbers.Add(li.Barcode);
                    return true;
                }
            }
            return false;
        }
        public int GetVerifyBoxCount()
        {
            if (readyBoxes == null)
                return 0;

            return readyBoxes.Count(x => x.state == NumberState.Верифицирован);
        }

        public int GetVerifyProductCount()
        {
            if (readyBoxes == null)
                return 0;

            return readyBoxes.Where(x => x.state == NumberState.Верифицирован
                    || x.state == NumberState.VerifyAndPlaceToReport
                    || x.state == NumberState.VerifyAndPlaceToPalete)
                     .Sum(x => x.productNumbers.Count);
        }


        public bool RemoveProduct(IBoxAssemblyController assemblyController, string _oldNum)
        {
          

            //замена в текушем 
            if (assemblyController.cBox.RemoveItem(_oldNum))
                return true;
          
            //поиск в очереди
            //проверить в коробах очереди
            foreach (BoxWithLayers l in boxQueue)
            {
                    if (l.Numbers.RemoveAll(item => item.Number == _oldNum) > 0)
                        return true;
            }

            //поиск в выпушенных
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {

                    if (b.productNumbers.RemoveAll(item => item.Contains($"{_oldNum}",StringComparison.OrdinalIgnoreCase)) > 0)
                        return true;               
            }
            return false;
        }
        public bool ReplaceNumInBox(IBoxAssemblyController assemblyController, string BoxNum, string _oldNum, string _newNum, string fullNumber, out bool boxAlreadyVerify)
        {
            boxAlreadyVerify = false;

            //замена в текушем 
            if (BoxNum == assemblyController.cBox.Number)
                return assemblyController.ReplaceNumInBox(_oldNum, fullNumber);

            //поиск в очереди
            //проверить в коробах очереди
            foreach (BoxWithLayers l in boxQueue)
            {
                if (l.Number == BoxNum)
                {
                    if (l.Numbers.RemoveAll(item => item.Number == _oldNum) < 1)
                        return false;

                    l.Numbers.Add(new Unit() { Number = _newNum, Barcode = fullNumber });
                    return true;
                }
            }

            //поиск в выпушенных
            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.boxNumber.Equals(BoxNum, StringComparison.OrdinalIgnoreCase))
                {
                    if (b.productNumbers.RemoveAll(item => item.Contains($"{_oldNum}", StringComparison.OrdinalIgnoreCase)) < 1)
                        return false;

                    b.productNumbers.Add(fullNumber);
                    boxAlreadyVerify = true;
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
        public (bool IsExist, string boxNum, bool IsCurrentLayer, bool IsAwaitVerify, int LayerNum) IsAlreadyInCurrentBoxes(IBoxAssemblyController assemblyController,string number)
        {
            if (string.IsNullOrEmpty(number))
                return (false, "", false, false,0);

            if(assemblyController.cBox is null)
                return (false, "", false, false,0);

            //проверить в коробах очереди ожидающих верификации
            foreach (BoxWithLayers l in boxQueue)
            {
                if (l.IsAlreadyInBox(number))
                    return (true, l.Number, false, true, 0);

            }

            //проверить в текущем слое 
            if (assemblyController.cBox.cLayer.FirstOrDefault(x => x.Number == number) is Unit l1)
                return (true, assemblyController.cBox.Number, false, false,l1.LayerNum);
            
            //проверить в текущем боксе
            if (assemblyController.cBox.Numbers.FirstOrDefault(x => x.Number == number) is Unit u)
                return (true, assemblyController.cBox.Number, false, false, u.LayerNum == 0 ? 1:u.LayerNum);
            
           
            return (false, "", false, false,0);
        }
        /// <summary>
        /// Ищет номер в обработанных коробах
        /// </summary>
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
        public PartAggSrvBoxNumber? GetReadyBox(string boxNum)
        {
            PartAggSrvBoxNumber? r = null;
            try
            {
                r = readyBoxes.FirstOrDefault(x => (x.state == NumberState.Верифицирован && x.boxNumber == boxNum));
                if (r is null)
                {
                    //поиск без кода группы 
                    if (boxNum[0..2] == "00" && boxNum.Length == 20)
                        r =  readyBoxes.FirstOrDefault(x => (x.state == NumberState.Верифицирован && x.boxNumber == boxNum[2..]));
                }
                return r;

            }
            catch 
            {
                ;
            }

            return r;
        }
        public bool IsProcessedBox(string boxNum)
        {
            //boxNum = "";
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            try
            {
                if (!readyBoxes.Exists(x => (x.state == NumberState.Верифицирован && x.boxNumber == boxNum)))
                {
                    //поиск без кода группы 
                    if (boxNum[0..2] == "00" && boxNum.Length == 20)
                        return readyBoxes.Exists(x => (x.state == NumberState.Верифицирован && x.boxNumber == boxNum[2..]));

                    return false;
                }
                return true;
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

        //public string  GetUnitByBarcode(string unitNum)
        //{
            
        //    try
        //    {
        //        //проверить в обработанных коробах
        //        foreach (PartAggSrvBoxNumber b in readyBoxes)
        //        {
        //            if (b.state == NumberState.Верифицирован)
        //            {
        //                if (b.IsAlreadyInBox(number))
        //                {
        //                    boxNum = b.boxNumber;
        //                    // Log.Write("foreach IsAlreadyInProcessedBox: " + sw.Elapsed);
        //                    return true;
        //                }
        //                //r.readyBox.Add(new ReadyBox(b));
        //                //b.state = NuberState.VerifyAndPlaceToReport;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        // Log.Write("IsAlreadyInProcessedBox: " + sw.Elapsed);
        //        //sw.Stop();
        //    }
        //}

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

        public BoxWithLayers GetNextBoxWithLayers(out int ost)
        {
            //если номера коробов в задании не пришли или не были указаны вообще
            //то считаем что идет сериализация. это в корне не верно . но пока так
            if (order1C is not null && (order1C?.boxNumbers == null || order1C.boxNumbers.Count < 1))
            {
                ost = 30;
                var b = new PartAggSrvBoxNumber(Guid.NewGuid().ToString()) { state = NumberState.Собирается };
                readyBoxes.Add(b);
                return new BoxWithLayers(b.boxNumber, order1C.numLayersInBox, order1C.numPacksInBox, BoxWLState.InProgress);
            }

            ost = -1;
            //найти свободный номер короба
            for (int i = 0; i < readyBoxes.Count; i++)
            {
                PartAggSrvBoxNumber b = readyBoxes[i];

                if (b.state == NumberState.Доступен)
                {
                    b.state = NumberState.Собирается;
                    ost = readyBoxes.Count - i;
                    return new BoxWithLayers(b.boxNumber, order1C.numLayersInBox, order1C.numPacksInBox, BoxWLState.InProgress);
                }
                else if (b.state == NumberState.Собирается)
                {
                    //если мы нашли короб который собирается. проверить есть ли он в очереди на сборку
                    //возможно он остался в этом состоянии после перезагрузки и его можно использовать заново
                    if (boxQueue?.Count < 1)
                    {
                        ost = readyBoxes.Count - i;
                        return new BoxWithLayers(b.boxNumber,order1C.numLayersInBox, order1C.numPacksInBox, BoxWLState.InProgress);
                    }
                }
            }

            //если номеров нет и стоит флаг генерировать номера сгенерить новый номер
            //if (ost < 0 && generateBoxNum)
            //{
            //    List<PartAggSrvBoxNumber> result = GeterateNewBoxNum(10);
            //    readyBoxes.AddRange(result);
            //    return new BoxWithLayers(result[0].boxNumber, 1/*order1C.numLayersInBox*/, order1C.numРacksInBox);
            //}
            //если номеров коробов нет проверить номера в состоянии собирается.
            //возможно после перезапуска остался какойто номер собираемый и теперь он свободен

            return null;
        }

        public List<PartAggSrvBoxNumber> GenerateNewBoxNum(int max,int Counter = 1)
        {
            List<PartAggSrvBoxNumber> result = new List<PartAggSrvBoxNumber>();
            //int Counter = 0;
            PartAggSrvBoxNumber lastBox = readyBoxes.LastOrDefault();

            if (lastBox != default)
            {
                GsLabelBoxData gsLabelBoxData = new GsLabelBoxData(lastBox.boxNumber);
                int temp;
                if (int.TryParse(gsLabelBoxData.SerialNumber, out temp))
                {
                    Counter = temp;
                    Counter++;
                }
            }

            string manDate = order1C.ManufactureDate.ToString("yyMMdd");
            string ItemInBox = order1C.numPacksInBox.ToString();
            //коррекция начального номера 
            if(Counter < 1)
                Counter =1;

            //добавить номера коробов
            for (int i = 0; i < max; i++)
            {
               
                result.Add(new PartAggSrvBoxNumber($"01{GTIN}11{manDate}10{lotNo}\u001d37{ItemInBox}\u001d21{Counter:D5}"));
                Counter++;
            }

            return result;
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

            if (ld.SerialNumber == null)
                return "Ошибка распознавания серийного номера";

            if (string.IsNullOrEmpty(ld.field93Sign))
            {
                if (ld.field91Key == null)
                    return "Ошибка распознавания номера сертификата";

                if (ld.CryptoHash == null)
                    return "Ошибка распознавания крипто подписи";
            }

            if (order1C == null)
                return "Невозможно верифицировать номер " + ld.SerialNumber + ". Нет задания.";

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
            //if (ld.Charge_Number_Lot != null)
            //{
            //    if (order1C.lotNo != "")
            //    {
            //        if (ld.Charge_Number_Lot != order1C.lotNo)
            //            return "Пачка другой серии!";
            //    }
            //}

            //if (ld.ExpiryDate_JJMMDD != null)
            //{
            //    if (order1C.ExpDate != "")
            //    {
            //        if (ld.ExpiryDate_JJMMDD != order1C.ExpDate)
            //            return "Не совпадает срок годности!";
            //    }
            //}


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
        public bool ProductNumAlreadyVerify(string prNum, out string boxNum, bool remove = false)
        {
            boxNum = "";
            if (prNum == null)
                return false;

            foreach (PartAggSrvBoxNumber b in readyBoxes)
            {
                if (b.IsAlreadyInBox(prNum))
                {
                    boxNum = b.boxNumber;
                    if (remove)
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

        public int GetPackCountInBox(IBoxAssemblyController assemblyController, string num)
        {
            if (assemblyController.cBox?.Number == num)
                return assemblyController.cBox.Numbers.Count;

            //проверка по текущим коробам
            foreach (BoxWithLayers c in boxQueue)
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
            //if (cBox?.Number == num)
            //    return "Короб входит в в задание и собирается";

            //проверка по текущим коробам
            foreach (BoxWithLayers c in boxQueue)
            {
                if (c.Number == num)
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
            return CheckCode(code, out s, false);
        }
        public CodeState CheckCode(string code, out string boxNum, bool removeNum)
        {
            boxNum = "";

            if (!VerifyProductNum(code))
                return CodeState.Missing;

            Util.GsLabelData ld = new Util.GsLabelData(code);
            if (ProductNumAlreadyVerify(ld.SerialNumber, out boxNum, removeNum))
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
