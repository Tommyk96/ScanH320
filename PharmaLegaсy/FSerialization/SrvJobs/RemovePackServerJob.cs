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
    [DataContract]
    public class RemoveServerJob : TmplServerJob<Remove1СOrder, RemoveReport, RemovePackJob>
    {
        public RemoveServerJob() : base()
        {
            meta.state = JobIcon.Default;
            JobState = JobStates.Empty;
            jobType = typeof(RemoveServerJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            Remove1СOrder o = order as Remove1СOrder;
            if (o == null)
                return "Wrong object type. Need Remove1СOrder";

            string ErrorReason = "";

            //создать задачу свервера
            order1C = o;
            id = o.id;
            lotNo = o.lotNo;
            startTime = DateTime.Now;
            JobState = JobStates.New;

            return ErrorReason;

        }
        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            RemovePackJob sj = new RemovePackJob();
            sj.id = id;
            sj.lotNo = order1C.lotNo;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            sj.order1C = order1C;
            //sj.allreadyNum = 
            //посчитать количество коробов для приемки
            foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            {
                sj.number += pl.boxNumbers.Count;
            }
            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return sj;
        }
        public override string ParceReport<T>(T rep) 
        {
            try {
                RemovePackJob rj = rep as RemovePackJob;
                if (rj == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

                //создать отчет
                readyReport = new RemoveReport();
                readyReport.id = id;
                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.OperatorId = rj.operators[0]?.id;

                //перекинуть все элементы по массивам
                foreach (SelectedItem it in rj.SelectedItems)
                {
                    switch (it.type)
                    {
                        case SelectedItem.ItemType.Упаковка:
                            if (it.st == CodeState.Verify)
                                readyReport.deleteNumbers.Add(it.fullNumber);
                            else if (it.st == CodeState.Moved)
                            {
                                RemoveReport.MovedNumbers mNum = new RemoveReport.MovedNumbers();
                                mNum.box = it.contNum;
                                mNum.number = it.fullNumber;
                                readyReport.moveNumbers.Add(mNum);
                            }
                            break;
                        case SelectedItem.ItemType.Паллета:
                            readyReport.deletePallets.Add(it.fullNumber);
                            break;
                        case SelectedItem.ItemType.Короб:
                            readyReport.deleteBox.Add(it.fullNumber);
                            break;                        
                    }
                }

                //пометить задание как отработанное
                JobState = JobStates.CloseAndAwaitSend;

                //сохранить в архив 
                Archive.SaveReport<RemoveReport>(readyReport, id);

            } catch (Exception ex) { return ex.Message; }
            return "";
        }

        public override string GetFuncName() { return "Списание"; }

    }
}
