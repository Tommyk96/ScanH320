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
    public class SamplesServerJob : TmplServerJob<Samples1СOrder, SamplesReport, SamplingJob>//AggCorobBaseInfo, IBaseJob
    {
      
        #region Реализация интерфейса BaseJob
        public override object GetTsdJob()
        {
            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return order1C;
        }       
        public override string ParceReport<T>(T rep) {


            try
            {
                SamplingJob sJob = rep as SamplingJob;
                if (sJob == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

                readyReport = new SamplesReport();
                readyReport.id = id;
                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.OperatorId = sJob.operators?.Last().id;

                foreach (SamplingJob.SampledObject so in sJob.sampledObjects)
                {
                    switch (so.type)
                    {

                        case 1:
                            //добавить обработанные коды
                            readyReport.moveNumbers.Add(new SamplingJob.MoveNumbers(so.boxNum, so.Number));
                            break;
                        case 2:
                            //добавить обработанные коды
                            readyReport.sampleNumbers.Add(so.Number);
                            break;
                        default:

                            break;
                    }
                }               
                //сохранить в архив 
                Archive.SaveReport<SamplesReport>(readyReport, id);
            }
            catch (Exception ex) { return ex.Message; }
            return "";
        }
        public override string GetFuncName() { return "Образцы"; }
        #endregion


        [DataMember]
        public List<string> sampledNumbers = new List<string>();
        [DataMember]
        public List<SamplingJob.MoveNumbers> movedNumbers = new List<SamplingJob.MoveNumbers>();

        public SamplesServerJob() : base()
        {
            meta.state = JobIcon.Default;
            jobType = typeof(SamplesServerJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            Samples1СOrder o = order as Samples1СOrder;
            if (o == null)
                return "Wrong object type. Need Samples1СOrder";

            string ErrorReason = "";

            //o.CheckContent();

            order1C = o;
            id = o.id;
            lotNo = o.lotNo;
            GTIN = o.gtin;
            ExpDate = o.expDate;
            //addProdInfo = o.addProdInfo;

            // pl.urlLabelBoxTemplate = "http://" + serverUrl + "//GetFile//" + pl.id + "//Pallete.tmpl";

            startTime = DateTime.Now;
            JobState = JobStates.New;

            //перекинуть слеши из пути  если надо
            o.urlLabelPalletTemplate = o.urlLabelPalletTemplate.Replace("\u005c", "\u002f");
            //сохранить и загрузить шаблоны
            string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

            //загрузить файл шаблона коробки
            if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "Pallete.tmpl"))
                return "Ошибка загрузки шаблона. url: " + o.urlLabelPalletTemplate;

            return ErrorReason;
        }

    }
    #region Old
    /*
[DataContract]
public class SamplesServerJob : AggCorobBaseInfo, IBaseJob
{
    private OrderMeta meta = new OrderMeta();
    #region Реализация интерфейса BaseJob

    [DataMember]
    public OrderMeta JobMeta
    {
        get
        {
            meta.name = "Серия:" + lotNo + "\n" + order1C.productName;
            meta.id = id;
            meta.type = 0;
            //meta.state = 0;
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

            if (JobState == JobStates.InWorck)
                return false;



            return true;
        }
    }

    public object GetTsdJob()
    {
        //отметить задание как поступившее в работу
        JobState = JobStates.InWorck;
        return order1C;
    }
    public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
    public bool WaitSend
    {
        get
        {
            if (JobState == JobStates.WaitSend)
                return true;

            if (JobState == JobStates.CloseAndAwaitSend)
                return true;

            return false;
        }
    }

    public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
    public string SendReports(string url, string user, string pass, bool partOfList)
    {
        string result = "Нет данных для отчета";

        if (JobState == JobStates.SendInProgress)
            return "Отправка уже идет";

        try
        {
            JobState = JobStates.SendInProgress;
            if(readyReport == null)
                return "Критическая ошибка формирования отчета. обратитесь в службу поддержки!";
            //создать отчет
            if (readyReport.id != order1C.id)
                return "Ошибка создания отчета. не совпадает ид задания и отчета!";
            //выполнить отправку 
            result = WebUtil.SendReport<SamplesReport>(url, user, pass, "POST", readyReport, "SmplRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));
            if (result != "")
            {
                return result;
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

            JobState = JobStates.CloseAndAwaitSend;
        }
        return result;
    }

    #endregion
    [DataMember]
    private SamplesReport readyReport = new SamplesReport(); //массив отчетов для отправки

    [DataMember]
    public Samples1СOrder order1C;

    [DataMember]
    public List<string> sampledNumbers = new List<string>();
    [DataMember]
    public List<SamplingJob.MoveNumbers> movedNumbers = new List<SamplingJob.MoveNumbers>();

    [DataMember]
    public DateTime startTime;
    public SamplesServerJob() : base()
    {
        meta.state = JobIcon.Default;
        jobType = typeof(SamplesServerJob);
    }
    public override string InitJob<T>(T order, string user, string pass)
    {
        Samples1СOrder o = order as Samples1СOrder;
        if (o == null)
            return "Wrong object type. Need Samples1СOrder";

        string ErrorReason = "";

        //o.CheckContent();

        order1C = o;
        id = o.id;
        lotNo = o.lotNo;
        gtin = o.gtin;
        ExpDate = o.expDate;
        addProdInfo = o.addProdInfo;

        // pl.urlLabelBoxTemplate = "http://" + serverUrl + "//GetFile//" + pl.id + "//Pallete.tmpl";

        startTime = DateTime.Now;
        JobState = JobStates.New;

        //перекинуть слеши из пути  если надо
        o.urlLabelPalletTemplate = o.urlLabelPalletTemplate.Replace("\u005c", "\u002f");
        //сохранить и загрузить шаблоны
        string fileName = System.IO.Path.GetDirectoryName(
               System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

        //загрузить файл шаблона коробки
        if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate,"","", fileName, "Pallete.tmpl"))
            return "Ошибка загрузки шаблона. url: " + o.urlLabelPalletTemplate;

        return ErrorReason;
    }
    public SamplesReport CreateReport(SamplingJob sJob)
    {
        SamplesReport r = new SamplesReport();
        r.id = id;
        r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
        r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        r.OperatorId = sJob.operators?.Last().id;

        foreach (SamplingJob.SampledObject so in sJob.sampledObjects)
        {
            switch (so.type)
            {

                case 1:
                    //добавить обработанные коды
                    r.moveNumbers.Add(new SamplingJob.MoveNumbers(so.boxNum, so.Number));
                    break;
                case 2:
                    //добавить обработанные коды
                    r.sampleNumbers.Add(so.Number);
                    break;
                default:

                    break;
            }
        }

        //пометить задание как отработанное
        JobState = JobStates.WaitSend;
        readyReport = r;
        return r;

    }

    public bool IsJObComplit()
    {
        /*
        //задание не найдено проверить возможно все закрыто и надо отправлять отчет
        foreach (PartAggSrvBoxNumber b in boxNumbers)
        {
            if (b.state != NumberState.Verify)
                return false;
        }
        //все коробки получили статус verify отправляем отчет
        return true;
    }

    public bool JobIsComplited()
    {
        if (JobState == JobStates.Complited)
            return true;

        return false;
    }
}
*/
    #endregion
}
