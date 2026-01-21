using BoxAgr.BLL.Collectors;
using BoxAgr.BLL.Controllers.Interfaces;
using BoxAgr.BLL.Events;
using BoxAgr.BLL.Exeptions;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using FluentFTP.Helpers;
using FSerialization;
using Peripherals;
using PharmaLegaсy.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media;
using Util;

namespace BoxAgr.BLL.Controllers
{

    public class BoxAssemblySerializeDioController : IBoxAssemblyController
    {
        private readonly System.ComponentModel.BackgroundWorker ScannerServerWorker = new();
        private TcpListener? _scannerServerListener; // Объект, принимающий TCP-клиентов
        private readonly IBoxRepository _boxRepository;
        private readonly IBusControl _modBus;
        private readonly IJob _job;
        private readonly ISystemState _systemState;
        private readonly IAppMsg _app;

        public event SessionStateEvent? StatusChange; // событие изменение статуса
        public event AddLayerEvent? AddLayer;
        public event MaxNoReadInlineStateEvent? MaxNoReadInlineState;

        private Dictionary<int, int> noreadInLineCounters = new();
        public bool ContiniusMode { get; }
        public int MaxNoReadInline { get; set; }

        public readonly int ServerPort;
        public static readonly int POINT_NUMBER = 1;
        public bool ScanEnable { get; set; }
        private readonly bool _packetLogenable;

        public BoxWithLayers cBox { get; set; } = new();
        public BoxAssemplyState BoxAssemplyState { get; set; } = BoxAssemplyState.None;

        public BoxAssemblySerializeDioController(IBoxRepository boxRepository, IBusControl modBusEvents, IJob job, ISystemState systemState, IAppMsg app,
            int scannerServerPort, int maxNoReadInline, bool continiusMode , bool packetLogenable)
        {
            _boxRepository = boxRepository;
            ServerPort = scannerServerPort;
            MaxNoReadInline = maxNoReadInline;
            _modBus = modBusEvents;
            _modBus.BoxInPosition += ModBus_BoxInPosition;
            _job = job;
            _app = app;
            this._systemState = systemState;
            ContiniusMode = continiusMode;
            _packetLogenable = packetLogenable;
        }

        /// <summary>
        /// Тригер короб вставлен. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void ModBus_BoxInPosition(object sender, bool state)
        {
            try
            {
                _modBus.OffGreenLight().ConfigureAwait(false);
               // _modBus.OffRedLight().ConfigureAwait(false);

                if (_systemState.StatusText == BoxAwaitConfirmMsg)
                    _app.ClearMsgBelt();
                
                //если короб убрали
                if (!state)
                {
                    //AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, BoxAddStatus.Uncknow, Array.Empty<Unit>(), cBox);
                    return;
                }


                if (_systemState.CriticalError)
                {
                    _systemState.StatusText = "Линия в ошибке! Устраните проблему для продолжения работы!";
                    _systemState.StatusBackground = Brushes.Red;
                    return;
                }
                //неделать ничего если нет задания
                if (_job.JobState == JobStates.Complited)
                {
                    _systemState.StatusText = "Нет задания для работы!";
                    _systemState.StatusBackground = Brushes.Red;
                    return;
                }

                //проверить если ли корректное задание?
                if (_job.JobState == JobStates.Empty)//if (Job.numРacksInBox < 1)
                {
                    _systemState.StatusText = "Нет задания для работы!";
                    _systemState.StatusBackground = Brushes.Red;
                    return;
                }
                //проверить запущено ли задание
                if (_job.JobState != JobStates.InWork)
                {
                    _systemState.StatusText = "Агрегация не начата !";
                    _systemState.StatusBackground = Brushes.Red;
                    return;
                }

                //проверить запушена ли линия c учетом счетчика останова
                if (_systemState.StopLine != false)
                {

                    _systemState.StatusText = "Короб установлен при состоянии \"линия не запущена\"!\nУберите короб, запустите агрегацию и снова установите короб для начала сканирования ";
                    _systemState.StatusBackground = Brushes.Red;
                    return;
                }

                //проверить полон ли короб если он полон ничего не делать пока не закроют короб
                if (cBox.NumbersCount == _job.numРacksInBox)
                {
                    _systemState.StatusText = "Верифицируйте предыдущий короб!";
                    _systemState.StatusBackground = Brushes.DarkOrange;
                    return;
                }

                //дать сигнал на скарен начать чтение
               // _modBus.StartScan().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Write("POC", ex.ToString(), EventLogEntryType.Error, 57);
            }

        }

        public void Start()
        {
            ScannerServerWorker.DoWork += delegate
            {
                try
                {
                    Thread.CurrentThread.Name = "PointOneServerWorker";

                    _scannerServerListener = new TcpListener(IPAddress.Any, ServerPort);
                    _scannerServerListener.Start(); // Запускаем его
                    Log.Write("POC", $"Сервер сканеров  точки  {POINT_NUMBER}  запущен как: {IPAddress.Any}:{ServerPort}", EventLogEntryType.Information, 2);
                    while (!ScannerServerWorker.CancellationPending)
                    {
                        // Принимаем нового клиента
                        ScanerAutoTcp sc = new(_scannerServerListener.AcceptTcpClient(), 10000) { Id = 1, RawPacketLogEnable = _packetLogenable };
                        sc.MessageRecieved += Sc_MessageRecieved;
                        sc.StatusChange += Sc_StatusChange;
                        
                        sc.Run();
                    }
                }
                catch (SocketException ex)
                {
                    Log.Write("POC", ex.Message, EventLogEntryType.Error, 3);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Log.Write("POC", $"Не возможно запустить сервер сканера порту: {ServerPort}\n" + ex.Message, EventLogEntryType.Information, 1);
                }
                catch (Exception ex)
                {
                    Log.Write("POC", ex.ToString(), EventLogEntryType.Error, 4);
                }
            };
            ScannerServerWorker.WorkerSupportsCancellation = true;
            ScannerServerWorker.RunWorkerAsync();

        }
        public bool AddSingleCodeToLayer(string data)
        {
            //если работа запрещена просто игнорируем данные зажигая красный
            if (!ScanEnable)
            {
                _app.ShowMessageOnUpBanner("BAC", "Программа в режиме Стоп, данные проигнорированны!", EventLogEntryType.Error, 10031);
                _modBus.StartRedBlink();
                return false;
            }

            try
            {
                //проверить код
                PreCheckBoxState(1);

                List<Unit> layer = new List<Unit>(1);
                var rnv = CodeVerify(_job.GTIN, data);
                Unit u = new() { Barcode = data, Number = rnv.Number, CodeState = rnv.codeState };

                //разрешаем повторы в задании. НЕ в коробе
                VerifyCodeInPart(layer.Any(x => x.Barcode == u.Barcode), u, allowRepitInJob: true);


                //НЕ! допустить повтор номера в слое если такое случается не добавлять ео повторно
                //проверить на повтор в текущем слое
                if (layer.Any(x => x.Barcode == u.Barcode))
                    throw new BoxInfoExeption($"Код не добавлен так как он уже присутствует в слое\n Повтор кода: {u.Barcode}",
                        Array.Empty<Unit>().ToList());

                //если статус не годен то сформировать сообщение 
                if (u.CodeState != CodeState.Verify)
                    _app.ShowMessageOnUpBanner("BAC", $"Продукт не может быть добавлен!\n {GetUnitStateInfo(u)}", EventLogEntryType.Error, 10021);
                else
                {
                    //присвоить статус хорошего добавленного вручную
                    u.CodeState = CodeState.ManualAdd;
                    layer.Add(u);
                    //собрать слой 
                    cBox.AddUnitsToAssembledLayer(layer);
                    _app.ShowMessageOnUpBanner("BAC", $"Продукт с номером {u.Barcode} добавлен.", EventLogEntryType.Information, 10034);

                    if (cBox.cLayer.Count == _job.numPacksInLayer)
                    {
                        var lCopy = cBox.cLayer.ToArray();
                        cBox.CloseAssembledLayer();
                        //_modBus.StartGreenSpot();  //зажеч гин спот
                        _modBus.OnGreenLight();

                        //если короб набран закрыть его
                        if (cBox.NumbersCount == _job.numРacksInBox)
                        {
                            cBox.State = BoxWLState.Closed;
                            AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, true, BoxAddStatus.BoxFull, lCopy, cBox);
                        }
                        else
                            AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, true, BoxAddStatus.LayerFull, lCopy, cBox);
                    }
                    else
                    {
                        AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, true, BoxAddStatus.PartsOfLayer, layer.ToArray(), cBox);
                    }
                    return true;
                }
            }
            catch (BoxInfoExeption ex)
            {
                _app.ShowMessageOnUpBanner("BAC", ex.Message, EventLogEntryType.Error, 10021);
            }
            catch (Exception ex)
            {
                _app.ShowMessageOnUpBanner("BAC", ex.Message, EventLogEntryType.Error, 10021);
            }
            return false;
        }

        private static readonly object syncObjState = new();
        private void Sc_StatusChange(int id, PeripheralsType type, SessionStates data)
        {
            lock (syncObjState)
                StatusChange?.Invoke(id, type, data);
        }

        public static UnitPoint[] ConvertToUnitPoints(string[] input)
        {
            List<UnitPoint> unitPoints = new List<UnitPoint>();

            foreach (var str in input)
            {
                string[] parts = str.Split(',');
                if (parts.Length == 2 && Int32.TryParse(parts[0], out int x) && Int32.TryParse(parts[1], out int y))
                {
                    unitPoints.Add(new UnitPoint { X = x, Y = y });
                }
                else
                {
                    throw new ArgumentException($"Invalid input string: {str}");
                }
            }

            return unitPoints.ToArray();
        }

        private static readonly object syncObjMsg = new();
        private readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
        private readonly string BoxAwaitConfirmMsg =
                 "Чтение нового короба заблокировано, короб не отправлен в термоупаковку.\nЗапустите ее или нажмите \"Стоп\".\nБлокировка чтения нового короба снимется, но считанный короб останется в результате.";
           
        private void Sc_MessageRecieved(object sender, int id, string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            lock (syncObjMsg)
            {
                _sw.Reset();
                _sw.Start();
                string[] barcodes = data.Split('\t', StringSplitOptions.RemoveEmptyEntries);

                try
                {

                    if (_systemState.CriticalError)
                    {
                        _modBus.StartRedBlink();
                        _systemState.StatusText = "Линия в ошибке! Устраните проблему для продолжения работы!";
                        _systemState.StatusBackground = Brushes.Red;
                        return;
                    }
                    //неделать ничего если нет задания
                    if (_job.JobState == JobStates.Complited)
                    {
                        _modBus.StartRedBlink();
                        _systemState.StatusText = "Нет задания для работы!";
                        _systemState.StatusBackground = Brushes.Red;
                        return;
                    }

                    //проверить если ли корректное задание?
                    if (_job.JobState == JobStates.Empty)
                    {
                        _modBus.StartRedBlink();
                        _systemState.StatusText = "Нет задания для работы!";
                        _systemState.StatusBackground = Brushes.Red;
                        return;
                    }
                    //проверить запущено ли задание
                    if (_job.JobState != JobStates.InWork)
                    {
                        _modBus.StartRedBlink();
                        _systemState.StatusText = "Агрегация не начата !";
                        _systemState.StatusBackground = Brushes.Red;
                        return;
                    }


                    //если работа запрещена просто игнорируем данные зажигая красный
                    if (!ScanEnable)
                    {
                        _modBus.StartRedBlink();
                        return;
                    }   
                    
                    if (_modBus.IsGreenLightActive)
                    {
                        _modBus.StartRedBlink();                       
                        _app.ShowMessageOnUpBanner("BAC", BoxAwaitConfirmMsg, EventLogEntryType.Error, 10021);
                        return;
                    }

                    //добавить счетчик если надо
                    if (!noreadInLineCounters.TryGetValue(id, out int noReadCounter))
                        noreadInLineCounters.Add(id, 0);

                    if (data.Equals("NR", StringComparison.OrdinalIgnoreCase))
                    {
                        noreadInLineCounters[id]++;
                        if (noreadInLineCounters[id] > MaxNoReadInline)
                            MaxNoReadInlineState?.Invoke(id, noreadInLineCounters[id]);

                        //если короб еще стоит запустить новый цикл сканирования если короба нет то зажеч красный
                        //if (_modBus.IsBoxInPosition && ContiniusMode)
                        //    _modBus.StartScan();
                        //else
                            _modBus.StartRedBlink();

                        AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.NoRead, Array.Empty<Unit>(), cBox);
                        return;
                    }

                    //сбросить счетчик норидов
                    if (noReadCounter > 0)
                        noreadInLineCounters[id] = 0;

                    //для сериализации принцип: новый скан = новый короб. поетому чистим все что было в предыдущем коробе
                    if (cBox != null)
                    {
                        cBox.ClearAssembledLayer();
                        cBox.ClearBox();
                    }

                   //проверка допустимости добавления слоя
                    PreCheckBoxState(barcodes.Length);

                    if (cBox.cLayer.FirstOrDefault(x => x.CodeState == CodeState.ManualAdd) is not null)
                        throw new BoxInfoExeption($"Нельзя добавить слой с автоматической камеры если уже добавлены  подукты вручную.\nНажмите кнопку \"Очистить короб\" считайте слой автоматичекой камерой и потом добавьте продукты ручным сканером.", new List<Unit>());


                    //верифицировать номера м создать слой
                    List<Unit> layer = new List<Unit>(barcodes.Length);
                    foreach (string s in barcodes)
                    {
                        try
                        {
                            //Обработка структцры пакета от HIK
                            Unit u = new();
                            string[] r = s.Split('\u000B');
                            if (r.Length == 2)
                            {

                                string[] coordinats = r[0].RemovePrefix("(")
                                    .RemovePostfix(")").Split(")(");

                                if(coordinats.Length != 4)
                                    throw new BoxInfoExeption("Ошибка формата пакета от сканера", layer);

                                string[] xy = coordinats[3].Split(',');
                                string[] xyEnd = coordinats[2].Split(',');

                                if(ConvertToUnitPoints(coordinats) is UnitPoint[] points)
                                    u.Points.AddRange(points);

                                u.X = Convert.ToInt16(xy[0]);
                                u.Y = Convert.ToInt16(xy[1]);

                                u.Width = Convert.ToInt16(xyEnd[0]) - u.X;

                                u.Barcode = r[1];
                                var rnv = CodeVerify(_job.GTIN, u.Barcode);
                                u.CodeState = rnv.codeState;
                                u.Number = rnv.Number;

                                //разрешаем повторы в задании. НЕ В КОРОБЕ!
                                VerifyCodeInPart(layer.Any(x => x.Barcode == u.Barcode), u, allowRepitInJob: true);

                                //допустить повтор номера в слое если такое случается не добавлять ео повторно
                                //проверить на повтор в текущем слое
                                if (cBox.cLayer.Any(x => x.Barcode == u.Barcode))
                                    u.CodeState = CodeState.ProductRepit;
                                else
                                    layer.Add(u);
                            }
                            else
                                throw new BoxInfoExeption("Ошибка формата пакета от сканера", layer);
                        }
                        catch (Exception e) when (e is ArgumentException || e is ArgumentOutOfRangeException || e is FormatException)
                        {
                            throw new Exception("Ошибка распознавания координат", e);
                        }
                    }


                    //добавить слой только если все коды в норме
                    if (!layer.Any(x => x.CodeState != CodeState.Verify) && (layer.Count + cBox.cLayer.Count) <= _job.numPacksInLayer)
                    {
                        //собрать слой 
                        cBox.AddUnitsToAssembledLayer(layer);

                        if (cBox.cLayer.Count == _job.numPacksInLayer)
                        {

                            cBox.CloseAssembledLayer();
                            //_modBus.StartGreenSpot();  //зажеч гин спот
                            _modBus.OnGreenLight();

                            //если короб набран закрыть его
                            if (cBox.NumbersCount == _job.numРacksInBox)
                            {
                                cBox.State = BoxWLState.Closed;
                                AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.BoxFull, layer.ToArray(), cBox);
                            }
                            else
                                AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.LayerFull, layer.ToArray(), cBox);
                        }
                        else
                        {
                            AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.PartsOfLayer, layer.ToArray(), cBox);

                            //если короб еще стоит запустить новый цикл сканирования если короба нет то зажеч красный
                            //if (_modBus.IsBoxInPosition && ContiniusMode)
                            //    _modBus.StartScan();
                            //else
                                _modBus.StartRedBlink();

                        }
                        return;
                    }

                    //вывести ошибку по первому попавшемуся коду с ошибкой в слое
                    if (layer.FirstOrDefault(x => x.CodeState != CodeState.Verify) is Unit ue)
                    {
                        _app.ShowMessageOnUpBanner("BAC", $"Слой не может быть добавлен!\n {GetUnitStateInfo(ue)}", EventLogEntryType.Error, 10021);
                    }
                    else
                    {
                        //вывести ошибку по превышению кодов в слое
                        if ((layer.Count + cBox.cLayer.Count) > _job.numPacksInLayer)
                            _app.ShowMessageOnUpBanner("BAC", $"Слой не может быть добавлен! Превышен максимум продуктов в одном слое.\nВсего кодов продукта распознано в слое: {(layer.Count + cBox.cLayer.Count)}.  Максимум {_job.numPacksInLayer} ", EventLogEntryType.Error, 10021);
                    }

                    //запустить вывод матрицы с плохими кодами
                    AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.Defected, layer.ToArray(), cBox);
                    _modBus.StartRedBlink();
                }
                catch (BoxInfoExeption ex)
                {
                    AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.LogicError, ex.Layer.ToArray(), cBox);
                    _app.ShowMessageOnUpBanner("BAC", ex.Message, EventLogEntryType.Error, 10021);
                    _modBus.StartRedBlink();
                }
                catch (Exception ex)
                {
                    AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, false, BoxAddStatus.LogicError, Array.Empty<Unit>(), cBox);
                    _app.ShowMessageOnUpBanner("BAC", ex.Message, EventLogEntryType.Error, 10021);
                    _modBus.StartRedBlink();
                }
                finally
                {
                    _sw.Stop();
                    Log.Write($"Sc_MessageRecieved: {_sw.ElapsedMilliseconds}");
                }
            }
        }
        public void StopCycle()
        {
            //отключить зеленый свет
            if(_modBus.IsGreenLightActive)
                _modBus.OffGreenLight();
        }

        private void VerifyCodeInPart(bool RepitInlayer, Unit u,bool allowRepitInJob)
        {
            if (u.CodeState != CodeState.Verify)
                u.CodeState = CodeState.ProductWrongGtin;

            //проверить на повтор в слое
            if (u.CodeState == CodeState.Verify && RepitInlayer)
                u.CodeState = CodeState.ProductRepit;



            //проверить на повтор по БД
            if (u.CodeState == CodeState.Verify && _boxRepository.GetUnitByBarcode(u.Barcode) is Unit ru)
            {
                if (ru.CodeState == CodeState.Verify && !allowRepitInJob)
                {
                    u.CodeState = CodeState.ProductRepit;
                    u.StatusInfo = $"Номер {u.Number} уже присутствует в коробе {ru.BoxNumber}";
                }
                else
                    u.CodeState = ru.CodeState;
            }

            //проверить по списку разрешенных в серии
            if (_job.order1C?.productNumbers?.Count > 0)
            {
                if (!_job.order1C.productNumbers.Exists(x => x == u.Number))
                    u.CodeState = CodeState.Missing;
            }
        }
        private void PreCheckBoxState(int barcodesCount)
        {
            //если в пакете больше номеров чем надо
            if (barcodesCount > _job.numPacksInLayer)
                throw new BoxInfoExeption($"Количество номеров в слое больше допустимого! Считано: {barcodesCount}. Максимум в слое:{_job.numPacksInLayer}", new List<Unit>());

            //если короб не создан создать его
            if (cBox is null)
                throw new BoxInfoExeption($"Нет номера короба для работы. ", new List<Unit>());

            //если короб не создан создать его
            if (cBox.State == BoxWLState.Uncknow)
                throw new BoxInfoExeption($"Нет номера короба для работы. ", new List<Unit>());

            //если короб в состоянии верификации
            if (_job.GetVerufyQueueSize() > 0)
                throw new BoxInfoExeption($"Нельзя начать новый короб пока предыдущий короб ждет верификацию номера.\nСчитайте ручным сканером номер предыдущего короба.", new List<Unit>());
        }

        public bool RemoveUnitFromLayer(string fullNumber)
        {
            if (cBox.cLayer.RemoveAll(x => x.Barcode == fullNumber) > 0)
            {
                AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, true, BoxAddStatus.PartsOfLayer, cBox.cLayer.ToArray(), cBox);
                return true;
            }

            return false;
        }
        public bool RemoveUnitFromBox(string fullNumber)
        {
            if (cBox.Numbers.RemoveAll(x => x.Barcode == fullNumber) > 0)
            {
                //AddLayer?.Invoke(POINT_NUMBER, cBox.LayerNum, BoxAddStatus.PartsOfLayer, cBox.cLayer.ToArray(), cBox);
                return true;
            }

            return false;
        }

        public bool ReplaceNumInBox(string removeNum, string newfullNumber)
        {
            if (cBox.Numbers.RemoveAll(item => item.Barcode == removeNum) < 1)
                return false;

            GsLabelData ld = new(newfullNumber);
            cBox.Numbers.Add(new Unit() { Number = ld.SerialNumber, Barcode = newfullNumber });
            return true; ;
        }

        private static string GetUnitStateInfo(Unit u)
        {
            switch (u.CodeState)
            {
                case CodeState.ProductWrongGtin:
                    return $"Номер {u.Barcode} имеет GTIN не совпадающий с заданием";
                case CodeState.ProductRepit:
                    if (!string.IsNullOrEmpty(u.StatusInfo))
                        return u.StatusInfo;
                    return $"Повтор номера {u.Number}";
                case CodeState.Missing:
                    return $"Номер {u.Barcode} отсутствует в списке разрешенных";
                default:
                    return $"{u.Barcode} - {u.CodeState}";
            }
        }
        private static (CodeState codeState, string Number) CodeVerify(string Gtin, string barcode)
        {
            GsLabelData sc = new(barcode);


            if (string.IsNullOrEmpty(sc.SerialNumber))
                return (CodeState.ProductNumIsNotGS1, "");



            if (string.IsNullOrEmpty(sc.CryptoHash))
                return (CodeState.ProductNumIsNotGS1, sc.SerialNumber);

            if (string.IsNullOrEmpty(sc.GTIN))
                return (CodeState.ProductNumIsNotGS1, sc.SerialNumber);

            if (!sc.GTIN.Equals(Gtin, StringComparison.OrdinalIgnoreCase))
                return (CodeState.ProductWrongGtin, sc.SerialNumber);



            return (CodeState.Verify, sc.SerialNumber);

        }
    }
}
