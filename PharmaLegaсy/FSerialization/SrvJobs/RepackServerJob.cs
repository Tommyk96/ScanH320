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
    public class RepackServerJob : TmplServerJob<Repack1СOrder, RepackReport, RepackJob>
    {
        [DataMember]
        public string urlLabelPalletTemplate = "http://l3/label/tb2.zpl";

        public RepackServerJob() : base()
        {
            meta.state = JobIcon.Default;
            JobState = JobStates.Empty;
            jobType = typeof(RepackServerJob);
        }

        public override string InitJob<T>(T order, string user, string pass)
        {
            Repack1СOrder o = order as Repack1СOrder;
            if (o == null)
                return "Wrong object type.Need Repack1СOrder";

           // o.CheckContent();

            //создать задачу свервера

            order1C = o;

            id = o.id;
            lotNo = o.lotNo;
            startTime = DateTime.Now;
            JobState = JobStates.New;
           

            //сохранить и загрузить шаблоны
            string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

            //загрузить файл шаблона коробки
            if (!WebUtil.DownLoadFile(o.urlLabelBoxTemplate, user, pass, fileName, "Box.tmpl"))
                return "Ошибка загрузки шаблона короба. url: " + o.urlLabelBoxTemplate;

            //загрузить файл шаблона палеты если он есть
            if (o.urlLabelPalletTemplate != "")
            {
                if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "Pallete.tmpl"))
                    return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
            }

            return "";
        }

        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            RepackJob sj = new RepackJob();
            sj.id = id;
            sj.lotNo = order1C.lotNo;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            sj.order1C = order1C;
            sj.boxLabelFields.AddRange(order1C.boxLabelFields);
            sj.palletLabelFields.AddRange(order1C.palletLabelFields);

            //sj.allreadyNum = 
            //посчитать количество коробов для приемки
            // foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            //{
            //    sj.number += pl.boxNumbers.Count;
            //}
            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return sj;
        }

        public override string ParceReport<T>(T rep)
        {
            try
            {
                RepackJob rj = rep as RepackJob;
                if (rj == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

                //создать отчет
                readyReport = new RepackReport();
                readyReport.id = id;
                readyReport.lotNo = order1C.lotNo;
                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.OperatorId = rj.operators[0]?.id;

                //перекинуть паллеты

                //перекинуть все элементы по массивам
                foreach (UnitItem uI in rj.repackPalets)
                {
                    //определить типа вложенности объекта
                    if (uI.tp == SelectedItem.ItemType.Короб)
                    {
                        readyReport.AddBoxToZeroPallete(uI);
                    }
                    else if (uI.tp == SelectedItem.ItemType.Паллета)
                    {
                        RepackReport.ReportPallet rp = new RepackReport.ReportPallet();
                        rp.palletNumber = uI.num;
                        foreach (UnitItem uL2 in uI.items)
                        {
                            RepackReport.ReportBox rb = new RepackReport.ReportBox();
                            rb.boxNumber = uL2.num;
                            foreach (UnitItem uL3 in uL2.items)
                            {
                                rb.Numbers.Add(uL3.num);
                            }
                            rp.boxNumbers.Add(rb);
                        }
                        readyReport.palletsNumbers.Add(rp);
                    }
                }

                //сохранить в архив 
                Archive.SaveReport<RepackReport>(readyReport, id);
            }
            catch (Exception ex) { return ex.Message; }
            return "";
        }
        public override string GetFuncName() { return "Переупаковка"; }
        public void SetJobAsNew()
        {
            JobState = JobStates.New;
        }
    }
}