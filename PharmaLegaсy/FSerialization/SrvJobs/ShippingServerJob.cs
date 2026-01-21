using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace FSerialization
{
    //класс задания выполняющегося на Сервере

    [DataContract]
    public class ShippingServerJob : TmplServerJob<Shipping1СOrder, SalesReport, ShippingJob>//AggCorobBaseInfo, IBaseJob
    {
        private ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        // private DatabaseContext context;

        #region Реализация интерфейса BaseJob
       
        [DataMember]
        public override OrderMeta JobMeta
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
                    meta.name = "Накл: " + invoiceNum + "\n" + order1C?.customer + "\n" + order1C.createDateTime;//.ToString("dd MMMM HH:mm");
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
        public override object GetTsdJob()
        {
            ShippingJob sj = new ShippingJob();
            sj.id = id;
            sj.invoise = order1C.invoiceNum;
            sj.customer = order1C.customer;
            sj.JobMeta = JobMeta;
            sj.product.AddRange(shipSelectedProduct);
            sj.numLabelAtPallete = order1C.numLabelAtPallet;
            /*
            //перекинуть позиции наименования товара
            foreach (Shipping1СOrder.Product pr in order1C.product)
            {
                  sj.product.Add(new ShippingJob.Product(pr.productName, pr.quantity,pr.gtin));
            }*/
            //отметить задание как поступившее в работу
            JobState = JobStates.InWork;
            return sj;
        }
        public override string ParceReport<T>(T rep)
        {
            ShippingQuery rj = rep as ShippingQuery;
            if (rj == null)
                return "При формировании отчета произошла критическая ошибка. Обратитесь в лужбу поддержки";

            readyReport = new SalesReport();
            readyReport.id = id;
            readyReport.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
            readyReport.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
            readyReport.OperatorId =   operatorId;
            //создать отчет
            bool bFound = false;
            //проход по всем продуктам
            foreach (ShippingJob.Product spr in shipSelectedProduct)
            {
                //UnitItemM r = new UnitItemM(gtin, (int)tp, num);
                foreach (UnitItem ui in spr.SelectedItems)
                {
                    //если елемент не числится на палете закинуть в масив иначе искать палету и кидать туда
                    if ((ui.pN == "") || (ui.pN == null))
                        readyReport.Items.Add(ui.GetUnitItemM(spr.gtin));
                    else
                    {
                        bFound = false;
                        //проход по всем уже обработанным позициям в поисках палеты
                        foreach (UnitItemM ri in readyReport.Items)
                        {
                            //если палета найдена добавить елемент в нее
                            if (ri.num == ui.pN)
                            {
                                ri.items.Add(ui.GetUnitItemM(spr.gtin));
                                bFound = true;
                                break;
                            }
                        }
                        //если палета не найдена . создать и добавить ее
                        if (!bFound)
                        {

                            //определить тип контейнера.
                            //если тип добавляемого короб. объявить контейнер палетой. иначе коробом
                            UnitItemM nPal = null;
                            if(ui.tp == SelectedItem.ItemType.Короб)
                                 nPal = new UnitItemM("", (int)SelectedItem.ItemType.Паллета, ui.pN);
                            else
                                 nPal = new UnitItemM("", (int)SelectedItem.ItemType.Короб, ui.pN);

                            nPal.items.Add(ui.GetUnitItemM(spr.gtin));
                            readyReport.Items.Add(nPal);
                        }
                    }
                }
            }

            //сохранить в архив 
            Archive.SaveReport<SalesReport>(readyReport, id);
            return "";
        }
        public override string GetFuncName() { return "Отгрузка"; }
        #endregion


        [DataMember]
        public string invoiceNum;
        [DataMember]
        public string operatorId;

        [DataMember]
        public List<ShippingJob.Product> shipSelectedProduct = new List<ShippingJob.Product>();

        //private SQLiteConnection db;

        public ShippingServerJob() : base()
        {
            meta.state = JobIcon.Default;
            jobType = typeof(ShippingServerJob);
        }
        public override string InitJob<T>(T order, string user, string pass)
        {
            Shipping1СOrder o = order as Shipping1СOrder;
            if (o == null)
                return "Wrong object type. Need Shipping1СOrder";


            //создать задачу свервера
            order1C = o;
            id = o.id;
            invoiceNum = o.invoiceNum;
            startTime = DateTime.Now;
            JobState = JobStates.New;
            ExpDate = DateTime.MinValue.ToShortDateString(); 
            //сохранить и загрузить шаблоны
            string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

            if (!Directory.Exists(fileName))
                Directory.CreateDirectory(fileName);

            //загрузить файл шаблона сборной палеты если он есть
            if (o.urlLabelPalletTemplate != "")
            {
                if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "Pallete.tmpl"))
                    return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
            }


            int idCounter = 0;
            foreach (Shipping1СOrder.Product pr in o.product)
            {
                //загрузить шаблон етикетки палеты
                //загрузить файл шаблона палеты если он есть
               /* if (pr.urlLabelPalletTemplate != "")
                {
                    if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, pr.gtin + ".tmpl"))
                        return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
                }*/

                pr.id = idCounter.ToString();
                ShippingJob.Product npr = new ShippingJob.Product(pr.id, pr.productName, pr.quantity, pr.GTIN, pr.lotNo, "tnvd", pr.ExpDate
                    , "Pallete.tmpl");//pr.gtin + ".tmpl");
                shipSelectedProduct.Add(npr);
                idCounter++;
            }
            return "";
        }
        public ShippingJob.AddItemAnswer ProssedNumber(string fullNumber, string palNumber, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(5000))
            {
                try
                {
                    //Thread.Sleep(100000);
                    errInfo = "Код не найден в задании";
                    //проверить ссцц или серийный номер если их нет выдать ошибку
                    Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

                    UnitItem item = new UnitItem("", SelectedItem.ItemType.Неизвестно);
                    //присвоить номер палеты на который его планируется положить
                    item.pN = palNumber;

                    //обработка номера коробочки(третичная упаковка)
                    if (ld.SerialNumber != null)
                    {
                        item.num = ld.SerialNumber;
                        item.gtin = ld.GTIN;
                        //выбрать все продукты с такими параметрами в задании
                        List<Shipping1СOrder.Product> orderProducts = order1C.product.FindAll((x)=> x.VerifyProductNum(ld)=="");
                        //если продукт не найден вернуть ошибку
                        if(orderProducts.Count <= 0 )
                            throw new Exception("Продукт отсутствует в задании!");

                        foreach (Shipping1СOrder.Product pr in orderProducts)
                        {
                            item.qP = 1;
                            item.tp = SelectedItem.ItemType.Упаковка;
                            item.oId = pr.id;
                            item.st = CodeState.Verify;

                            //проверить переполнение если продукт полон перейти к следующему
                            ShippingJob.Product spr = shipSelectedProduct.First((x) => x.id == pr.id);
                            if (spr.number <= spr.allreadyNum)
                                continue;

                            break;
                        }
                    }
                    //обработка номера короба или палеты
                    else if (ld.SerialShippingContainerCode00 != null)
                    {
                        item.num = ld.SerialShippingContainerCode00;

                        //найти номер третичной упаковки в задании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            //уровень палеты
                            foreach (Shipping1СOrder.Pallets pl in pr.palletsNumbers)
                            {
                                if (item.num == pl.palletNumber)
                                {
                                    //если предпринята попытка добавить на палету код другой палеты
                                    //выдать ошибку
                                    if (palNumber != null)
                                        throw new Exception("Нельзя добавлять одну палету в другую");

                                    item.tp = SelectedItem.ItemType.Паллета;
                                    item.qP = pr.numРacksInBox * pl.boxNumbers.Count;
                                    item.oId = pr.id;
                                    item.st = CodeState.Verify;
                                    item.gtin = pr.GTIN;
                                    //уровень короба
                                    foreach (string boxNum in pl.boxNumbers)
                                        item.items.Add(new UnitItem(boxNum, SelectedItem.ItemType.Короб));

                                    //уровень короба
                                    //уровень неполных коробов 
                                    if (pl?.NotComplBox != null)
                                    {
                                        foreach (Shipping1СOrder.notCompleteBoxItem boxNum in pl.NotComplBox)
                                        {
                                            item.items.Add(new UnitItem(boxNum.num, SelectedItem.ItemType.Короб, boxNum.quantity));
                                            item.qP += boxNum.quantity;
                                        }
                                    }

                                    break;
                                }

                                //уровень короба
                                foreach (string boxNum in pl.boxNumbers)
                                {
                                    if (item.num == boxNum)
                                    {
                                        //если идет сборка палеты проверить корректность GTIN. на одной палете должен быть только 1 тип GTIN
                                        //if (!VerifyGtinInNewContainer(palNumber, pr.gtin))
                                        //    throw new Exception("Продукт не соответствует контейнеру!\nВ одном контейнере могут быть продукты только одинакового GTIN");

                                        item.tp = SelectedItem.ItemType.Короб;
                                        item.qP = pr.numРacksInBox;
                                        item.st = CodeState.Verify;
                                        item.oId = pr.id;
                                        item.gtin = pr.GTIN;
                                        item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                        break;
                                    }
                                }
                                //уровень неполных коробов 
                                if (pl?.NotComplBox != null)
                                {
                                    foreach (Shipping1СOrder.notCompleteBoxItem boxNum in pl.NotComplBox)
                                    {
                                        if (item.num == boxNum.num)
                                        {
                                            //если идет сборка палеты проверить корректность GTIN. на одной палете должен быть только 1 тип GTIN
                                            //if(!VerifyGtinInNewContainer(palNumber, pr.gtin))
                                            //    throw new Exception("Продукт не соответствует контейнеру!\nВ одном контейнере могут быть продукты только одинакового GTIN");
                                            //
                                            item.tp = SelectedItem.ItemType.Короб;
                                            item.st = CodeState.Verify;
                                            item.oId = pr.id;
                                            item.qP = boxNum.quantity;
                                            item.gtin = pr.GTIN;
                                            item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                            break;
                                        }
                                    }
                                }

                                //если элемент найден закончить цыкл
                                if (item.tp != SelectedItem.ItemType.Неизвестно)
                                    break;
                            }
                            //если элемент найден закончить цыкл
                            if (item.tp != SelectedItem.ItemType.Неизвестно)
                                break;

                            //если не найден в основном проверить  в массиве неполных
                            foreach (Shipping1СOrder.notCompleteBoxItem ncbi in pr.notCompleteBoxNumbers)
                            {
                                if (item.num == ncbi.num)
                                {
                                    //если идет сборка палеты проверить корректность GTIN. на одной палете должен быть только 1 тип GTIN
                                    //if (!VerifyGtinInNewContainer(palNumber, pr.gtin))
                                    //    throw new Exception("Продукт не соответствует контейнеру!\nВ одном контейнере могут быть продукты только одинакового GTIN");

                                    item.tp = SelectedItem.ItemType.Короб;
                                    item.qP = ncbi.quantity;
                                    item.st = CodeState.Verify;
                                    item.oId = pr.id;
                                    item.gtin = pr.GTIN;
                                    //item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                    break;
                                }
                            }

                            //если элемент найден закончить цыкл
                            if (item.tp != SelectedItem.ItemType.Неизвестно)
                                break;
                        }

                    }
                    else
                        throw new Exception("Номер не распознан!");

                    //если элемент не найден закончить цыкл
                    if (item.tp == SelectedItem.ItemType.Неизвестно)
                        throw new Exception("Номер не найден в задании!");

                    //выбрать все продукты с такими параметрами в задании
                    List<ShippingJob.Product> selectedProducts = shipSelectedProduct.FindAll((x) => x.gtin == item.gtin);

                    ShippingJob.Product prodInc = null;
                    //проверить что номер  еще не обработался
                    foreach (ShippingJob.Product spr in selectedProducts)
                    {
                        #region проверить добавлен ли уже продукт?
                        //проверить что такой код ещ ене добавлен
                        foreach (UnitItem it in spr.SelectedItems)
                        {
                            if (it.num == item.num)
                            {
                                //если такой номер уже есть вернуть GTIN для номера и признак
                                errInfo = "ALRNUM*" + spr.gtin + "*";
                                return null;
                            }

                            //проверить не вложен ли номер внутри уже обработанных контейнеров
                            if (it.num == item.GetRoot()?.num)
                            {
                                throw new Exception("Номер уже обработан в составе контейнера : " + item.GetRoot().num);
                                //errInfo = "ALRINCL*" + spr.gtin + "*"+ item.rootContainerNumber+"*";
                                //return null;
                            }
                        }

                        //если ето номер палеты то проверить не числится ли какойто номер из вложенных как обработанный
                        foreach (UnitItem it in spr.SelectedItems)
                        {
                            if (it.GetRoot()?.num == item.num)
                            {
                                throw new Exception("Часть контейнера уже обработана! Контейнер не может быть обработан как целый.   Обработано:" + it.tp.ToString() + "              №:" + it.num);
                            }
                        }
                        #endregion

                        //если продукт не определен то определить его
                        if (item.oId == spr.id)
                            prodInc = spr;
                    }

                  


                    //все проверки пройдены. добавить номер 
                    if (prodInc != null)//item.oId == spr.id)
                    {
                        //продукт найден выполняем требуемое
                        switch (type)
                        {
                            case ShippingQueryType.Add:
                                //если идет сборка палеты проверить корректность GTIN. на одной палете должен быть только 1 тип GTIN
                                if (!VerifyGtinInNewContainer(palNumber, item.gtin))
                                    throw new Exception("Продукт не соответствует контейнеру!   В одном контейнере могут быть продукты только одинакового GTIN");

                                //сравнить добовляющееся количество чтоб оно не превышало 
                                //нужное
                                if (prodInc.number < (prodInc.allreadyNum + item.GetItemsQuantity()))
                                    throw new Exception("Количество добавляемого продукта превышает необходимое!");

                                //добавить в отобранные
                                /*ShippingJob.Item sit = new ShippingJob.Item(SelectedItem.ItemType.Неизвестно, 0, "");
                                sit.fullNumber = item.quantity;
                                sit.type = item.tp;
                                sit.rootContainerNumber = item.GetRoot()?.quantity;
                                sit.numРacks = 1;
                                sit.numberItemInPack = item.GetItemsQuantity();
                                sit.productId = item.oId;*/
                                UnitItem sit = item.Clone();
                                sit.qIp = item.GetItemsQuantity();
                                item.items.Clear();


                                if (prodInc.Add(sit))
                                {
                                    SafeToDisk();
                                    ShippingJob.AddItemAnswer answer = new ShippingJob.AddItemAnswer();
                                    answer.products.Add(prodInc);
                                    //answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN);
                                    answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN,
                                        out answer.curentPalettePackCount,
                                        out answer.curentPaletteBoxCount,
                                        out answer.paletteProductGtin);


                                    return answer;
                                }
                                else
                                    return null;
                            //break;
                            default:
                                errInfo = "Тип добавляемого элемента имеет неверный метод: " + type.ToString();
                                return null;
                        }
                    }

                    errInfo = "Не найден ID продукта :" + item.oId.ToString();
                    return null;

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }
        private ShippingJob.AddItemAnswer AddItemToProduct(ShippingJob.Product spr, UnitItem item)
        {
            //сравнить добовляющееся количество чтоб оно не превышало 
            //нужное
            if (spr.number < (spr.allreadyNum + item.GetItemsQuantity()))
                throw new Exception("Количество добавляемого продукта превышает необходимое!");

            //добавить в отобранные
            /*ShippingJob.Item sit = new ShippingJob.Item(SelectedItem.ItemType.Неизвестно, 0, "");
            sit.fullNumber = item.quantity;
            sit.type = item.tp;
            sit.rootContainerNumber = item.GetRoot()?.quantity;
            sit.numРacks = 1;
            sit.numberItemInPack = item.GetItemsQuantity();
            sit.productId = item.oId;*/
            UnitItem sit = item.Clone();
            sit.qIp = item.GetItemsQuantity();
            item.items.Clear();


            if (spr.Add(sit))
            {
                SafeToDisk();
                ShippingJob.AddItemAnswer answer = new ShippingJob.AddItemAnswer();
                answer.products.Add(spr);
                //answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN);
                answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN,
                    out answer.curentPalettePackCount,
                    out answer.curentPaletteBoxCount,
                    out answer.paletteProductGtin);


                return answer;
            }
            else
                return null;
        }
        public int GetItemsCountAtPalette(string number, out int PackCount, out int BoxCount, out List<string> _gtins)
        {
            PackCount = 0;
            BoxCount = 0;
            _gtins = new List<string>();

            if (number == null)
                return 0;

            if (number == "")
                return 0;

            int result = 0;
            try
            {
                //если элемент не найден закончить цыкл
                if (number == " ")
                    throw new Exception("Номер не распознан!");


                //пройти по всем продуктам чтоб найти все элементы на палете
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    foreach (UnitItem it in spr.SelectedItems)
                    {
                        if (it.pN == number)
                        {
                            result++;

                            if (it.tp == SelectedItem.ItemType.Упаковка)
                                PackCount++;
                            else if (it.tp == SelectedItem.ItemType.Короб)
                                BoxCount++;

                            //добавить GTIN продукта на палету
                            string _g = _gtins.FindLast(x => x == number);
                            if (_g != number)
                                _gtins.Add(spr.gtin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();

            }
            finally
            {

            }
            return result;
        }
        public PaleteLadelData GetPaleteLabelData(string number)
        {
            PaleteLadelData data = new PaleteLadelData();
            bool bFound = false;
            try
            {
                //проходж по всем отобранным продуктам
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    //проход по всем отобранным елементам в продукте
                    foreach (UnitItem it in spr.SelectedItems)
                    {
                        if (it.pN == number)
                        {
                            //счетчики пачек и коробов на палете
                            if (it.tp == SelectedItem.ItemType.Упаковка)
                                data.curentPalettePackCount++;
                            else
                                data.curentPaletteBoxCount++;


                            bFound = false;
                            //найти такой продукт в массиве
                            foreach (PaleteLadelData.ProductInfo pi in data.products)
                            {
                                if (pi.gtin != spr.gtin)
                                    continue;

                                if (pi?.lotNo != spr?.lotNo)
                                    continue;

                                pi.quantity += it.qIp;
                                bFound = true;
                            }

                            if (bFound)
                                continue;

                            //добавить продукт раз его нет
                            PaleteLadelData.ProductInfo pri = new PaleteLadelData.ProductInfo();
                            pri.gtin = spr.gtin;
                            pri.lotNo = spr?.lotNo;
                            pri.expDate = spr?.expDate;
                            //pri.addProdInfo = spr?.addProdInfo;
                            pri.quantity = it.qIp;
                            pri.name = spr.name;

                            data.products.Add(pri);
                        }
                    }
                }
                data.sscc = number;
                return data;


            }
            catch (Exception ex) { ex.ToString(); }
            return null;
        }
        public string GetPaleteLabel(string palNum)
        {
            string fileName = System.IO.Path.GetDirectoryName(
                           System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id;

            List<string> _gtins = new List<string>();
            //если элемент не найден закончить цыкл
            if (palNum == " ")
                throw new Exception("Номер не распознан!");

            //всегда использолвать мулти палету
            return fileName + "\\mPallete.tmpl";

            //отключено потому что великий сказал нахуй ненадо. правда логисты с етим не согласны. ну  что уж тут.
            //try
            //{

            //    //пройти по всем продуктам чтоб найти все элементы на палете
            //    foreach (ShippingJob.Product spr in shipSelectedProduct)
            //    {
            //        foreach (UnitItem it in spr.SelectedItems)
            //        {
            //            if (it.pN == palNum)
            //            {
            //                //добавить GTIN продукта на палету
            //                string _g = _gtins.FindLast(x => x == spr.gtin);
            //                if (_g != spr.gtin)
            //                    _gtins.Add(spr.gtin);
            //            }
            //        }
            //    }

            //    if (_gtins.Count > 1)
            //    {
            //        return fileName + "\\mPallete.tmpl";
            //    }
            //    else
            //        return fileName + "\\" + _gtins[0] + ".tmpl";

            //}
            //catch (Exception ex)
            //{
            //    ex.ToString();

            //}
            //finally
            //{

            //}
            //return "";
        }
        public ShippingJob.AddItemAnswer ClosePalete(string palNumber, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(5000))
            {
                errInfo = "";
                ShippingJob.AddItemAnswer answer = new ShippingJob.AddItemAnswer();
                bool bUpdate = false;
                try
                {

                    switch (type)
                    {
                        case ShippingQueryType.CloseBox: 
                            //пройти по всем отобранным позициям 
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                UnitItem p = spr.SelectedItems.Find(x => x.num == palNumber);
                                if (p != default(UnitItem))
                                {
                                    p.tp = SelectedItem.ItemType.Короб;
                                    return answer;
                                }
                            }
                        return answer;
                        case ShippingQueryType.ClosePalete:
                            //закрываем палету . ниче не делаем просто возврашаем ок
                            //пройти по всем отобранным позициям 
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                UnitItem p = spr.SelectedItems.Find(x => x.num == palNumber);
                                if (p != default(UnitItem))
                                {
                                    p.tp = SelectedItem.ItemType.Паллета;
                                    return answer;
                                }

                            }
                            return answer;
                        case ShippingQueryType.DropPaleteDropContent:
                            //пройти по всем отобранным позициям 
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                //удалить отобранные позиции из отобранного
                                if (spr.SelectedItems.RemoveAll(t => t.pN == palNumber) > 0)
                                {
                                    //пересчитать счетчики отобранного
                                    spr.RefreshCounters();
                                    //добавить в ответ 
                                    answer.products.Add(spr);
                                }
                            }
                            break;
                        case ShippingQueryType.DropPaleteSafeContent:
                            //пройти по всем отобранным продуктам
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                bUpdate = false;
                                //проверить что такой код ещ ене добавлен
                                foreach (UnitItem it in spr.SelectedItems)
                                {
                                    if (it.pN != palNumber)
                                        continue;

                                    it.pN = null;
                                    bUpdate = true;
                                }

                                if (bUpdate)//добавить в ответ 
                                    answer.products.Add(spr);
                            }
                            break;
                        default:
                            errInfo = "ClosePalete Неверный тип запроса: " + type.ToString();
                            return null;
                    }
                    return answer;

                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;
        }
        private bool VerifyGtinInNewContainer(string palNumber,string gtin)
        {
            if (string.IsNullOrEmpty(palNumber))
                return true;


            //пройти по всем отобранным позициям 
            foreach (ShippingJob.Product spr in shipSelectedProduct)
            {
                //удалить отобранные позиции из отобранного
                UnitItem et = spr.SelectedItems.Find(t => t.pN == palNumber);
                if (et != default(UnitItem))
                {
                    if (gtin != spr.gtin)
                        return false;  
                    else
                        return true;
                }
            }

            return true;
        }
        /*
        public ShippingJob.Product ProssedNumber(string fullNumber, ShippingQueryType type, out string errInfo)
        {
            if(_rw == null)
                  _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(500))
            {

                try
                {
                    errInfo = "Код не найден в задании";
                    //проверить ссцц или серийный номер если их нет выдать ошибку
                    Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

                    ShippingJob.Item item = new ShippingJob.Item(ShippingJob.ItemType.Неизвестно, 0, "");

                    if (ld.SerialNumber != null)
                    {
                        item.fullNumber = ld.SerialNumber;
                        //проверить есть ли такой GTIN в задлании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            if (pr.VerifyProductNum(fullNumber))
                            {
                                item.numРacks = 1;
                                item.type = ShippingJob.ItemType.Упаковка;
                                item.productId = pr.id;
                                break;
                            }
                        }
                    }
                    else if (ld.SerialShippingContainerCode00 != null)
                    {
                        item.fullNumber = ld.SerialShippingContainerCode00;

                        //найти номер третичной упаковки в задании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            //уровенить палеты
                            foreach (Shipping1СOrder.Pallets pl in pr.palletsNumbers)
                            {
                                if (item.fullNumber == pl.palletNumber)
                                {
                                    item.type = ShippingJob.ItemType.Паллета;
                                    item.numРacks = pl.boxNumbers.Count;
                                    item.numberItemInPack = pr.numРacksInBox;
                                    item.productId = pr.id;
                                    break;
                                }
                                //уровень короба
                                foreach (string boxNum in pl.boxNumbers)
                                {
                                    if (item.fullNumber == boxNum)
                                    {
                                        item.type = ShippingJob.ItemType.Короб;
                                        item.numРacks = 1;
                                        item.numberItemInPack = pr.numРacksInBox;
                                        item.productId = pr.id;
                                        item.rootContainerNumber = pl.palletNumber;
                                        break;
                                    }
                                }
                                //если элемент найден закончить цыкл
                                if (item.type != ShippingJob.ItemType.Неизвестно)
                                    break;

                            }
                            //если элемент найден закончить цыкл
                            if (item.type != ShippingJob.ItemType.Неизвестно)
                                break;
                        }

                    }
                    else
                        throw new Exception("Номер не распознан!");

                    //если элемент не найден закончить цыкл
                    if (item.type == ShippingJob.ItemType.Неизвестно)
                        throw new Exception("Номер не найден в задании!");

                    //проверить что номер  еще не обработался
                    foreach (ShippingJob.Product spr in shipSelectedProduct)
                    {
                        if (item.productId == spr.id)
                        {
                            //продукт найден выполняем требуемое
                            switch (type)
                            {
                                case ShippingQueryType.Add:
                                    //проверить что такой код ещ ене добавлен
                                    foreach (ShippingJob.Item it in spr.SelectedItems)
                                    {
                                        if (it.fullNumber == item.fullNumber)
                                        {
                                            //если такой номер уже есть вернуть GTIN для номера и признак
                                            errInfo = "ALRNUM*"+spr.gtin+"*";
                                            return null;
                                            
                                        }
                                        //проверить не вложен ли номер внутри уже обработанных контейнеров
                                        if (it.fullNumber == item.rootContainerNumber)
                                        {
                                            throw new Exception("Номер уже обработан в составе контейнера : "+item.rootContainerNumber);
                                            //errInfo = "ALRINCL*" + spr.gtin + "*"+ item.rootContainerNumber+"*";
                                            //return null;
                                        }
                                    }

                                    //сравнить добовляющееся количество чтоб оно не превышало 
                                    //нужное
                                    if (spr.number < (spr.allreadyNum + item.GetItemsQuantity()))
                                        throw new Exception("Количество добавляемого продукта превышает необходимое!");

                                    if (spr.Add(item))
                                        return spr;
                                    else
                                        return null;

                                    break;
                            }

                        }
                    }

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }
        */
        public ShippingJob.Product DeleteNumber(string number, string gtin, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(500))
            {
                try
                {
                    errInfo = "Код не найден в задании";


                    //если элемент не найден закончить цыкл
                    if (number == " ")
                        throw new Exception("Номер не распознан!");

                    //найти номер в обработанных
                    foreach (ShippingJob.Product spr in shipSelectedProduct)
                    {
                        if (spr.gtin == gtin)
                        {
                            UnitItem itm = spr.GetItemAtNum(number);
                            if (itm != null)
                            {
                                spr.Remove(itm);
                                return spr;
                            }
                        }
                    }

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }

        #region SqlQuery
        //private string ProductTableCreate = @"CREATE TABLE [ProductSqlA] (
        //  [productId] INTEGER NOT NULL
        //, [id] TEXT NOT NULL
        //, [gtin] NVARCHAR(20) NOT NULL
        //, [addProdInfo] TEXT NOT NULL
        //, [lotNo] TEXT NOT NULL
        //, [expDate] TEXT NOT NULL
        //, [productName] TEXT NOT NULL
        //, [numРacksInBox] INTEGER NOT NULL
        //, [numBoxesinPallet] INTEGER NOT NULL
        //, [quantity] INTEGER NOT NULL
        //, CONSTRAINT [PK_ProductSqlA] PRIMARY KEY ([productId])
        //)";

        //private string PalleteTableCreate = @"CREATE TABLE Pallets (
        //palleteId    INTEGER NOT NULL
        //                  PRIMARY KEY AUTOINCREMENT,
        //productId INTEGER NOT NULL,
        //quantity      NVARCHAR(20) NOT NULL,
        //rootCode  NVARCHAR(20) NOT NULL,
        //numРacks  TEXT    NOT NULL,
        //tp        INTEGER NOT NULL,
        //st        INTEGER NOT NULL,
        //FOREIGN KEY (productId)
        //REFERENCES ProductSqlA (productId) ON DELETE NO ACTION
        //                               ON UPDATE NO ACTION)";

        //private string BoxesTableCreate = @"CREATE TABLE Boxes (
        //boxId   INTEGER NOT NULL
        //                 PRIMARY KEY AUTOINCREMENT,
        //palleteId  INTEGER NOT NULL,
        //quantity      NVARCHAR(20) NOT NULL,
        //rootCode  NVARCHAR(20) NOT NULL,
        //numРacks TEXT    NOT NULL,
        //tp       INTEGER NOT NULL,
        //st       INTEGER NOT NULL,
        //FOREIGN KEY (
        //    palleteId
        //)
        //REFERENCES Pallets (palleteId) ON DELETE NO ACTION
        //                          ON UPDATE NO ACTION
        //)";
        #endregion

        #region SqlFunc
        public string AcceptOrder(Shipping1СOrder o)
        {
            //пока отключить 
            return "";

            //try
            //{
            //    //создать подключение и файл базы
            //    string storedPath = "";

            //    storedPath = System.IO.Path.GetDirectoryName(
            //    System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\";

            //    //создать директорию для хранения текущих заданий если надо
            //    if (!System.IO.Directory.Exists(storedPath))
            //        System.IO.Directory.CreateDirectory(storedPath);


            //    //создать директорию для хранения конкретно этого задания 
            //    storedPath += id;
            //    if (!System.IO.Directory.Exists(storedPath))
            //        System.IO.Directory.CreateDirectory(storedPath);

            //    string dbFileName = storedPath + "\\" + id + ".sl3";
            //    if (File.Exists(dbFileName))
            //        File.Delete(dbFileName);

            //    //создать и подключить файл данных
            //    SQLiteConnection m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
            //    SQLiteCommand m_sqlCmd = new SQLiteCommand();
            //    SQLiteConnection.CreateFile(dbFileName);

            //    m_dbConn.Open();
            //    m_sqlCmd.Connection = m_dbConn;
            //    //создать таблицып 
            //    m_sqlCmd.CommandText = ProductTableCreate;
            //    m_sqlCmd.ExecuteNonQuery();

            //    m_sqlCmd.CommandText = PalleteTableCreate;
            //    m_sqlCmd.ExecuteNonQuery();

            //    m_sqlCmd.CommandText = BoxesTableCreate;
            //    m_sqlCmd.ExecuteNonQuery();

            //    m_sqlCmd.Connection.Close();
            //    m_sqlCmd.Dispose();

            //    m_dbConn.Close();
            //    m_dbConn.Dispose();

            //    using (var context = new DatabaseContext(dbFileName))
            //    {
            //        for (int i = 0; i < order1C.product.Count; i++)
            //        {
            //            Shipping1СOrder.Product pr = order1C.product[i];
            //            ProductSqlA prod = new ProductSqlA();
            //            prod.productId = i;
            //            prod.id = pr.id;
            //            prod.gtin = pr.gtin;
            //            //prod.addProdInfo = pr.addProdInfo;
            //            prod.lotNo = pr.lotNo;
            //            prod.expDate = pr.ExpDate;
            //            prod.productName = pr.productName;
            //            prod.numРacksInBox = pr.numРacksInBox;
            //            prod.numBoxesInPallet = pr.numBoxesInPallet;
            //            prod.num = pr.quantity;

            //            for (int iK = 0; iK < pr.palletsNumbers.Count; iK++)
            //            {
            //                Shipping1СOrder.Pallets pl = pr.palletsNumbers[iK];
            //                ItemSqlP itemP = new ItemSqlP();
            //                itemP.palleteId = iK;
            //                itemP.productId = prod.productId;

            //                itemP.code = pl.palletNumber;
            //                itemP.rootCode = "";
            //                itemP.numРacks = 0;
            //                itemP.tp = SelectedItem.ItemType.Паллета;
            //                itemP.st = CodeState.New;

            //                prod.Items.Add(itemP);
            //                //уровень короба
            //                foreach (string boxNum in pl.boxNumbers)
            //                {
            //                    ItemSqlB itemB = new ItemSqlB();
            //                    //itemB.boxId = null;
            //                    itemB.palleteId = itemP.palleteId;

            //                    itemB.code = boxNum;
            //                    itemB.rootCode = "";
            //                    itemB.numРacks = 0;
            //                    itemB.tp = SelectedItem.ItemType.Короб;
            //                    itemB.st = CodeState.New;

            //                    itemP.Items.Add(itemB);
            //                }
            //            }
            //            context.Products.Add(prod);
            //        }
            //        context.SaveChanges();
            //        context.Database.Connection.Close();
            //        context.Dispose();
            //    }

            //    //db.Dispose();

            //}
            //catch (SQLiteException ex)
            //{
            //    Log.Write("SqlLite новое состоние: " + ex.ToString());
            //}
            //catch (Exception ex)
            //{
            //    Log.Write("SqlLite новое состоние: " + ex.ToString());
            //}
            //return "";
        }
        #endregion

    }


    #region OldServerJob
    /*
[DataContract]
public class ShippingServerJobOld : AggCorobBaseInfo, IBaseJob
{
    private ReaderWriterLockSlim _rw;
    private DatabaseContext context;

    #region Реализация интерфейса BaseJob
    private OrderMeta meta = new OrderMeta();

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

            meta.name = "Накл: " + invoiceNum + "\r" + order1C.customer;
            meta.id = id;
            meta.type = 0;
            //meta.state = 0;
            return meta;
        }
        set { meta = value; }
    }

    [DataMember]
    public JobState JobState { get; set; }
    public bool JobIsAwaible
    {
        get
        {
            if (JobState == JobState.New)
                return true;

            if (JobState == JobState.WaitSend)
                return true;

            if (JobState == JobState.CloseAndAwaitSend)
                return true;

            return false;
        }
    }

    public object GetTsdJob()
    {
        ShippingJob sj = new ShippingJob();
        sj.id = id;
        sj.invoise = order1C.invoiceNum;
        sj.customer = order1C.customer;
        sj.JobMeta = JobMeta;
        sj.product.AddRange(shipSelectedProduct);


        //перекинуть позиции наименования товара
        //foreach (Shipping1СOrder.Product pr in order1C.product)
       // {
        //      sj.product.Add(new ShippingJob.Product(pr.productName, pr.quantity,pr.gtin));
        //}
        //отметить задание как поступившее в работу
        JobState = JobState.InWorck;
        return sj;
    }

    public  object GetTsdSqLiteJob() { throw new NotImplementedException(); }
    public bool WaitSend
    {
        get
        {
            if (JobState == JobState.WaitSend)
                return true;

            if (JobState == JobState.CloseAndAwaitSend)
                return true;

                return false;
        }
    }

    public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
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
            result = WebUtil.SendReport<SalesReport>(url, user, pass, "POST", readyReport, "ShippRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));
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
    }
    #endregion
    [DataMember]
    private SalesReport  readyReport = new SalesReport(); //массив отчетов для отправки

    [DataMember]
    public Shipping1СOrder order1C;

    [DataMember]
    public string invoiceNum;

    [DataMember]
    public string operatorId;

    [DataMember]
    public List<ShippingJob.Product> shipSelectedProduct = new List<ShippingJob.Product>();

    [DataMember]
    public DateTime startTime;
    public ShippingServerJobOld() : base()
    {
        meta.state = JobIcon.Default;
        jobType = typeof(ShippingServerJobOld);
    }

    public SalesReport CreateReportA()
    {
        SalesReport r = new SalesReport();
        r.id = id;
        r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
        r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        r.OperatorId = operatorId;
        //создать отчет
        //проход по всем продуктам
        foreach (ShippingJob.Product spr in shipSelectedProduct)
        {
            SalesReport.Product sPr = new SalesReport.Product();
            sPr.gtin = spr.gtin;
            sPr.lotNo = spr.lotNo;
            //sPr.expDate = spr.expDate;

            //проход по всем отобранным кодам в продукте
            foreach (UnitItem it in spr.SelectedItems)
            {
                switch (it.tp)
                {
                    case SelectedItem.ItemType.Короб:
                        sPr.boxNumbers.Add(it.num);
                        break;
                    case SelectedItem.ItemType.Паллета:
                        sPr.palletsNumbers.Add(it.num);
                        break;
                    case SelectedItem.ItemType.Упаковка:
                        sPr.Numbers.Add(it.num);
                        break;

                }
            }

            //добавить в отчет если есть что добавлять
            if (sPr.IsNonEmpty())
                r.product.Add(sPr);
        }

        //пометить задание как отработанное
        //JobState = JobState.WaitSend;
        return r;
    }

    private SQLiteConnection db;
    public ShippingJob.Product ProssedNumber(string fullNumber, ShippingQueryType type, out string errInfo)
    {
        if (_rw == null)
            _rw = new ReaderWriterLockSlim();

        if (_rw.TryEnterWriteLock(500))
        {

            try
            {
                errInfo = "Код не найден в задании";
                //проверить ссцц или серийный номер если их нет выдать ошибку
                Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

                UnitItem item = new UnitItem("",SelectedItem.ItemType.Неизвестно);

                if (ld.SerialNumber != null)
                {
                    item.num = ld.SerialNumber;
                    //проверить есть ли такой GTIN в задлании
                    foreach (Shipping1СOrder.Product pr in order1C.product)
                    {
                        if (pr.VerifyProductNum(fullNumber))
                        {
                            item.qP = 1;
                            item.tp = SelectedItem.ItemType.Упаковка;
                            item.oId = pr.id;
                            item.st = CodeState.Verify;
                            break;
                        }
                    }
                }
                else if (ld.SerialShippingContainerCode00 != null)
                {
                    item.num = ld.SerialShippingContainerCode00;

                    //найти номер третичной упаковки в задании
                    foreach (Shipping1СOrder.Product pr in order1C.product)
                    {
                        //уровенить палеты
                        foreach (Shipping1СOrder.Pallets pl in pr.palletsNumbers)
                        {
                            if (item.num == pl.palletNumber)
                            {
                                item.tp = SelectedItem.ItemType.Паллета;
                                item.qP = pr.numРacksInBox* pl.boxNumbers.Count;
                                item.oId = pr.id;
                                item.st = CodeState.Verify;
                                //уровень короба
                                foreach (string boxNum in pl.boxNumbers)
                                    item.items.Add(new UnitItem(boxNum, SelectedItem.ItemType.Короб));

                                break;
                            }

                            //уровень короба
                            foreach (string boxNum in pl.boxNumbers)
                            {
                                if (item.num == boxNum)
                                {
                                    item.tp = SelectedItem.ItemType.Короб;
                                    item.qP = pr.numРacksInBox;
                                    item.st = CodeState.Verify;
                                    item.oId = pr.id;
                                    item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                    break;
                                }
                            }
                            //если элемент найден закончить цыкл
                            if (item.tp != SelectedItem.ItemType.Неизвестно)
                                break;

                        }
                        //если элемент найден закончить цыкл
                        if (item.tp != SelectedItem.ItemType.Неизвестно)
                            break;
                    }

                }
                else
                    throw new Exception("Номер не распознан!");

                //если элемент не найден закончить цыкл
                if (item.tp == SelectedItem.ItemType.Неизвестно)
                    throw new Exception("Номер не найден в задании!");

                //проверить что номер  еще не обработался
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    if (item.oId == spr.id)
                    {
                        //продукт найден выполняем требуемое
                        switch (type)
                        {
                            case ShippingQueryType.Add:
                                //проверить что такой код ещ ене добавлен
                                foreach (UnitItem it in spr.SelectedItems)
                                {
                                    if (it.num == item.num)
                                    {
                                        //если такой номер уже есть вернуть GTIN для номера и признак
                                        errInfo = "ALRNUM*" + spr.gtin + "*";
                                        return null;

                                    }

                                    //проверить не вложен ли номер внутри уже обработанных контейнеров
                                    if (it.num == item.GetRoot()?.num)
                                    {
                                        throw new Exception("Номер уже обработан в составе контейнера : " + item.GetRoot().num);
                                        //errInfo = "ALRINCL*" + spr.gtin + "*"+ item.rootContainerNumber+"*";
                                        //return null;
                                    }
                                }

                                //если ето номер палеты то проверить не числится ли какойто номер из вложенных как обработанный
                                foreach (UnitItem it in spr.SelectedItems)
                                {

                                }

                                //сравнить добовляющееся количество чтоб оно не превышало 
                                //нужное
                                if (spr.number < (spr.allreadyNum + item.GetItemsQuantity()))
                                    throw new Exception("Количество добавляемого продукта превышает необходимое!");

                                //добавить в отобранные
                                //ShippingJob.Item sit = new ShippingJob.Item(SelectedItem.ItemType.Неизвестно, 0,"");
                                UnitItem sit = new UnitItem(item.num, item.tp);
                                sit.num = item.num;
                                sit.tp = item.tp;
                                sit.qP = 1;
                                sit.qIp = item.GetItemsQuantity();
                                sit.id = item.id;

                                if (spr.Add(sit))
                                    return spr;
                                else
                                    return null;

                                break;
                        }

                    }
                }

                // ShippingJob.Product
            }
            catch (Exception ex)
            {
                errInfo = ex.Message;
                return null;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
        return null;// new ShippingJob.Product("", 0, "");
    }

    public ShippingJob.Product DeleteNumber(string number, string gtin, ShippingQueryType type, out string errInfo)
    {
        if (_rw == null)
            _rw = new ReaderWriterLockSlim();

        if (_rw.TryEnterWriteLock(500))
        {
            try
            {
                errInfo = "Код не найден в задании";


                //если элемент не найден закончить цыкл
                if (number == " ")
                    throw new Exception("Номер не распознан!");

                //найти номер в обработанных
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    if (spr.gtin == gtin)
                    {
                        UnitItem itm = spr.GetItemAtNum(number);
                        if (itm != null)
                        {
                            spr.Remove(itm);
                            return spr;
                        }
                    }
                }

                // ShippingJob.Product
            }
            catch (Exception ex)
            {
                errInfo = ex.Message;
                return null;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
        return null;// new ShippingJob.Product("", 0, "");
    }

    public bool JobIsComplited()
    {
        if (JobState == JobState.Complited)
            return true;

        return false;
    }



    #region SqlQuery
    private string ProductTableCreate = @"CREATE TABLE [ProductSqlA] (
      [productId] INTEGER NOT NULL
    , [id] TEXT NOT NULL
    , [gtin] NVARCHAR(20) NOT NULL
    , [addProdInfo] TEXT NOT NULL
    , [lotNo] TEXT NOT NULL
    , [expDate] TEXT NOT NULL
    , [productName] TEXT NOT NULL
    , [numРacksInBox] INTEGER NOT NULL
    , [numBoxesinPallet] INTEGER NOT NULL
    , [quantity] INTEGER NOT NULL
    , CONSTRAINT [PK_ProductSqlA] PRIMARY KEY ([productId])
    )";

    private string PalleteTableCreate = @"CREATE TABLE Pallets (
    palleteId    INTEGER NOT NULL
                      PRIMARY KEY AUTOINCREMENT,
    productId INTEGER NOT NULL,
    quantity      NVARCHAR(20) NOT NULL,
    rootCode  NVARCHAR(20) NOT NULL,
    numРacks  TEXT    NOT NULL,
    tp        INTEGER NOT NULL,
    st        INTEGER NOT NULL,
    FOREIGN KEY (productId)
    REFERENCES ProductSqlA (productId) ON DELETE NO ACTION
                                   ON UPDATE NO ACTION)";

    private string BoxesTableCreate = @"CREATE TABLE Boxes (
    boxId   INTEGER NOT NULL
                     PRIMARY KEY AUTOINCREMENT,
    palleteId  INTEGER NOT NULL,
    quantity      NVARCHAR(20) NOT NULL,
    rootCode  NVARCHAR(20) NOT NULL,
    numРacks TEXT    NOT NULL,
    tp       INTEGER NOT NULL,
    st       INTEGER NOT NULL,
    FOREIGN KEY (
        palleteId
    )
    REFERENCES Pallets (palleteId) ON DELETE NO ACTION
                              ON UPDATE NO ACTION
    )";
    #endregion

    #region SqlFunc
    public string AcceptOrder(Shipping1СOrder o)
    {
        try
        {
            //создать подключение и файл базы
            string storedPath = "";

            storedPath = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\";

            //создать директорию для хранения текущих заданий если надо
            if (!System.IO.Directory.Exists(storedPath))
                System.IO.Directory.CreateDirectory(storedPath);


            //создать директорию для хранения конкретно этого задания 
            storedPath += id;
            if (!System.IO.Directory.Exists(storedPath))
                System.IO.Directory.CreateDirectory(storedPath);

            string dbFileName = storedPath + "\\" + id + ".sl3";
            if (File.Exists(dbFileName))
                File.Delete(dbFileName);

            //создать и подключить файл данных
            SQLiteConnection m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
            SQLiteCommand m_sqlCmd = new SQLiteCommand();
            SQLiteConnection.CreateFile(dbFileName);

            m_dbConn.Open();
            m_sqlCmd.Connection = m_dbConn;
            //создать таблицып 
            m_sqlCmd.CommandText = ProductTableCreate;
            m_sqlCmd.ExecuteNonQuery();

            m_sqlCmd.CommandText = PalleteTableCreate;
            m_sqlCmd.ExecuteNonQuery();

            m_sqlCmd.CommandText = BoxesTableCreate;
            m_sqlCmd.ExecuteNonQuery();

            m_sqlCmd.Connection.Close();
            m_sqlCmd.Dispose();

            m_dbConn.Close();
            m_dbConn.Dispose();

            using (var context = new DatabaseContext(dbFileName))
            {
                for(int i = 0; i < order1C.product.Count; i++)
                {
                    Shipping1СOrder.Product pr = order1C.product[i];
                    ProductSqlA prod = new ProductSqlA();
                    prod.productId = i;
                    prod.id = pr.id;
                    prod.gtin = pr.gtin;
                    prod.addProdInfo = pr.addProdInfo;
                    prod.lotNo = pr.lotNo;
                    prod.expDate = pr.expDate;
                    prod.productName = pr.productName;
                    prod.numРacksInBox = pr.numРacksInBox;
                    prod.numBoxesInPallet = pr.numBoxesInPallet;
                    prod.num = pr.quantity;

                    for (int iK=0;iK < pr.palletsNumbers.Count;iK++ )
                    {
                        Shipping1СOrder.Pallets pl = pr.palletsNumbers[iK];
                        ItemSqlP itemP = new ItemSqlP();
                        itemP.palleteId = iK;
                        itemP.productId = prod.productId;

                        itemP.code = pl.palletNumber;
                        itemP.rootCode = "";
                        itemP.numРacks = 0;
                        itemP.tp = SelectedItem.ItemType.Паллета;
                        itemP.st = CodeState.New;

                        prod.Items.Add(itemP);
                        //уровень короба
                        foreach (string boxNum in pl.boxNumbers)
                        {
                            ItemSqlB itemB = new ItemSqlB();
                            //itemB.boxId = null;
                            itemB.palleteId = itemP.palleteId;

                            itemB.code = boxNum;
                            itemB.rootCode = "";
                            itemB.numРacks = 0;
                            itemB.tp = SelectedItem.ItemType.Короб;
                            itemB.st = CodeState.New;

                            itemP.Items.Add(itemB);
                        }
                    }
                    context.Products.Add(prod);
                }
                context.SaveChanges();
                context.Database.Connection.Close();
                context.Dispose();
            }

            //db.Dispose();

        }
        catch (SQLiteException ex)
        {
            Log.Write("SqlLite новое состоние: " + ex.ToString());
        }
        catch (Exception ex)
        {
            Log.Write("SqlLite новое состоние: " + ex.ToString());
        }
        return "";
    }
    #endregion

}*/
    #endregion


    #region NewOldServerJob
    /*[DataContract]
    public class ShippingServerJob :  AggCorobBaseInfo, IBaseJob
    {
        private ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
       // private DatabaseContext context;

        #region Реализация интерфейса BaseJob
        private OrderMeta meta = new OrderMeta();

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

                meta.name = "Накл: " + invoiceNum + "\n" + order1C.customer;
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
                if (JobState == JobStates.New)
                    return true;

                if (JobState == JobStates.WaitSend)
                    return true;

                if (JobState == JobStates.CloseAndAwaitSend)
                    return true;

                return false;
            }
        }

        public object GetTsdJob()
        {
            ShippingJob sj = new ShippingJob();
            sj.id = id;
            sj.invoise = order1C.invoiceNum;
            sj.customer = order1C.customer;
            sj.JobMeta = JobMeta;
            sj.product.AddRange(shipSelectedProduct);

            /*
            //перекинуть позиции наименования товара
            foreach (Shipping1СOrder.Product pr in order1C.product)
            {
                  sj.product.Add(new ShippingJob.Product(pr.productName, pr.quantity,pr.gtin));
            }*
            //отметить задание как поступившее в работу
            JobState = JobStates.InWorck;
            return sj;
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
                //создать отчет
                readyReport = CreateReportA();
                //выполнить отправку 
                result = WebUtil.SendReport<SalesReport>(url, user, pass, "POST", readyReport, "ShippRep" + DateTime.Now.ToString(" dd HH.mm.ss.fff"));
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
        #endregion
        [DataMember]
        private SalesReport readyReport = new SalesReport(); //массив отчетов для отправки

        [DataMember]
        public Shipping1СOrder order1C;

        [DataMember]
        public string invoiceNum;

        [DataMember]
        public string operatorId;

        [DataMember]
        public List<ShippingJob.Product> shipSelectedProduct = new List<ShippingJob.Product>();

        [DataMember]
        public DateTime startTime;

        //private SQLiteConnection db;

        public ShippingServerJob() : base()
        {
            meta.state = JobIcon.Default;
            jobType = typeof(ShippingServerJob);
        }

        public SalesReport CreateReportA()
        {
            SalesReport r;
            //если задание завершено и отчет уже сформирован просто отдать ранее сохраненный отчет
            if ((JobState == JobStates.CloseAndAwaitSend)||(JobState == JobStates.Complited))
            {
                r = Archive.RestoreReport<SalesReport>(id);
                if (r != null)
                    return r;
            }
            

            r = new SalesReport();
            r.id = id;
            r.startTime = startTime.ToString("yyyy-MM-ddThh:mm:ssz");
            r.endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
            r.OperatorId = operatorId;
            //создать отчет
            bool bFound = false;
            //проход по всем продуктам
            foreach (ShippingJob.Product spr in shipSelectedProduct)
            {
                //UnitItemM r = new UnitItemM(gtin, (int)tp, num);
                foreach (UnitItem ui in spr.SelectedItems)
                {
                    //если елемент не числится на палете закинуть в масив иначе искать палету и кидать туда
                    if ((ui.pN == "") || (ui.pN == null))
                        r.Items.Add(ui.GetUnitItemM(spr.gtin));
                    else
                    {
                        bFound = false;
                        //проход по всем уже обработанным позициям в поисках палеты
                        foreach (UnitItemM ri in r.Items)
                        {
                            //если палета найдена добавить елемент в нее
                            if (ri.num == ui.pN)
                            {
                                ri.items.Add(ui.GetUnitItemM(spr.gtin));
                                bFound = true;
                                break;
                            }
                        }
                        //если палета не найдена . создать и добавить ее
                        if (!bFound)
                        {
                            UnitItemM nPal = new UnitItemM("", (int)SelectedItem.ItemType.Паллета, ui.pN);
                            nPal.items.Add(ui.GetUnitItemM(spr.gtin));
                            r.Items.Add(nPal);
                        }
                    }
                }
            }

            //сохранить в архив 
            Archive.SaveReport<SalesReport>(r, id);
                

            //пометить задание как отработанное
            //JobState = JobState.WaitSend;
            return r;
        }

        public override string InitJob<T>(T order, string user, string pass)
        {
            Shipping1СOrder o = order as Shipping1СOrder;
            if (o == null)
                return  "Wrong object type. Need Shipping1СOrder";


            //создать задачу свервера
            order1C = o;
            id = o.id;
            invoiceNum = o.invoiceNum;
            startTime = DateTime.Now;
            JobState = JobStates.New;

            //сохранить и загрузить шаблоны
            string fileName = System.IO.Path.GetDirectoryName(
                   System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id + "\\";

            if (!Directory.Exists(fileName))
                Directory.CreateDirectory(fileName);

            //загрузить файл шаблона сборной палеты если он есть
            if (o.urlLabelPalletTemplate != "")
            {
                if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, "mPallete.tmpl"))
                    return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
            }

           
            int idCounter = 0;
            foreach (Shipping1СOrder.Product pr in o.product)
            {
                //загрузить шаблон етикетки палеты
                //загрузить файл шаблона палеты если он есть
                if (pr.urlLabelPalletTemplate != "")
                {
                    if (!WebUtil.DownLoadFile(o.urlLabelPalletTemplate, user, pass, fileName, pr.gtin+".tmpl"))
                        return "Ошибка загрузки шаблона палеты. url: " + o.urlLabelPalletTemplate;
                }

                pr.id = idCounter.ToString();
                ShippingJob.Product npr = new ShippingJob.Product(pr.id, pr.productName, pr.quantity, pr.gtin, pr.lotNo, pr.addProdInfo, pr.ExpDate
                    , pr.gtin + ".tmpl");
                shipSelectedProduct.Add(npr);
                idCounter++;
            }
            return "";
        }
        
        public ShippingJob.AddItemAnswer ProssedNumber(string fullNumber,string palNumber, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(5000))
            {

                try
                {
                    errInfo = "Код не найден в задании";
                    //проверить ссцц или серийный номер если их нет выдать ошибку
                    Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

                    UnitItem item = new UnitItem("", SelectedItem.ItemType.Неизвестно);
                    //присвоить номер палеты на который его планируется положить
                    item.pN = palNumber;

                    if (ld.SerialNumber != null)
                    {
                        item.num = ld.SerialNumber;
                        //проверить есть ли такой GTIN в задлании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            if (pr.VerifyProductNum(fullNumber))
                            {
                                item.qP = 1;
                                item.tp = SelectedItem.ItemType.Упаковка;
                                item.oId = pr.id;
                                item.st = CodeState.Verify;
                                break;
                            }
                        }
                    }
                    else if (ld.SerialShippingContainerCode00 != null)
                    {
                        item.num = ld.SerialShippingContainerCode00;

                        //найти номер третичной упаковки в задании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            //уровенить палеты
                            foreach (Shipping1СOrder.Pallets pl in pr.palletsNumbers)
                            {
                                if (item.num == pl.palletNumber)
                                {
                                    //если предпринята попытка добавить на палету код другой палеты
                                    //выдать ошибку
                                    if(palNumber != null)
                                        throw new Exception("Нельзя добавлять одну палету в другую");

                                    item.tp = SelectedItem.ItemType.Паллета;
                                    item.qP = pr.numРacksInBox * pl.boxNumbers.Count;
                                    item.oId = pr.id;
                                    item.st = CodeState.Verify;
                                    //уровень короба
                                    foreach (string boxNum in pl.boxNumbers)
                                        item.items.Add(new UnitItem(boxNum, SelectedItem.ItemType.Короб));

                                    break;
                                }

                                //уровень короба
                                foreach (string boxNum in pl.boxNumbers)
                                {
                                    if (item.num == boxNum)
                                    {
                                        item.tp = SelectedItem.ItemType.Короб;
                                        item.qP = pr.numРacksInBox;
                                        item.st = CodeState.Verify;
                                        item.oId = pr.id;
                                        item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                        break;
                                    }
                                }
                                //если элемент найден закончить цыкл
                                if (item.tp != SelectedItem.ItemType.Неизвестно)
                                    break;
                            }
                            //если элемент найден закончить цыкл
                            if (item.tp != SelectedItem.ItemType.Неизвестно)
                                break;

                            //если не найден в основном проверить  в массиве неполных
                            foreach (Shipping1СOrder.notCompleteBoxItem ncbi in pr.notCompleteBoxNumbers)
                            {
                                if (item.num == ncbi.num)
                                {
                                    item.tp = SelectedItem.ItemType.Короб;
                                    item.qP = ncbi.quantity;
                                    item.st = CodeState.Verify;
                                    item.oId = pr.id;
                                    //item.SetRoot(new UnitItem(pl.palletNumber, SelectedItem.ItemType.Паллета));
                                    break;
                                }
                            }

                            //если элемент найден закончить цыкл
                            if (item.tp != SelectedItem.ItemType.Неизвестно)
                                break;
                        }

                    }
                    else
                        throw new Exception("Номер не распознан!");

                    //если элемент не найден закончить цыкл
                    if (item.tp == SelectedItem.ItemType.Неизвестно)
                        throw new Exception("Номер не найден в задании!");

                    //проверить что номер  еще не обработался
                    foreach (ShippingJob.Product spr in shipSelectedProduct)
                    {
                        if (item.oId == spr.id)
                        {
                            //продукт найден выполняем требуемое
                            switch (type)
                            {
                                case ShippingQueryType.Add:
                                    //проверить что такой код ещ ене добавлен
                                    foreach (UnitItem it in spr.SelectedItems)
                                    {
                                        if (it.num == item.num)
                                        {
                                            //если такой номер уже есть вернуть GTIN для номера и признак
                                            errInfo = "ALRNUM*" + spr.gtin + "*";
                                            return null;

                                        }

                                        //проверить не вложен ли номер внутри уже обработанных контейнеров
                                        if (it.num == item.GetRoot()?.num)
                                        {
                                            throw new Exception("Номер уже обработан в составе контейнера : " + item.GetRoot().num);
                                            //errInfo = "ALRINCL*" + spr.gtin + "*"+ item.rootContainerNumber+"*";
                                            //return null;
                                        }
                                    }

                                    //если ето номер палеты то проверить не числится ли какойто номер из вложенных как обработанный
                                    foreach (UnitItem it in spr.SelectedItems)
                                    {
                                        if (it.GetRoot()?.num == item.num)
                                        {
                                            throw new Exception("Часть контейнера уже обработана! Контейнер не может быть обработан как целый.   Обработано:"+it.tp.ToString()+"              №:" + it.num);
                                        }
                                    }

                                    //сравнить добовляющееся количество чтоб оно не превышало 
                                    //нужное
                                    if (spr.number < (spr.allreadyNum + item.GetItemsQuantity()))
                                        throw new Exception("Количество добавляемого продукта превышает необходимое!");

                                    //добавить в отобранные
                                    /*ShippingJob.Item sit = new ShippingJob.Item(SelectedItem.ItemType.Неизвестно, 0, "");
                                    sit.fullNumber = item.quantity;
                                    sit.type = item.tp;
                                    sit.rootContainerNumber = item.GetRoot()?.quantity;
                                    sit.numРacks = 1;
                                    sit.numberItemInPack = item.GetItemsQuantity();
                                    sit.productId = item.oId;*
                                    UnitItem sit = item.Clone();
                                    sit.qIp = item.GetItemsQuantity();
                                    item.items.Clear();
                                    

                                    if (spr.Add(sit))
                                    {
                                        SafeToDisk();
                                        ShippingJob.AddItemAnswer answer = new ShippingJob.AddItemAnswer();
                                        answer.products.Add(spr);
                                        //answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN);
                                        answer.curentPaletteItemCount = GetItemsCountAtPalette(item.pN,
                                            out answer.curentPalettePackCount,
                                            out answer.curentPaletteBoxCount,
                                            out answer.paletteProductGtin);
                                       

                                        return answer;
                                    }
                                    else
                                        return null;

                                    //break;
                            }

                        }
                    }

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }
        public int GetItemsCountAtPalette(string number, out int PackCount,out int BoxCount, out List<string> _gtins)
        {
            PackCount = 0;
            BoxCount = 0;
            _gtins = new List<string>();

            if (number == null)
                return 0;

            if (number == "")
                return 0;

            int result = 0;
            try
            {
                //если элемент не найден закончить цыкл
                if (number == " ")
                    throw new Exception("Номер не распознан!");

                
                //пройти по всем продуктам чтоб найти все элементы на палете
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    foreach (UnitItem it in spr.SelectedItems)
                    {
                        if (it.pN == number)
                        {
                            result++;

                            if (it.tp == SelectedItem.ItemType.Упаковка)
                                PackCount++;
                            else if (it.tp == SelectedItem.ItemType.Короб)
                                BoxCount++;

                            //добавить GTIN продукта на палету
                            string _g = _gtins.FindLast(x => x == number);
                            if (_g != number)
                                _gtins.Add(spr.gtin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();

            }
            finally
            {

            }
            return result;
        }

        public PaleteLadelData GetPaleteLabelData(string number)
        {
            PaleteLadelData data = new PaleteLadelData();
            bool bFound = false;
            try
            {
                //проходж по всем отобранным продуктам
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    //проход по всем отобранным елементам в продукте
                    foreach (UnitItem it in spr.SelectedItems)
                    {
                        if (it.pN == number)
                        {
                            //счетчики пачек и коробов на палете
                            if (it.tp == SelectedItem.ItemType.Упаковка)
                                data.curentPalettePackCount++;
                            else
                                data.curentPaletteBoxCount++;


                             bFound = false;
                            //найти такой продукт в массиве
                            foreach (PaleteLadelData.ProductInfo pi in data.products)
                            {
                                if (pi.gtin != spr.gtin)
                                    continue;

                                if (pi?.lotNo != spr?.lotNo)
                                    continue;

                                pi.quantity += it.qIp;
                                bFound = true;
                            }

                            if (bFound)
                                continue;

                            //добавить продукт раз его нет
                            PaleteLadelData.ProductInfo pri = new PaleteLadelData.ProductInfo();
                            pri.gtin = spr.gtin;
                            pri.lotNo = spr?.lotNo;
                            pri.expDate = spr?.expDate;
                            pri.addProdInfo = spr?.addProdInfo;
                            pri.quantity =  it.qIp;
                            pri.name = spr.name;

                            data.products.Add(pri);
                        }
                    }
                }
                data.sscc = number;
                return data;


            }
            catch (Exception ex) { ex.ToString(); }
            return null;
        }
        public string GetPaleteLabel(string palNum)
        {
             string fileName = System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\" + id ;

            List<string> _gtins = new List<string>();
            //если элемент не найден закончить цыкл
            if (palNum == " ")
                throw new Exception("Номер не распознан!");

            try
            {
              
                //пройти по всем продуктам чтоб найти все элементы на палете
                foreach (ShippingJob.Product spr in shipSelectedProduct)
                {
                    foreach (UnitItem it in spr.SelectedItems)
                    {
                        if (it.pN == palNum)
                        {
                            //добавить GTIN продукта на палету
                            string _g = _gtins.FindLast(x => x == spr.gtin);
                            if (_g != spr.gtin)
                                _gtins.Add(spr.gtin);
                        }
                    }
                }

                if (_gtins.Count > 1)
                {
                    return fileName + "\\mPallete.tmpl";
                }else
                    return fileName + "\\" + _gtins[0] + ".tmpl";
                
            }
            catch (Exception ex)
            {
                ex.ToString();

            }
            finally
            {

            }
            return "";
        }
        public ShippingJob.AddItemAnswer ClosePalete(string palNumber, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(5000))
            {
                errInfo = "";
                ShippingJob.AddItemAnswer answer = new ShippingJob.AddItemAnswer();
                bool bUpdate = false;
                try
                {
                    
                    switch (type)
                    {
                        case ShippingQueryType.ClosePalete:
                            //закрываем палету . ниче не делаем просто возврашаем ок
                           
                            break;
                        case ShippingQueryType.DropPaleteDropContent:
                            //пройти по всем отобранным позициям 
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                //удалить отобранные позиции из отобранного
                                if (spr.SelectedItems.RemoveAll(t => t.pN == palNumber) > 0)
                                {
                                    //пересчитать счетчики отобранного
                                    spr.RefreshCounters();
                                    //добавить в ответ 
                                    answer.products.Add(spr);
                                }
                            }
                            break;
                        case ShippingQueryType.DropPaleteSafeContent:
                           //пройти по всем отобранным продуктам
                            foreach (ShippingJob.Product spr in shipSelectedProduct)
                            {
                                bUpdate = false;
                                //проверить что такой код ещ ене добавлен
                                foreach (UnitItem it in spr.SelectedItems)
                                {
                                    if (it.pN != palNumber)
                                        continue;

                                    it.pN = null;
                                    bUpdate = true;
                                }

                                if(bUpdate)//добавить в ответ 
                                    answer.products.Add(spr);
                            }
                            break;
                        default:
                            errInfo = "ClosePalete Неверный тип запроса: " + type.ToString();
                            return null;
                    }
                    return answer;

                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;
        }
        /*
        public ShippingJob.Product ProssedNumber(string fullNumber, ShippingQueryType type, out string errInfo)
        {
            if(_rw == null)
                  _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(500))
            {

                try
                {
                    errInfo = "Код не найден в задании";
                    //проверить ссцц или серийный номер если их нет выдать ошибку
                    Util.GsLabelData ld = new Util.GsLabelData(fullNumber);

                    ShippingJob.Item item = new ShippingJob.Item(ShippingJob.ItemType.Неизвестно, 0, "");

                    if (ld.SerialNumber != null)
                    {
                        item.fullNumber = ld.SerialNumber;
                        //проверить есть ли такой GTIN в задлании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            if (pr.VerifyProductNum(fullNumber))
                            {
                                item.numРacks = 1;
                                item.type = ShippingJob.ItemType.Упаковка;
                                item.productId = pr.id;
                                break;
                            }
                        }
                    }
                    else if (ld.SerialShippingContainerCode00 != null)
                    {
                        item.fullNumber = ld.SerialShippingContainerCode00;

                        //найти номер третичной упаковки в задании
                        foreach (Shipping1СOrder.Product pr in order1C.product)
                        {
                            //уровенить палеты
                            foreach (Shipping1СOrder.Pallets pl in pr.palletsNumbers)
                            {
                                if (item.fullNumber == pl.palletNumber)
                                {
                                    item.type = ShippingJob.ItemType.Паллета;
                                    item.numРacks = pl.boxNumbers.Count;
                                    item.numberItemInPack = pr.numРacksInBox;
                                    item.productId = pr.id;
                                    break;
                                }
                                //уровень короба
                                foreach (string boxNum in pl.boxNumbers)
                                {
                                    if (item.fullNumber == boxNum)
                                    {
                                        item.type = ShippingJob.ItemType.Короб;
                                        item.numРacks = 1;
                                        item.numberItemInPack = pr.numРacksInBox;
                                        item.productId = pr.id;
                                        item.rootContainerNumber = pl.palletNumber;
                                        break;
                                    }
                                }
                                //если элемент найден закончить цыкл
                                if (item.type != ShippingJob.ItemType.Неизвестно)
                                    break;

                            }
                            //если элемент найден закончить цыкл
                            if (item.type != ShippingJob.ItemType.Неизвестно)
                                break;
                        }

                    }
                    else
                        throw new Exception("Номер не распознан!");

                    //если элемент не найден закончить цыкл
                    if (item.type == ShippingJob.ItemType.Неизвестно)
                        throw new Exception("Номер не найден в задании!");

                    //проверить что номер  еще не обработался
                    foreach (ShippingJob.Product spr in shipSelectedProduct)
                    {
                        if (item.productId == spr.id)
                        {
                            //продукт найден выполняем требуемое
                            switch (type)
                            {
                                case ShippingQueryType.Add:
                                    //проверить что такой код ещ ене добавлен
                                    foreach (ShippingJob.Item it in spr.SelectedItems)
                                    {
                                        if (it.fullNumber == item.fullNumber)
                                        {
                                            //если такой номер уже есть вернуть GTIN для номера и признак
                                            errInfo = "ALRNUM*"+spr.gtin+"*";
                                            return null;
                                            
                                        }
                                        //проверить не вложен ли номер внутри уже обработанных контейнеров
                                        if (it.fullNumber == item.rootContainerNumber)
                                        {
                                            throw new Exception("Номер уже обработан в составе контейнера : "+item.rootContainerNumber);
                                            //errInfo = "ALRINCL*" + spr.gtin + "*"+ item.rootContainerNumber+"*";
                                            //return null;
                                        }
                                    }

                                    //сравнить добовляющееся количество чтоб оно не превышало 
                                    //нужное
                                    if (spr.number < (spr.allreadyNum + item.GetItemsQuantity()))
                                        throw new Exception("Количество добавляемого продукта превышает необходимое!");

                                    if (spr.Add(item))
                                        return spr;
                                    else
                                        return null;

                                    break;
                            }

                        }
                    }

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }
        *
        public ShippingJob.Product DeleteNumber(string number, string gtin, ShippingQueryType type, out string errInfo)
        {
            if (_rw == null)
                _rw = new ReaderWriterLockSlim();

            if (_rw.TryEnterWriteLock(500))
            {
                try
                {
                    errInfo = "Код не найден в задании";


                    //если элемент не найден закончить цыкл
                    if (number == " ")
                        throw new Exception("Номер не распознан!");

                    //найти номер в обработанных
                    foreach (ShippingJob.Product spr in shipSelectedProduct)
                    {
                        if (spr.gtin == gtin)
                        {
                            UnitItem itm = spr.GetItemAtNum(number);
                            if (itm != null)
                            {
                                spr.Remove(itm);
                                return spr;
                            }
                        }
                    }

                    // ShippingJob.Product
                }
                catch (Exception ex)
                {
                    errInfo = ex.Message;
                    return null;
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }

            errInfo = "Превышено время ожидания обработки на сервере! Попробуйте позднее.";
            return null;// new ShippingJob.Product("", 0, "");
        }

        public bool JobIsComplited()
        {
            if (JobState == JobStates.Complited)
                return true;

            return false;
        }



        #region SqlQuery
        private string ProductTableCreate = @"CREATE TABLE [ProductSqlA] (
          [productId] INTEGER NOT NULL
        , [id] TEXT NOT NULL
        , [gtin] NVARCHAR(20) NOT NULL
        , [addProdInfo] TEXT NOT NULL
        , [lotNo] TEXT NOT NULL
        , [expDate] TEXT NOT NULL
        , [productName] TEXT NOT NULL
        , [numРacksInBox] INTEGER NOT NULL
        , [numBoxesinPallet] INTEGER NOT NULL
        , [quantity] INTEGER NOT NULL
        , CONSTRAINT [PK_ProductSqlA] PRIMARY KEY ([productId])
        )";

        private string PalleteTableCreate = @"CREATE TABLE Pallets (
        palleteId    INTEGER NOT NULL
                          PRIMARY KEY AUTOINCREMENT,
        productId INTEGER NOT NULL,
        quantity      NVARCHAR(20) NOT NULL,
        rootCode  NVARCHAR(20) NOT NULL,
        numРacks  TEXT    NOT NULL,
        tp        INTEGER NOT NULL,
        st        INTEGER NOT NULL,
        FOREIGN KEY (productId)
        REFERENCES ProductSqlA (productId) ON DELETE NO ACTION
                                       ON UPDATE NO ACTION)";

        private string BoxesTableCreate = @"CREATE TABLE Boxes (
        boxId   INTEGER NOT NULL
                         PRIMARY KEY AUTOINCREMENT,
        palleteId  INTEGER NOT NULL,
        quantity      NVARCHAR(20) NOT NULL,
        rootCode  NVARCHAR(20) NOT NULL,
        numРacks TEXT    NOT NULL,
        tp       INTEGER NOT NULL,
        st       INTEGER NOT NULL,
        FOREIGN KEY (
            palleteId
        )
        REFERENCES Pallets (palleteId) ON DELETE NO ACTION
                                  ON UPDATE NO ACTION
        )";
        #endregion

        #region SqlFunc
        public string AcceptOrder(Shipping1СOrder o)
        {
            //пока отключить 
            return "";

            try
            {
                //создать подключение и файл базы
                string storedPath = "";

                storedPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\orders\\";

                //создать директорию для хранения текущих заданий если надо
                if (!System.IO.Directory.Exists(storedPath))
                    System.IO.Directory.CreateDirectory(storedPath);


                //создать директорию для хранения конкретно этого задания 
                storedPath += id;
                if (!System.IO.Directory.Exists(storedPath))
                    System.IO.Directory.CreateDirectory(storedPath);

                string dbFileName = storedPath + "\\" + id + ".sl3";
                if (File.Exists(dbFileName))
                    File.Delete(dbFileName);

                //создать и подключить файл данных
                SQLiteConnection m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                SQLiteCommand m_sqlCmd = new SQLiteCommand();
                SQLiteConnection.CreateFile(dbFileName);

                m_dbConn.Open();
                m_sqlCmd.Connection = m_dbConn;
                //создать таблицып 
                m_sqlCmd.CommandText = ProductTableCreate;
                m_sqlCmd.ExecuteNonQuery();

                m_sqlCmd.CommandText = PalleteTableCreate;
                m_sqlCmd.ExecuteNonQuery();

                m_sqlCmd.CommandText = BoxesTableCreate;
                m_sqlCmd.ExecuteNonQuery();

                m_sqlCmd.Connection.Close();
                m_sqlCmd.Dispose();

                m_dbConn.Close();
                m_dbConn.Dispose();

                using (var context = new DatabaseContext(dbFileName))
                {
                    for (int i = 0; i < order1C.product.Count; i++)
                    {
                        Shipping1СOrder.Product pr = order1C.product[i];
                        ProductSqlA prod = new ProductSqlA();
                        prod.productId = i;
                        prod.id = pr.id;
                        prod.gtin = pr.gtin;
                        prod.addProdInfo = pr.addProdInfo;
                        prod.lotNo = pr.lotNo;
                        prod.expDate = pr.ExpDate;
                        prod.productName = pr.productName;
                        prod.numРacksInBox = pr.numРacksInBox;
                        prod.numBoxesInPallet = pr.numBoxesInPallet;
                        prod.num = pr.quantity;

                        for (int iK = 0; iK < pr.palletsNumbers.Count; iK++)
                        {
                            Shipping1СOrder.Pallets pl = pr.palletsNumbers[iK];
                            ItemSqlP itemP = new ItemSqlP();
                            itemP.palleteId = iK;
                            itemP.productId = prod.productId;

                            itemP.code = pl.palletNumber;
                            itemP.rootCode = "";
                            itemP.numРacks = 0;
                            itemP.tp = SelectedItem.ItemType.Паллета;
                            itemP.st = CodeState.New;

                            prod.Items.Add(itemP);
                            //уровень короба
                            foreach (string boxNum in pl.boxNumbers)
                            {
                                ItemSqlB itemB = new ItemSqlB();
                                //itemB.boxId = null;
                                itemB.palleteId = itemP.palleteId;

                                itemB.code = boxNum;
                                itemB.rootCode = "";
                                itemB.numРacks = 0;
                                itemB.tp = SelectedItem.ItemType.Короб;
                                itemB.st = CodeState.New;

                                itemP.Items.Add(itemB);
                            }
                        }
                        context.Products.Add(prod);
                    }
                    context.SaveChanges();
                    context.Database.Connection.Close();
                    context.Dispose();
                }

                //db.Dispose();

            }
            catch (SQLiteException ex)
            {
                Log.Write("SqlLite новое состоние: " + ex.ToString());
            }
            catch (Exception ex)
            {
                Log.Write("SqlLite новое состоние: " + ex.ToString());
            }
            return "";
        }
        #endregion

    }
    */
    #endregion

    /*
     FSerialization.ItemSqlA: : EntityType 'ItemSqlA' has no key defined. Define the key for this EntityType.
Items: EntityType: EntitySet 'Items' is based on type 'ItemSqlA' that has no keys defined.
         */
    [Table("ProductSqlA")]
    public class ProductSqlA 
    {
        [Key]
        public long   productId     { get; set; }
        public string id            { get; set; }
        public string gtin          { get; set; }
        public string addProdInfo   { get; set; }
        public string lotNo         { get; set; }
        public string expDate       { get; set; }
        public string productName   { get; set; }
        public int    numРacksInBox { get; set; }
        public int    numBoxesInPallet { get; set; }
        public int    num           { get; set; }
        public virtual ICollection<ItemSqlP> Items { get; set; }
        public ProductSqlA()
        {
            id = "";
            gtin = "";
            lotNo = "";
            expDate = "";
            addProdInfo = "";
            Items = new List<ItemSqlP>();
        }
    }

    [Table("Pallets")]
    public class ItemSqlP 
    { 
        [Key]
        public long                 palleteId   { get; set; }//уникальный номер в базе слк
        public long                 productId   { get; set; }//ссылка на контейнер в который он вложен
        public virtual ProductSqlA  ProductSqlA { get; set; }
        //  public int id { get; set; }
        // public SelectedItem.ItemType inventLevel { get; set; }
        // public DateTime dt { get; set; }
        // public string oId { get; set; } //для инвентаризации номер продукта
        public string                rootCode   { get; set; }
        public string                code       { get; set; }
        public int                   numРacks   { get; set; } // количество упаковок в юните      
        public SelectedItem.ItemType tp         { get; set; } //тип елемента
        public CodeState             st         { get; set; }//состояние элемента

        public virtual ICollection<ItemSqlB> Items { get; set; }

        public ItemSqlP()
        {
            rootCode = "";
            Items = new List<ItemSqlB>();
        }
    }

    [Table("Boxes")]
    public class ItemSqlB
    {
        [Key]
        public long boxId { get; set; }//уникальный номер в базе слк
        public long palleteId { get; set; }//ссылка на контейнер в который он вложен
        public virtual ItemSqlP ItemSqlP { get; set; }
        //  public int id { get; set; }
        // public SelectedItem.ItemType inventLevel { get; set; }
        // public DateTime dt { get; set; }
        // public string oId { get; set; } //для инвентаризации номер продукта
        public string rootCode { get; set; }
        public string code { get; set; }
        public int numРacks { get; set; } // количество упаковок в юните      
        public SelectedItem.ItemType tp { get; set; } //тип елемента
        public CodeState st { get; set; }//состояние элемента
    }

   

}

namespace ExtensionMethods
{
    public static class MyExtensions
    {
        public static int WordCount(this String str)
        {
            return str.Split(new char[] { ' ', '.', '?' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
        }
        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>
        /// <returns>
        /// Whether the table was created or migrated.
        /// </returns>
      /*  public CreateTableResult CreateTable(this Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            var map = GetMapping(ty, createFlags);

            // Present a nice error if no columns specified
            if (map.Columns.Length == 0)
            {
                throw new Exception(string.Format("Cannot create a table without columns (does '{0}' have public properties?)", ty.FullName));
            }

            // Check if the table exists
            var result = CreateTableResult.Created;
            var existingCols = GetTableInfo(map.TableName);

            // Create or migrate it
            if (existingCols.Count == 0)
            {

                // Facilitate virtual tables a.k.a. full-text search.
                bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
                bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
                bool fts = fts3 || fts4;
                var @virtual = fts ? "virtual " : string.Empty;
                var @using = fts3 ? "using fts3 " : fts4 ? "using fts4 " : string.Empty;

                // Build query.
                var query = "create " + @virtual + "table if not exists \"" + map.TableName + "\" " + @using + "(\n";
                var decls = map.Columns.Select(p => Orm.SqlDecl(p, StoreDateTimeAsTicks));
                var decl = string.Join(",\n", decls.ToArray());
                query += decl;
                query += ")";
                if (map.WithoutRowId)
                {
                    query += " without rowid";
                }

                Execute(query);
            }
            else
            {
                result = CreateTableResult.Migrated;
                MigrateTable(map, existingCols);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                        throw new Exception("All the columns in an index must have the same value for their Unique property");

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                var columns = index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray();
                CreateIndex(indexName, index.TableName, columns, index.Unique);
            }

            return result;
        }*/
    }
}
