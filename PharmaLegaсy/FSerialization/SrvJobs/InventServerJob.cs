using System.Runtime.Serialization;
using Util;

namespace FSerialization
{
    [DataContract]
    public class InventServerJob : TmplServerJob<InventJobOrder, InventReport, InventJob>
    {
        //[DataMember]
        // public string urlLabelPalletTemplate = "http://host.ru/pal.tmpl";
        //
        [DataMember]
        private int num;
        [DataMember]
        private string productName;
       

        public InventServerJob() : base()
        {
            meta.state = JobIcon.Default;
            JobState = JobStates.Empty;
            jobType = typeof(InventServerJob);
        }

        public override string InitJob<T>(T order, string user, string pass)
        {
            string ErrorReason = "";
            Invent1СOrder o = order as Invent1СOrder;
            if (o == null)
                return "Wrong object type.Need Invent1СOrder";

            //o.CheckContent();

            id = o.id;
            lotNo = o.lotNo;
            //addProdInfo = o.addProdInfo;
            GTIN = o.gtin;
            ExpDate = o.ExpDate;


            startTime = DateTime.Now;
            JobState = JobStates.New;

            order1C = new InventJobOrder();
            order1C.id = o.id;
            order1C.lotNo = o.lotNo;
            order1C.gtin = o.gtin;
            order1C.expDate = o.ExpDate;
            //order1C.addProdInfo = o.addProdInfo;
            //order1C.JobMeta = JobMeta;
            //number = 0;
            //order1C.order1C = order1C;
            order1C.numРacksInBox = o.numРacksInBox;
            order1C.numBoxInPallet = o.numBoxInPallet;

            order1C.productName = productName = o.productName;

            num = o.num;
           
            //пересоздать массив данных в более гибкую структуру
            foreach (Invent1СOrder.Pallets pl in o.palletsNumbers)
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

        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            InventJob sj = new InventJob();
            //sj.order1C = new InventJobOrder();
            //sj.order1C.id = id;
            //sj.order1C.lotNo = lotNo;
            //sj.order1C.gtin = gtin;
            //sj.order1C.expDate = expDate;
            //sj.order1C.addProdInfo = addProdInfo;
            //sj.order1C.numРacksInBox = numРacksInBox;
            //sj.order1C.numBoxInPallet = numBoxInPallet;
            //sj.order1C.palletsNumbers.AddRange(order1C.palletsNumbers);

            sj.order1C = order1C;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            
            sj.productName = productName;      
            sj.num = num;
            sj.lotNo = lotNo;
            sj.id = id;

            //CreateTsdSqLiteDataFile();
            //sj.allreadyNum = 
            //посчитать количество коробов для приемки
            // foreach (Invent1СOrder.Pallets pl in order1C.palletsNumbers)
            //{
            //    sj.number += pl.boxNumbers.Count;
            //}

            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return sj;
        }
        public override object GetTsdSqLiteJob()
        {
            try
            {
                string storedPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\" + id + ".sl3";

                var data = File.ReadAllBytes(storedPath);
                return data;

            }
            catch (IOException ex) {
                Log.Write("GetTsdSqLiteJob" + ex.ToString());
            } catch (Exception ex)
            {
                Log.Write("GetTsdSqLiteJob" + ex.ToString());
            }
            return null;
        }

        public override string ParceReport<T>(T rep)
        {
            try
            {
                InventJob rj = rep as InventJob;
                if (rj == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

                //создать отчет
                readyReport = new InventReport();
                readyReport.id = id;
                readyReport.lotNo = order1C.lotNo;
                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.OperatorId = rj.operators[0]?.id;

                //перекинуть паллеты

                //перекинуть все элементы по массивам
                foreach (UnitItem uI in rj.order1C.palletsNumbers)
                {
                    if (uI.st != CodeState.Verify)
                        continue;

                    switch (uI.tp)
                    {
                        case SelectedItem.ItemType.Короб:
                        case SelectedItem.ItemType.Паллета:
                            readyReport.containerNumbers.Add(uI.num);
                            break;
                        case SelectedItem.ItemType.Упаковка:
                            readyReport.Numbers.Add(uI.num);
                            break;
                    }
                    #region old
                    /*
                    //определить типа вложенности объекта
                    if (uI.tp == SelectedItem.ItemType.Короб)
                    {
                        readyReport.AddBoxToZeroPallete(uI);
                    }
                    else if (uI.tp == SelectedItem.ItemType.Паллета)
                    {
                        RepackReport.ReportPallet rp = new RepackReport.ReportPallet();
                        rp.palletNumber = uI.quantity;
                        foreach (UnitItem uL2 in uI.items)
                        {
                            RepackReport.ReportBox rb = new RepackReport.ReportBox();
                            rb.boxNumber = uL2.quantity;
                            foreach (UnitItem uL3 in uL2.items)
                            {
                                rb.Numbers.Add(uL3.quantity);
                            }
                            rp.boxNumbers.Add(rb);
                        }
                        readyReport.palletsNumbers.Add(rp);
                    }*/
                    #endregion
                }
                //сохранить в архив 
                Archive.SaveReport<InventReport>(readyReport, id);
            }
            catch (Exception ex) { return ex.Message; }
            return "";
        }

        public override string GetFuncName() { return "Инвентаризация"; }
    }
}
