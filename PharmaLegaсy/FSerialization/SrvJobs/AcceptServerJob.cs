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
    // public class AcceptServerJob : AggCorobBaseInfo, IBaseJob
    public class AcceptServerJob : TmplServerJob<InventJobOrder, AcceptReport, AcceptJob>
    {

        #region Реализация интерфейса BaseJob

        //смена статуса JobState= JobState.InWorck;
        //делается в функции которая вызывает эту
        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            AcceptJob sj = new AcceptJob();

            sj.order1C = order1C;
            sj.JobMeta = JobMeta;
            sj.number = 0;

            sj.productName = productName;
            sj.num = num;
            sj.lotNo = lotNo;
            sj.id = id;

            /*
            sj.id = id;
            sj.lotNo = lotNo;
            sj.gtin = gtin;
            sj.expDate = expDate;
            sj.addProdInfo = addProdInfo;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            sj.order1C = order1C;

            sj.productName = productName;
            sj.numРacksInBox = numРacksInBox;
            sj.numBoxInPallet = numBoxInPallet;
            sj.quantity = quantity;
            */
            //sj.allreadyNum = 
            /*
            //посчитать количество коробов для приемки
            foreach (FSerialization.UnitItem pl in order1C.palletsNumbers)
            {
                if()
                sj.number += pl.boxNumbers.Count;
            }*/

           //отметить задание как поступившее в работу
           JobState = JobStates.InWork;
            return sj;
        }
        public override string GetFuncName() { return "Приёмка"; }

        /*
        public string SendReports(string url, string user, string pass, bool partOfList)
        {
            string result = "Нет данных для отчета";

            if (JobState == JobState.SendInProgress)
                return "Отправка уже идет";

            try
            {
                JobState = JobState.SendInProgress;
                //создать отчет
                readyReport = CreateReportA();
                //выполнить отправку 
                result = WebUtil.SendReport<AcceptReport>(url, user, pass, "POST", readyReport, "AccRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));
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

                JobState = JobState.CloseAndAwaitSend;
            }
            return result;
        }*/
        #endregion


        [DataMember]
        public string productName;

        [DataMember]
        public int numBoxInPallet;

        [DataMember]
        public int num;

        public AcceptServerJob() : base()
        {
            meta.state = JobIcon.Default;
            jobType = typeof(AcceptServerJob);
        }

        public override string InitJob<T>(T order, string user, string pass)
        {
            string ErrorReason = "";
            Accept1СOrder o = order as Accept1СOrder;
            if (o == null)
                return "Wrong object type.Need Shipping1СOrder";

            //o.CheckContent();

            id = o.id;
            lotNo = o.lotNo;
            GTIN = o.gtin;
            //expDate = o.ExpDate;
            //addProdInfo = o.addProdInfo;

            productName = o.productName;
            numРacksInBox = o.numРacksInBox;
            numBoxInPallet = o.numBoxInPallet;
            num = o.num;

            startTime = DateTime.Now;
            JobState = JobStates.New;

            //создать задание и заполнить его
            order1C = new InventJobOrder();
            order1C.id = o.id;
            order1C.lotNo = o.lotNo;
            order1C.gtin = o.gtin;
            order1C.expDate = o.ExpDate;
            //order1C.addProdInfo = o.addProdInfo;

            order1C.numРacksInBox = o.numРacksInBox;
            order1C.numBoxInPallet = o.numBoxInPallet;

            order1C.productName = productName = o.productName;

            //пересоздать массив данных в более гибкую структуру
            foreach (Accept1СOrder.Pallets pl in o.palletsNumbers)
            {
                //если это список коробов без патеных номеров 
                if (pl.palletNumber == PalleteRezervCodes.PalletIDForUnpaletBox)
                {
                    foreach (string s in pl.boxNumbers)
                    {
                        UnitItem u2 = new UnitItem(s, SelectedItem.ItemType.Короб);
                        order1C.palletsNumbers.Add(u2);
                    }
                }
                else
                { //иначе добавить палету и короба
                    UnitItem u1 = new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета);
                    foreach (string s in pl.boxNumbers)
                    {
                        UnitItem u2 = new UnitItem(s, SelectedItem.ItemType.Короб);
                        u1.items.Add(u2);
                    }
                    order1C.palletsNumbers.Add(u1);
                }
            }


            return ErrorReason;
        }
     

        public override string ParceReport<T>(T rep)
        {
            try
            {
                AcceptJob rj = rep as AcceptJob;
                if (rj == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в службу поддержки";

                //создать отчет
                readyReport = new AcceptReport();
                readyReport.id = id;
                readyReport.lotNo = lotNo;

                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.OperatorId = rj.operators[0]?.id; 

                //перекинуть паллеты и пачки
                foreach (UnitItem it in rj.order1C.palletsNumbers)
                {
                    //обрабатываеем только верифицированные
                    if (it.st != CodeState.Verify)
                        continue;

                    switch (it.tp)
                    {
                        case SelectedItem.ItemType.Короб:
                        case SelectedItem.ItemType.Паллета:
                            readyReport.containerNumbers.Add(it.num);
                            break;
                        case SelectedItem.ItemType.Упаковка:
                            readyReport.Numbers.Add(it.num);
                            //readyReport.Numbers.Add("01"+it.gtin+"21"+it.quantity);
                            break;
                    }          
                }

                //сохранить в архив 
                Archive.SaveReport<AcceptReport>(readyReport, id);
            }
            catch (Exception ex) { return ex.Message; }
            return "";
        }
    }

   
}
