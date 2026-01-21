using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;

namespace FSerialization
{

    [DataContract]
    public class PalAggregateServerJob : TmplServerJob<PalAggregate1СOrder, PalAggerateReport, PalAggregateJob>
    {
        #region Реализация интерфейса BaseJob
        //public OrderMeta meta = new OrderMeta();

        [DataMember]
        public override  OrderMeta JobMeta
        {
            get
            {

                if ((meta.name == "" || meta.name == null) && order1C != null)
                    meta.name = "Cерия: " + order1C?.lotNo + "\n" + order1C?.productName+"\nЛиния: "+order1C.line+ "\n" + DateTime.Now.ToString("dd MMMM HH:mm");
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

        public override bool JobIsAwaible
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

                if (JobState == JobStates.InWork)
                    return true;

                if (JobState == JobStates.InWorkNoNum)
                    return true;

                return false;
            }
        }
        #endregion

        private ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();

        [DataMember]
        public List<UnitItem> Pallets = new List<UnitItem>();

        [DataMember]
        public decimal startBoxDiapazon = 0;
        [DataMember]
        public decimal stopBoxDiapazon = 0;


        [DataMember]
        public string urlLabelPalletTemplate = "http://l3/label/tb2.zpl";

        public PalAggregateServerJob() : base()
        {
            meta.state = JobIcon.Default;
            JobState = JobStates.Empty;
            jobType = typeof(PalAggregateServerJob);
        }

        public override string InitJob<T>(T order, string user, string pass)
        {
            PalAggregate1СOrder o = order as PalAggregate1СOrder;
            if (o == null)
                return "Wrong object type.Need PalAggregate1СOrder";

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
           //if (!WebUtil.DownLoadFile(o.urlLabelBoxTemplate, user, pass, fileName, "Box.tmpl"))
           //      return "Ошибка загрузки шаблона короба. url: " + o.urlLabelBoxTemplate;

            //загрузить файл шаблона палеты если он есть
            if (o.urlLabelPalletTemplate != "")
            {
                if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "Pallete.tmpl"))
                    return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
            }

            if(order1C.palletNumbers == null)
                return "тег palletNumbers не может быть пустым";

            //создать массив номеров
            for (int id = 0; id < order1C.palletNumbers.Count; id++)
                Pallets.Add(new UnitItem(order1C.palletNumbers[id],"", SelectedItem.ItemType.Паллета, CodeState.New, id));

            //вычислить диапазон номеров коробов
            decimal d = 0;
            startBoxDiapazon = Decimal.MaxValue;
            stopBoxDiapazon = 0;

            foreach (string b in order1C.boxNumbers)
            {
                try {
                    d = Convert.ToDecimal(b);

                    if (d < startBoxDiapazon)
                        startBoxDiapazon = d;

                    if (d > stopBoxDiapazon)
                        stopBoxDiapazon = d;

                } catch { return "Ошибка обработки номера короба:  " + b; }
            }

            return "";
        }

        public override object GetTsdJob()
        {
            if (JobState == JobStates.CloseAndAwaitSend)
            {
                return null;
            }

            PalAggregateJob sj = new PalAggregateJob();
            sj.id = id;
            sj.lotNo = order1C.lotNo;
            sj.JobMeta = JobMeta;
            sj.number = 0;
            sj.order1C = (PalAggregate1СOrder)order1C.Clone();
            sj.order1C.palletNumbers = new List<string>();
            sj.order1C.boxNumbers = new List<string>();
            sj.selectedItem = GetNextPallet();
         

            sj.boxLabelFields.AddRange(order1C.boxLabelFields);
            sj.palletLabelFields.AddRange(order1C.palletLabelFields);

            //sj.allreadyNum = 
            //посчитать количество коробов для приемки
            // foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            //{
            //    sj.number += pl.boxNumbers.Count;
            //} 
            
            if (sj.selectedItem == null && JobState == JobStates.InWork)
            {
                JobState = JobStates.InWorkNoNum;
            }else if(sj.selectedItem != null)//отметить задание как поступившее в работу
                JobState = JobStates.InWork;
            sj.JobState = JobState;

            return sj;
        }

        public override string ParceReport<T>(T rep)
        {
            try
            {
                UnitItem rj = rep as UnitItem;
                if (rj == null)
                    return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

                //создать отчет
                readyReport = new PalAggerateReport();
                readyReport.id = id;
                //readyReport.lotNo = order1C.lotNo;
                readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
                readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
                //readyReport.OperatorId = null;
                readyReport.OperatorId = rj.oId;
                //перекинуть паллеты

                //перекинуть все элементы по массивам
                foreach (UnitItem uI in Pallets)
                {
                    //определить типа вложенности объекта
                    if ((uI.tp == SelectedItem.ItemType.Паллета)&&(uI.st == CodeState.Verify))
                    {
                        PalAggerateReport.ReadyPallete rp = new PalAggerateReport.ReadyPallete();
                        rp.number = uI.num;
                        rp.operatorId = uI.oId;
                        rp.createTime = uI.dt.ToString("yyyy-MM-ddThh:mm:ssz");
                        foreach (UnitItem uL2 in uI.items)
                        {
                            /*
                            RepackReport.ReportBox rb = new RepackReport.ReportBox();
                            rb.boxNumber = uL2.num;
                            foreach (UnitItem uL3 in uL2.items)
                            {
                                rb.Numbers.Add(uL3.num);
                            }*/
                            rp.boxNumbers.Add(uL2.num);
                        }
                        readyReport.palletsNumbers.Add(rp);
                    }
                }

                //сохранить в архив 
                Archive.SaveReport<PalAggerateReport>(readyReport, id);
            }
            catch (Exception ex) { return ex.Message; }
            return "";
        }
        public override string GetFuncName() { return "Агрегация палеты"; }
        public void SetJobAsNew()
        {
            JobState = JobStates.New;
        }


        public bool CloseNotFullPalet(UnitItem p)
        {
            return true;
        }
        public PalletItem SetPaleteComplitedAndGetNext(UnitItem u )
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(3000))
            {
                try
                {
                    for (int i = 0; i < Pallets.Count; i++)
                    {
                        if (Pallets[i].num == u.num)
                        {
                            UnitItem p = Pallets[i];
                            p.oId = u.oId;
                            p.dt = u.dt;

                            p.items.AddRange(u.items);
                            p.st = CodeState.Verify;

                            if (i + 1 >= Pallets.Count)
                                return null;

                            if (Pallets[i + 1].st != CodeState.New)
                                return null;

                            int c = GetCountPocessedBox();
                            return new PalletItem(Pallets[i + 1], order1C.boxNumbers.Count - c);

                            //return new PalletItem( Pallets[i + 1]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //errInfo = ex.Message;
                    ex.ToString();
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            //errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;
        }
        public PalletItem GetNextPallet()
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(3000))
            {
                try
                {
                    for (int i = 0; i < Pallets.Count; i++)
                    {
                        if (Pallets[i].st == CodeState.New)
                        {
                            int c = GetCountPocessedBox();
                            return new PalletItem(Pallets[i], order1C.boxNumbers.Count - c);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //errInfo = ex.Message;
                    ex.ToString();
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            return null;
        }
        public UnitItem NumberAlreadyProcessed(string number)
        {
            foreach(UnitItem i in Pallets)
            {
                if (i.num == number)
                {
                    if (i.st == CodeState.Verify)
                        return i;
                    else
                        return null;
                }

                UnitItem u1 = i.CodeAlreadyInUnit(number);
                if (u1 != null)
                {
                    if (u1.st == CodeState.Verify)
                    {
                        u1.SetRoot(i);
                        u1.pN = i.num;
                        return u1;
                    }
                    else
                        return null;
                }
            }
            return null;// new UnitItem();
        }
        public UnitItem CheckBoxInOrder(string box)
        {
            for (int i = 0; i < order1C.boxNumbers?.Count; i++)
            {
                if (order1C.boxNumbers[i] == box)
                    return new UnitItem(box, SelectedItem.ItemType.Короб);
            }
            return null;
        }
        public bool RepackPallete(string num)
        {
            try {
                //найти палету
                foreach (UnitItem i in Pallets)
                {
                    if (i.num == num)
                    {
                        i.st = CodeState.New;
                        i.items.Clear();
                        return true;
                    }
                }

            } catch { }

            return false;
        }
        public int GetCountPocessedBox()
        {
            int counter = 0;
            foreach(UnitItem u in Pallets)
            {
                if (u.st == CodeState.Verify)
                    counter += u.items.Count;
            }

            return counter;
        }

    }
}
