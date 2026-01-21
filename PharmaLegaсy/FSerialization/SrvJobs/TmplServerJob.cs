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
    public class TmplServerJob<Order,Report, TsdJob> : AggCorobBaseInfo, IBaseJob
        where Order: Order1CBaseInfo
        where TsdJob : baseTsdAccJob<Order>, new() 
        where Report: BaseReportInfo, new()
    {

        #region Реализация интерфейса BaseJob
        public OrderMeta meta = new OrderMeta();

        [DataMember]
        public virtual OrderMeta JobMeta
        {
            get
            {

                if ((meta.name == "" || meta.name == null) && order1C != null)
                    meta.name = "Cерия: " + order1C?.lotNo + "\n" + order1C?.productName + "\n" + DateTime.Now.ToString("dd MMMM HH:mm");
                else if (meta.name != "")
#pragma warning disable CS0642
                    ;
#pragma warning restore CS0642
                else
                    meta.name = "ошибка создания имени ";

                meta.id = id;
                meta.type = 0;
               
                return meta;
            }
            set { meta = value; }
        }
        [DataMember]
        public JobStates JobState { get; set; }
        public virtual bool JobIsAwaible
        {
            get
            {
                if (JobState == JobStates.New)
                    return true;

                if (JobState == JobStates.Paused)
                    return true;

                if (JobState == JobStates.WaitSend)
                    return true;

                if (JobState == JobStates.CloseAndAwaitSend)
                    return true;
                
                return false;
            }
        }
        public virtual object GetTsdJob() {throw new NotImplementedException(); }
        public virtual object GetTsdSqLiteJob() { throw new NotImplementedException(); }
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
        public virtual string ParceReport<T>(T rep)
        {
            throw new NotImplementedException();
        }
        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobStates.SendInProgress)
                return "Отправка задания идет. id: "+ JobMeta.id;

            try
            {
                JobState = JobStates.SendInProgress;
                //создать отчет
               if(GetReport() == null)
                    return "Критическая ошибка генерации отчета";

                //выполнить отправку 
                result = WebUtil.SendReport<Report>(url, user, pass, "POST", readyReport, "Rep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"),id, reguestTimeOut);
                if (result != "")
                    return result;

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

        public virtual string GetFuncName() { throw new NotImplementedException(); }
        #endregion
        protected Report readyReport = null; //массив отчетов для отправки

        [DataMember]
        public Order order1C;
        [DataMember]
        public List<SelectedItem> selectedProduct = new List<SelectedItem>();
        [DataMember]
        public DateTime startTime;

        public TmplServerJob() : base()
        {
            meta.state = JobIcon.Default;
            //jobType = typeof(AcceptServerJob);
        }
        public virtual object GetReport() 
        {
            
           // if (typeof(T) != typeof(Report))
            //   throw new Exception("Неверно указан тип класса отчета. ожидается "+typeof(Report).ToString());

            //если задание завершено и отчет уже сформирован просто отдать ранее сохраненный отчет
            if ((JobState == JobStates.CloseAndAwaitSend) || (JobState == JobStates.Complited)
                || (JobState == JobStates.SendInProgress))
            {
                if (readyReport != null)
                    return readyReport;

                readyReport = Archive.RestoreReport<Report>(id);
                if (readyReport != null)
                    return readyReport;
            }
            return null;
        }
        public bool JobIsComplited()
        {
            if (JobState == JobStates.Complited)
                return true;

            return false;
        }

    }

    //тип определяет соответствие всем типа задиний для алгоритма возврата заданий по типу
    public class AllServerJob { }
}
