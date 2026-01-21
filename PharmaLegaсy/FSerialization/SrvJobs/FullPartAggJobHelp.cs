using FSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FSerialization
{
     //класс задания выполняющегося на Сервере
    [DataContract]
    public class FullPartAggJobHelp : TmplServerJob<AcceptAll1СOrder, AcceptAllReport, AcceptAllJob>
    {

        #region Реализация интерфейса BaseJob
        //делается в функции которая вызывает эту
        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            AcceptAllJob sj = new AcceptAllJob();

            sj.order1C = order1C;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            sj.lotNo = lotNo;
            sj.id = id;

            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return sj;
        }
        public override string GetFuncName() { return "Приёмка по факту"; }
        #endregion

        public FullPartAggJobHelp() : base()
        {
            meta.state = JobIcon.Default;
            jobType = typeof(AcceptServerAllJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            string ErrorReason = "";
            AcceptAll1СOrder o = order as AcceptAll1СOrder;
            if (o == null)
                return "Wrong object type.Need AcceptAll1СOrder";
            order1C = o;
            id = o.id;
            meta.name = "Накл.: " + order1C?.invoiceNum + "\n" + order1C.createDateTime;//.ToString("dd MMMM HH:mm");// + "\n" + order1C?.productName;
            startTime = DateTime.Now;
            JobState = JobStates.New;


            return ErrorReason;
        }
        public override string ParceReport<T>(T rep)
        {
            AcceptAllJob rj = rep as AcceptAllJob;
            if (rj == null)
                return "При формировании отчета произошла критическая ошибка. Обратитесь в службу поддержки";

            //создать отчет
            readyReport = new AcceptAllReport();
            readyReport.id = id;
            readyReport.OperatorId = rj.operators.Last().id;

            foreach (UnitItem i in rj.SelectedItems)
            {
                readyReport.Items.Add(i.GetUnitItemM());
            }
            //сохранить в архив 
            Archive.SaveReport<AcceptAllReport>(readyReport, id);

            return "";
        }

    }
}
