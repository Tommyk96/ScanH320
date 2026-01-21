// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel;
//using System.Runtime.Serialization.Json;
using Util;

namespace FSerialization
{

    #region BaseClasses
    public delegate void JobChangeEventHandler(object sender, bool needSafe);

    public class PalleteRezervCodes
    {
        public static readonly string PalletIDForUnpaletBox = "000000000000000000";
        public static readonly string PalletIDForUnboxedPack = "00000000000000000A";
    }
    [DataContract]
    public class PrintTemplate
    {
        [DataMember]
        public byte[] data;
    }
    public enum CodeState
    {
        Missing = 0,
        New = 1,
        Printed = 2,
        InWorck = 3,
        Verify = 4,
        Bad = 5,
        Sample = 6,
        Moved = 7,
        Invent = 8,
        WrongLot = 9,
        ProductNumIsNotGS1 = 10,
        ProductWrongGtin = 11,
        ProductRepit = 12,
        ManualAdd = 13

    }
    public enum JobStates
    {
        Empty = 0,
        New = 1,
        InWork = 2,
        Paused = 3,
        CloseAndAwaitSend = 4,
        WaitSend = 5,
        SendInProgress = 6,
        Complited = 7,
        Closes = 8,
        InWorkNoNum=9,
        BoxesReadyWaitPalete=10

    }
    public enum JobIcon
    {
        Default = 0,
        ErrorSended = 1,
        JobInWork = 2
    }

    [Serializable]
    public class JobFormatException : Exception
    {
        public JobFormatException(string message) : base(message) { }
    }
    [Serializable]
    public class SendReportException : Exception
    {
        public SendReportException(string message) : base(message) { }
    }

    [DataContract]
    public class OrderMeta
    {
        [DataMember]
        public string id;

        [DefaultValue("")]
        [DataMember]
        public string name;

        [DataMember]
        public JobIcon state;

        [DataMember]
        public int type;

        [DataMember]
        public string path;

        public OrderMeta Copy()
        {
            OrderMeta r = new OrderMeta();
            r.id = id;
            r.name = name;
            r.state = state;
            r.type = type;
            r.path = path;
            return r;
        }
    }

    /// Базовый класс для всех заданий в работе
    public interface IBaseJob
    {
        OrderMeta JobMeta { get; set; }

        JobStates JobState { get; set; }

        bool JobIsAwaible { get; }

        object GetTsdJob();

        object GetTsdSqLiteJob();

        bool WaitSend { get; }

        string ParceReport<T>(T rep);

        string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut,bool repeat);

        object GetReport();

        string GetFuncName();

    }
    //базовый класс с описанием общих реквизитов
    [DataContract]
    public class BaseJobInfo : Base1COrder
    {
        [DataMember]
        public string GTIN { get; set; }
       // [DataMember]
       // public string addProdInfo { get; set; }
        [DataMember]
        public string lotNo { get; set; }

        public DateTime _expDate;

        [DataMember]
        private string expDate
        {
            get { return _expDate.ToString("yyyy-MM-ddThh:mm:ssz"); }//yyMMdd
            set
            {
                if (value != null)
                {
                    if (value.Length == 6)
                    {
                        String dFormat = "yyMMdd";
                        _expDate = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                    }
                    else
                        _expDate = DateTime.Parse(value);
                }
                else
                    _expDate = DateTime.MinValue;

                strExpDate = _expDate.ToString("yyMMdd");
            }
        }

        private string strExpDate;
        [DataMember]
        public string ExpDate 
        {
            get { return strExpDate; }
            set {

                if (value != null)
                {
                    if (value.Length == 6)
                    {
                        String dFormat = "yyMMdd";
                        _expDate = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                    }
                    else
                        _expDate = DateTime.Parse(value);
                }
                else
                    _expDate = DateTime.MinValue;
                strExpDate = value;
            }
        }

        [DataMember]
        private string jType = "Nullable";
        public Type jobType { get { return Type.GetType(jType); } set { jType = value.ToString(); } }

        public virtual Type GetJobType() { return jobType; }
        public virtual string InitJob<T>(T order, string user, string pass) { return "NotImplemented"; }
        public event JobChangeEventHandler JobChange;
        public void SafeToDisk()
        {
            JobChange?.Invoke(this, true);
        }

    }

    [DataContract]
    public class Operator
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public int type = 1;
        [DataMember]
        public string startTime { get; set; } //= DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public string endTime { get; set; }

        public Operator():this("") { }
        public Operator(string _id) : this(_id, DateTime.Now, 1) { }
        public Operator(string _id, int t) : this(_id, DateTime.Now, t) { }
        public Operator(string _id, DateTime _st, int _t)
        {
            id = _id;
            startTime = _st.ToString("yyyy-MM-ddThh:mm:ssz");
            type = _t;
        }
    }

    [DataContract]
    public class OperatorRep
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string startTime { get; set; } //= DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public string endTime { get; set; }

        public OperatorRep() : this("") { }
        public OperatorRep(string _id) : this(_id, DateTime.Now, 1) { }
        public OperatorRep(string _id, int t) : this(_id, DateTime.Now, t) { }
        public OperatorRep(string _id, DateTime _st, int _t)
        {
            id = _id;
            startTime = _st.ToString("yyyy-MM-ddThh:mm:ssz");
        }
        public OperatorRep(Operator o)
        {
            id = o.id;
            startTime = o.startTime;
            endTime = o.endTime;
        }

        //public assadasdasd
    }

    public class Operators : List<Operator>
    {
        public bool CloseLastSesion()
        {
            if (Count < 1)
                return false;

            this[Count - 1].endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
            return true;
        }
    }



    public class BoxContainer
    {
        public BoxContainer()
        {
        }

        [DataMember]
        public string Number { get; set; }

        [DataMember]
        public string startTime { get; set; }

        [DataMember]
        public string endTime { get; set; }

        [DataMember]
        public string ContainerType { get; set; }

        [DataMember]
        public List<Box> boxes = new List<Box>();

        public bool IsPackAlreadyInContainer(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.IsAlreadyInBox(number))
                    return true;
            }
            return false;
        }
        public bool IsBoxAlreadyInContainer(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.Number == number)
                    return true;
            }
            return false;
        }

        public bool RemoveBox(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.Number == number)
                {
                    boxes.Remove(s);
                    return true;
                }
            }

            return false;
        }
    }


    [DataContract]
    public class Box
    {
        public enum State
        {
            New = 0,
            Printed = 1,
            Verify = 2,
            Uncknow = 3,
        }

        public State state = new State();

        [DataMember]
        public string Number = "";
        [DataMember]
        public List<string> packNumbers = new List<string>();

        public bool IsAlreadyInBox(string number)
        {
            foreach (string s in packNumbers)
            {
                if (s == number)
                    return true;
            }

            return false;
        }
        public bool RemovePack(string number)
        {
            foreach (string s in packNumbers)
            {
                if (s == number)
                {
                    packNumbers.Remove(s);
                    return true;
                }
            }

            return false;
        }
    }


    [DataContract]
    public class LabelField
    {
        public LabelField(string k, string d)
        {
            FieldName = k;
            FieldData = d;
        }

        [DataMember]
        public string FieldName;
        [DataMember]
        public string FieldData;
    }

    public enum NumberState
    {
        Доступен = 0,
        Собирается = 1,
        Верифицирован = 2,
        VerifyAndPlaceToPalete =3,
        VerifyAndPlaceToReport=4,
        Забракован=5,
        ВерифицированУпакован = 6
    }


    [DataContract]
    public class DefectiveCode
    {
        public DefectiveCode() : this("", "") { }
        public DefectiveCode(string i, string n)
        {
            id = i;
            number = n;
        }
        [DataMember]
        public string number { get; set; }

        [DataMember]
        public string id { get; set; }
    }

    [DataContract]
    public class ReadyBox
    {
        // [DataMember]
        public DateTime date;


        public string _boxNumber = "";

        [DataMember]
        public string boxNumber
        {
            get { return _boxNumber; }
            set { _boxNumber = value; }
        }


        [DataMember]
        public List<string> productNumbers = new List<string>();

        [DataMember]
        public string boxTime
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set { date = DateTime.Parse(value); }
        }

        [DataMember]
        public string id { get; set; }

        public ReadyBox()
        {
            id = "";
        }

        public ReadyBox(ReadyBox o)
        {
            boxNumber = o.boxNumber;
            boxTime = o.boxTime;
            id = o.id;
            productNumbers.AddRange(o.productNumbers);
        }
        public bool IsAlreadyInBox(string number)
        {
            foreach (string s in productNumbers)
            {
                if (s == number)
                    return true;
            }

            return false;
        }
        public bool RemovePack(string number)
        {
            foreach (string s in productNumbers)
            {
                if (s == number)
                {
                    productNumbers.Remove(s);
                    return true;
                }
            }

            return false;
        }

        public bool AddLayer(List<string> layerCodes)
        {

            return false;
        }
    }

    [DataContract]
    public class UnitItem
    {
        public int id;
        /// <summary>
        /// inventLevel продукта. что бы это ни значило!
        /// </summary>
        public SelectedItem.ItemType invLv;
        /// <summary>
        /// gtin продукта 
        /// </summary>
        [DataMember]
        public string gtin { get; set; }
        [DataMember]
        public DateTime dt;
        /// <summary>
        /// Ид продукта не gtin! для инвентаризации номер продукта,для упаковки палет Ид пользователя!
        /// для отгрузки это просто порядковый номер продукта
        /// </summary>
        [DataMember]
        public string oId { get; set; }
        /// <summary>
        /// номер юнита. серийник или sscc18
        /// </summary>
        [DataMember]
        public string num { get; set; }
        /// <summary>
        /// количество упаковок в юните. например количество упаковок в коробе.
        ///  для палеты ???
        /// </summary>
        [DataMember]
        public int qP;
        /// <summary>
        /// количество упаковок в юнитах вложенных в этот юнит
        /// </summary>
        [DataMember]
        public int qIp;
        /// <summary>
        /// тип контейнера коробка . палета. короб .
        /// </summary>
        [DataMember]
        public SelectedItem.ItemType tp = SelectedItem.ItemType.Неизвестно;
        /// <summary>
        /// состояние элемента
        /// </summary>
        [DataMember]
        public CodeState st; //
        /// <summary>
        /// Номер палеты на которой лежит контейнер 
        /// </summary>
        [DefaultValue("")]
        [DataMember]
        public string pN { get; set; }

        [DataMember]
        public List<UnitItem> items = new List<UnitItem>();
        /// <summary>
        /// Объект в который вложен этот елемент
        /// </summary>
        private UnitItem root = null;



        public UnitItem() : this("") { }
        public UnitItem(string _code) : this(_code, SelectedItem.ItemType.Неизвестно) { }
        public UnitItem(string _code, SelectedItem.ItemType _tp) : this(_code,"", _tp, CodeState.New, 0) { }
        public UnitItem(string _code, string _gtin, SelectedItem.ItemType _tp) : this(_code, _gtin, _tp,  CodeState.New, 0) { }
        public UnitItem(string _code, SelectedItem.ItemType _tp,int _quantity) : this(_code, "", _tp, CodeState.New, 0) { qP = _quantity; }
        public UnitItem(string _code, string _gtin, SelectedItem.ItemType _tp, CodeState _st, int _id) { num = _code;gtin = _gtin; tp = _tp; st = _st; dt = DateTime.MinValue.ToUniversalTime(); id = _id; }

        public UnitItem(System.Data.DataRow row)
        {
            try
            {
                if (row[1].GetType() != typeof(DBNull))
                    num = row[1].ToString();

                if (row[2].GetType() != typeof(DBNull))
                    id = Convert.ToInt32(row[3]);

                if (row[4].GetType() != typeof(DBNull))
                    tp = (SelectedItem.ItemType)Convert.ToInt32(row[4]);

                if (row[5].GetType() != typeof(DBNull))
                    st = (CodeState)Convert.ToInt32(row[5]);

            }
            catch (Exception ex) { dt = DateTime.MinValue.ToUniversalTime(); ex.ToString(); }
        }
        //Проверить есть ли указанный в quantity код внутри этого контейнера
        public UnitItem CodeAlreadyInUnit(string code)
        {
            foreach (UnitItem u in items)
            {
                if (u.num == code)
                    return u;

                UnitItem u1 = u.CodeAlreadyInUnit(code);

                if (u1 != null)
                {
                    u1.root = u;
                    return u1;
                }
            }
            return null;
        }
        //вернуть первый юнит с указанным статусом
        public UnitItem GetFirstCodeFromUnitOneLevelAtStatus(CodeState _st)
        {
            foreach (UnitItem u in items)
            {
                if (u.st == _st)
                    return u;
                /*
                UnitItem u1 = u.GetFirstCodeFromUnitAtStatus(_st);
                if (u1 != null)
                    return u1;*/
            }
            return null;
        }
        //Удалить елемент с номером указанными в quantity из этого контейнера
        public bool RemoveItemFromUnit(string code)
        {

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].num == code)
                    return items.Remove(items[i]);

                if (items[i].RemoveItemFromUnit(code))
                    return true;
            }
            return false;
        }
        //установить статус юниту с указанным кодом
        //Проверить есть ли указанный в quantity код внутри этого контейнера
        public UnitItem SetUnitStatus(string _code, CodeState _st)
        {
            if (num == _code)
            {
                st = _st;
                return this;
            }

            foreach (UnitItem u in items)
            {
                if (u.num == _code)
                {
                    u.st = _st;
                    return u;
                }

                UnitItem u1 = u.SetUnitStatus(_code, _st);
                if (u1 != null)
                    return u1;
            }
            return null;
        }
        //возвращает количество унитов к в контейнере и в подконтейнерах с указанным статусом
        public int GetUtinCountAtStatusAndType(CodeState _st, SelectedItem.ItemType[] _tp)
        {
            int result = 0;
            foreach (SelectedItem.ItemType t in _tp)
            {
                if ((st == _st) && (tp == t))
                {
                    result++;
                    break;
                }
            }

            foreach (UnitItem u in items)
            {
                result += u.GetUtinCountAtStatusAndType(_st, _tp);
            }
            return result;
        }
        //пробует установить указанныц статус для элемента
        public UnitItem TrySetNumberStatus(string _code, CodeState _st)
        {
            if (num == _code)
            {
                //проверить доступно ли установление требуемого статуса
                switch (_st)
                {
                    case CodeState.Verify:
                        if (st != CodeState.New)
                            throw new Exception("Статус не может быть установлен.");
                        break;
                }

                st = _st;
                return this;
            }

            foreach (UnitItem u in items)
            {
                UnitItem u1 = u.TrySetNumberStatus(_code, _st);
                if (u1 != null)
                    return u1;
            }
            return null;
        }
        //возвращает общее количество пачек вложенных в юнит и и контейнеры 
        public int GetItemsQuantity()
        {
            int itemNum = -1;
            switch (tp)
            {
                case SelectedItem.ItemType.Паллета:
                    itemNum = qP;// * items.Count;
                    break;
                case SelectedItem.ItemType.Короб:
                    itemNum = qP;
                    break;
                case SelectedItem.ItemType.Упаковка:
                    itemNum = 1;
                    break;
            }

            return itemNum;
        }
        //скопировать элемент
        public UnitItem Clone()
        {
            UnitItem r = new UnitItem();
            r.id = this.id;
            r.invLv = this.invLv;
            r.gtin = this.gtin;
            r.dt = this.dt;
            r.oId = this.oId;
            r.num = this.num;
            r.qP = this.qP;
            r.qIp = this.qIp;
            r.tp = this.tp;
            r.st = this.st;
            r.root = this.root;
            r.pN = this.pN;
            //
            foreach (UnitItem i in this.items)
                r.items.Add(i.Clone());

            return r;
        }
        //методы скрывают переменную root 
        //от сериалайзера json для избежания колец на себя
        public void SetRoot(UnitItem u) { root = u; }
        public UnitItem GetRoot() { return root; }

        public UnitItemM GetUnitItemM(string _gtin)
        {
            UnitItemM r = new UnitItemM(gtin, (int)tp, num);
            if (tp == SelectedItem.ItemType.Упаковка)
                r.gtin = _gtin;
            else
                r.gtin = "";

            foreach (UnitItem ui in items)
                r.items.Add(ui.GetUnitItemM(_gtin));

            return r;
        }

        public UnitItemM GetUnitItemM()
        {
            UnitItemM r = new UnitItemM(gtin, (int)tp, num);
            if (tp == SelectedItem.ItemType.Упаковка)
                r.gtin = gtin;
            else
                r.gtin = "";

            foreach (UnitItem ui in items)
                r.items.Add(ui.GetUnitItemM());

            return r;
        }
    }

    /// <summary>
    /// Класс для компактного сохранения данных UnitItem в json 
    /// компактность за счет отказа от большинства полей
    /// </summary>
    [DataContract]
    public class UnitItemM
    {
        [DefaultValue("")]
        [DataMember]
        public string gtin { get; set; }
        [DataMember]
        public int type;
        [DataMember]
        public string num { get; set; }
        [DataMember]
        public List<UnitItemM> items = new List<UnitItemM>();

        public UnitItemM(string _gtin, int _type, string _num)
        {
            gtin = _gtin;
            type = _type;
            num = _num;
        }
    }


    [DataContract]
    public class Base1COrder
    {
        [DataMember]
        public string id;

        [DataMember]
        public string createDateTime;
       
        public virtual bool CheckContent() { return false; }

    }

    /// <summary>
    /// Класс хранения данных о подобранном элементе. ето тип,номер, и способ  добавления
    /// используется в списании. отгрузке. приемке.
    /// </summary>
    [DataContract]
    public class SelectedItem
    {
        public enum ItemType
        {
            Упаковка = 0,
            Короб = 1,
            Паллета = 2,
            Неизвестно = 3
            /*,
       ПеремещеннаяПаллета = 11,
        ПеремещенныйКороб = 12,
        ПеремещеннаяУпаковка = 13*/
        }

        [DataMember]
        public ItemType type;//0-def,1-pallete,2-box,3-коробка
        [DataMember]
        public CodeState st;//состояние кода передвинут. напечатан итд..
        [DataMember]
        public string fullNumber;
        [DataMember]
        public int numberItemInPack;
        //количество пачек в объекте.ПАЧЕК!!!НЕ КОРОБОК!!**ВНИМАНИЕ!!! КАКИМТО ОБРАЗОМ В ИМЕНИ ТЕГА БУКВА P ОКАЗАЛАСЬ РУССКАЯ!!!**
        [DataMember]
        public int numРacks;
        [DataMember]
        public string productId;
        [DataMember]
        public bool autoAdd;
        [DataMember]
        public string contNum; // новое место хранения

        public SelectedItem() { }
        public SelectedItem(ItemType t, int _numItemInPack, string np, bool aut) : this(t, _numItemInPack, np, aut, "", CodeState.New) { }
        public SelectedItem(ItemType t, int _numItemInPack, string np, bool aut, string _contNum) : this(t, _numItemInPack, np, aut, _contNum, CodeState.New) { }
        public SelectedItem(ItemType t, int _numItemInPack, string np, bool aut, string _contNum, CodeState _st)
        {
            type = (ItemType)t;
            numberItemInPack = _numItemInPack;
            fullNumber = np;
            autoAdd = aut;
            contNum = _contNum;
            st = _st;
        }


        public int GetItemsNumber()
        {
            int itemNum = -1;
            switch (type)
            {
                case ItemType.Паллета:
                    itemNum = numРacks * numberItemInPack;
                    break;
                case ItemType.Короб:
                    itemNum = numberItemInPack;
                    break;
                case ItemType.Упаковка:
                    itemNum = 1;
                    break;
            }

            return itemNum;
        }

    }
    #endregion

    #region ItemCollectio
    /*
    public class BaseItem
    {
        public string number;
        public int state;
    }
    /// <summary>
    /// Коллекция билетов. Содержит методы и свойства представления БД билетов в памяти
    /// </summary>
    public class TicketCollection<T> : List<T> where T : BaseItem
    {

        public int Add(string value)
        {
            Ticket newDataItem = new Ticket(value);
            return base.List.Add(newDataItem);
        }
        public int Add(string[] value)
        {
            Ticket newDataItem = new Ticket(value);
            return base.List.Add(newDataItem);
        }
        void Ticket_Changed(object sender, TicketState oldState, TicketState newState)
        {
            if (newState == TicketState.Вошёл)
            {
                enteredTickets++;
                if (oldState == TicketState.Вышел)
                    exitTickets--;
            }
            else if (newState == TicketState.Вышел)
            {
                enteredTickets--;
                exitTickets++;
            }

            if (oldState != newState && this.TicketChanged != null)
                this.TicketChanged(sender, oldState, newState);

        }
        public int Add(Ticket value)
        {
            return base.List.Add(value);
        }

        public bool Contains(string value)
        {
            Ticket newDataItem = new Ticket(value);
            return base.List.Contains(newDataItem);
        }

        public int IndexOf(Ticket value)
        {
            return base.List.IndexOf(value);
        }

        public void Insert(int index, Ticket value)
        {
            base.List.Insert(index, value);
        }

        public void Remove(Ticket value)
        {
            base.List.Remove(value);
        }

        #region Переопределённые методы
        protected override void OnClear()
        {
            foreach (Ticket item in base.List)
                item.Changed -= new TicketStateEventHandler(this.Ticket_Changed);

            base.OnClear();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            Ticket item = (Ticket)value;
            item.Changed += new TicketStateEventHandler(this.Ticket_Changed);
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            if (value.GetType() != typeof(Ticket))
            {
                throw new ArgumentException("значение должно быть типа DataItem.");
            }
            Ticket item = (Ticket)value;
            item.Changed -= new TicketStateEventHandler(this.Ticket_Changed);
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            Ticket item = (Ticket)newValue;
            item.Changed += new TicketStateEventHandler(this.Ticket_Changed);

            Ticket item2 = (Ticket)oldValue;
            item.Changed -= new TicketStateEventHandler(this.Ticket_Changed);
        }
        #endregion

        public Ticket this[string barcode]
        {
            get
            {
                for (int i = 0; i < base.List.Count; i++)
                {
                    Ticket template = (Ticket)base.InnerList[i];
                    if (template.barcode == barcode)
                    {
                        return template;
                    }
                }
                return null;
            }
        }

        public Ticket this[int index]
        {
            get
            {
                if ((index >= 0) && (index < base.Count))
                {
                    return (Ticket)base.InnerList[index];
                }
                return null;
            }
            set
            {
                base.List[index] = value;
            }
        }
    }*/
    #endregion

    #region 

    [DataContract]
    public class BoxArgJob
    {
        //ид задания по 1с его мы вернем в отчете в теге OrderIdentifier
        [DataMember]
        public string id { get; set; } = "";
        [DataMember]
        public string gtin { get; set; } = "";
        //номер партии
        [DataMember]
        public string lotNo { get; set; } = "";
        //дата производства в формате YYYY-MM-DDThh:mm:ss.
        [DataMember]
        public string manufactureDate { get; set; } = "";
        [DataMember]
        public DateTime ManufactureDate  = DateTime.Now;
        //срок годности в формате YYYY-MM-DDThh:mm:ss.
        [DataMember]
        public string expDate { get; set; } = "";
        [DataMember]
        public DateTime ExpDate  = DateTime.Now;
        //срок годности в формате YYYY-MM-DDThh:mm:ss.

        //формат для печати срока годности по стандарту ISO 8601
        [DataMember]
        public string formatExpDate { get; set; } = "";
        //веб ссылка на шаблон печати для короба . в примере ссылка на локальный файл. если сейчас нет
        [DataMember]
        public string urlLabelBoxTemplate { get; set; } = "file:c:\\tmpl\\Output.prn";
        [DataMember]
        public string urlLabelPalletTemplate { get; set; } = "file:c:\\tmpl\\Output.prn";
        //количество этикеток на короб
        [DataMember]
        public int numLabelAtBox { get; set; } = 1;
        //количество этикеток на палета
        [DataMember]
        public int numLabelAtPallet { get; set; } = 1;
        //количество упаковок в коробе
        [DataMember]
        public int numPacksInBox { get; set; }
        //количество коробов в палете
        [DataMember]
        public int numBoxInPallet { get; set; }
        //общее количество продукта в серии
        [DataMember]
        public int numPacksInSeries { get; set; }
        //вес одной пачки в граммах
        [DataMember]
        public int packWeightGramm { get; set; }
        // начальные номера коробов и палет
        [DataMember]
        public int boxStartNumber { get; set; }
        [DataMember]
        public int palletStartNumber { get; set; }



        //массив полей для печати на этикетке.
        //поле productName является обязательным и должно быть всегда !
        [DataMember]
        public List<LabelField> boxLabelFields { get; set; } = new List<LabelField>();

        //массив серийных номеров коробов.
        //если будет пустой программа сама будет генерировать код короба по шаблону
        //01{GTIN}11{дата производства}10{номер партии}37{кол-во едениц в коробе}21{сквозной счетчик в рамках партии 5 символов}
        [DataMember]
        public List<string> boxNumbers { get; set; } = new List<string>();
        //массив номеров продукта для контроля . если пуст то принимаются все номера с указанным GTIN.
        //если не пуст то примутся только номера из этого списка
        [DataMember]
        public List<string> productNumbers { get; set; } = new List<string>();

        public bool CheckContent()
        {
            if (string.IsNullOrEmpty(id))
                throw new JobFormatException("Тег id не распознан");

            if (string.IsNullOrEmpty(gtin))
                throw new JobFormatException("Тег gtin не распознан");

            if (string.IsNullOrEmpty(lotNo))
                throw new JobFormatException("Тег lotNo не распознан");

            if (string.IsNullOrEmpty(manufactureDate))
                throw new JobFormatException("Тег manufactureDate не распознан");

            if (string.IsNullOrEmpty(expDate))
                throw new JobFormatException("Тег expDate не распознан");

            if (string.IsNullOrEmpty(formatExpDate))
                throw new JobFormatException("Тег formatExpDate не распознан");

            if (string.IsNullOrEmpty(urlLabelBoxTemplate))
            {
                urlLabelBoxTemplate = "file:c:\\tmpl\\box.prn";
                //throw new JobFormatException("Тег urlLabelBoxTemplate не распознан");
            }

            if (string.IsNullOrEmpty(urlLabelPalletTemplate))
            {
                urlLabelPalletTemplate = "file:c:\\tmpl\\Pallet.prn";
                //throw new JobFormatException("Тег urlLabelBoxTemplate не распознан");
            }
            

            if (numPacksInBox < 1)
                throw new JobFormatException("Тег numРacksInBox должен быть больше  0");

            if (numBoxInPallet < 1)
                throw new JobFormatException("Тег numBoxInPallet должен быть больше  0");

            if (numPacksInSeries < 1)
                throw new JobFormatException("Тег numPacksInSeries должен быть больше  0");

            if (packWeightGramm < 1)
                throw new JobFormatException("Тег packWeightGramm должен быть больше  0");
            
            if (boxLabelFields == null)
                throw new JobFormatException("Массив boxLabelFields отсутствует");

            if (boxLabelFields.Exists(x=>x.FieldName?.Equals("#productName#") == true) == false)
                throw new JobFormatException("Массив boxLabelFields должен содержать елемент с FieldName равным \"#productName#\"");

            if (boxNumbers == null)
                boxNumbers = new List<string>();

            if (productNumbers == null)
                productNumbers = new List<string>();

            if(!DateTime.TryParse(manufactureDate,out ManufactureDate))
                throw new JobFormatException("Тег manufactureDate содержит дату в неверном формате. для передачи даты используйте формат 2019-12-20T00:00:00");

            if (!DateTime.TryParse(expDate, out ExpDate))
                throw new JobFormatException("Тег expDate содержит дату в неверном формате. для передачи даты используйте формат 2019-12-20T00:00:00");

            return true;
        }
    }
    #endregion

    #region Шаблоны задания отчета и задания для терминала
    public class GS1DataFormat : IFormatProvider, ICustomFormatter
    {
        private const int ACCT_LENGTH = 12;

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }

        public string Format(string fmt, object arg, IFormatProvider formatProvider)
        {
            return String.Empty;
        }

        private string HandleOtherFormats(string format, object arg)
        {
            /*
            if (arg is IFormattable)
                return ((IFormattable)arg).ToString(format, CultureInfo.CurrentCulture);
            else if (arg != null)
                return arg.ToString();
            else
                return String.Empty;
                */
            return String.Empty;
        }
    }

    [DataContract]
    public class Order1CBaseInfo : Base1COrder
    {
        //private DateTime _createTime = DateTime.Now;

        [DataMember]
        public string lotNo;
        [DataMember]
        public string gtin { get; set; }
       // [DataMember]
        //public DateTime createTime { get { return _createTime; } set { _createTime = value; } }
        [DataMember]
        [DefaultValue("")]
        public string formatExpDate { get; set; }

        private DateTime _expDate;
        [DataMember]
        public string expDate
        {
            get
            {
                return _expDate.ToString("yyMMdd");// _expDate.ToString("yyyy-MM-ddThh:mm:ssz"); }//yyMMdd
            }
            set
            {
                DateTime ret = DateTime.MinValue;
                try
                {
                    if (value.Length == 6)
                    {
                        //DateTime convertedDate = DateTime.MinValue;
                        String dFormat = "yyMMdd";
                        ret = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

                    }
                    else
                        ret = DateTime.Parse(value);
                }
                catch
                {

                }
                _expDate = ret;
            }
        }
        [DataMember]
        public string productName;

        public string GetExpDateInPrintFormat()
        {
            try
            {
                if (formatExpDate != null)
                    return _expDate.ToString(formatExpDate);
                else
                    return expDate;
            }
            catch 
            {
                return "Ошибка формирования даты";
            }
        }
    }


    [DataContract]
    public class baseAcc1СOrder : Order1CBaseInfo
    {
        [DataContract]
        public class Pallet
        {
            [DataMember]
            public string palletNumber;

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            public bool CheckContent()
            {

                if (palletNumber == null)
                    throw new JobFormatException("Тег palletNumber  не распознан");

                if (boxNumbers.Count == 0)
                    throw new JobFormatException("Тег boxNumber не распознан");

                return true;
            }

            public bool IsBoxInPallete(string num)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == num)
                        return true;
                }
                return false;
            }

        }

        [DataMember]
        public List<Pallet> palletsNumbers = new List<Pallet>();

        public baseAcc1СOrder()
        {

        }

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            foreach (Pallet p in palletsNumbers)
            {
                p.CheckContent();
            }

            return true;
        }
    }

    //отчет в 1С
    //класс задания выполняющегося на Сервере
    [DataContract]
    public class BaseReportInfo
    {
        private string _id = "";
        private string _startTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
        private string _endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:sszz");
        private string _OperatorId = "";

        [DataMember]
        public string id { get { return _id; } set { _id = value; } }
        [DataMember]
        public string startTime { get { return _startTime; } set { _startTime = value; } }
        [DataMember]
        public string endTime { get { return _endTime; } set { _endTime = value; } }
        [DataMember]
        public string OperatorId { get { return _OperatorId; } set { _OperatorId = value; } }
    }


    [DataContract]
    public class baseTsdAccJob<Order> : IBaseJob
       where Order : Order1CBaseInfo
    {
        #region Реализация интерфейса BaseJob
        [DataMember]
        private OrderMeta meta = new OrderMeta();

        public OrderMeta JobMeta
        {
            get
            {
                //meta.name = "Серия:" + order.invoiceNum + "\r" + order.customer;
                //meta.id = order.id;
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

                return true;
            }
        }

        public object GetTsdJob()
        {
            return null;
        }
        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.WaitSend)
                    return true;
                else
                    return false;
            }
        }

        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
        public string SendReports(string url, string user, string pass, bool partOfList,int reguestTimeOut, bool repeat) { return " Нет реализации"; }
        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Приёмка"; }
        #endregion

        [DataMember]
        public Order order1C;

        public baseTsdAccJob()
        {
        }

        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string lotNo { get; set; }
        [DataMember]
        public int allreadyNum { get; set; }
        [DataMember]
        public int number { get; set; }
        [DataMember]
        public string startTime { get; set; }
        [DataMember]
        public string serverUrl { get; set; }
        [DataMember]
        public Operators operators = new Operators();
        //[DataMember]
        //public string operatorId { get; set; }

    }
    #endregion

    #region Чтоб не мешалось

    [DataContract]
    public class Pallet
    {

        public Pallet()
        {
        }


        [DataMember]
        public string Number { get; set; }

        [DataMember]
        public string startTime { get; set; }

        [DataMember]
        public string endTime { get; set; }

        [DataMember]
        public string PalletType { get; set; }

        [DataMember]
        public List<string> boxNumbers = new List<string>();

        public bool IsAlreadyInPallete(string number)
        {
            foreach (string s in boxNumbers)
            {
                if (s == number)
                    return true;
            }

            return false;
        }
        public bool RemoveBox(string number)
        {
            foreach (string s in boxNumbers)
            {
                if (s == number)
                {
                    boxNumbers.Remove(s);
                    return true;
                }
            }

            return false;
        }
    }


    [DataContract]
    public class ReportFile
    {
        public ReportFile()
        {
        }

        [DataMember]
        public string ReportType { get; set; }

        [DataMember]
        public string ReportData { get; set; }
    }
    //*/


    /// <summary>
    /// Задание на агрегацию палеты
    /// </summary>

    [DataContract]
    public class PallAggregateOrder
    {

        public PallAggregateOrder()
        {
            id = "";

            lotNo = "";

            productName = "";
            productDescription = "";
            numBoxesInPallet = 0;
            numLabelAtPallet = 0;
        }

        [DataMember]

        public string id { get; set; }
        //public string gtin { get; set; }  

        [DataMember]

        public string lotNo { get; set; }
        // public string expDate { get; set; }     
        // public string addProdInfo { get; set; }  

        [DataMember]

        public string productDescription { get; set; }

        [DataMember]

        public string productName { get; set; }

        [DataMember]

        public int numBoxesInPallet { get; set; }

        [DataMember]

        public int numLabelAtPallet { get; set; }

        [DataMember]

        public string lineNum = "";

        [DataMember]

        public string LabelPalletTemplate = "http://l3/label/pal.zpl";

        [DataMember]

        public string LabelBoxTemplate = "http://l3/label/box.zpl";

        [DataMember]

        public List<string> boxNumbers = new List<string>();

        [DataMember]

        public List<string> palletNumbers = new List<string>();



        public bool CheckContent()
        {
            //try
            //{
            if (id == null)
                throw new JobFormatException("CheckContent error");

            if (productDescription == null)
                throw new JobFormatException("CheckContent error");

            if (lotNo == null)
                throw new JobFormatException("CheckContent error");

            if (productName == null)
                throw new JobFormatException("CheckContent error");

            if (lineNum == "")
                throw new JobFormatException("CheckContent error");

            return true;
            //}
            // catch (Exception ex)
            // {
            //     ex.ToString();
            // }
            // return false;
        }
    }

    [DataContract]
    public class OrderList
    {

        public OrderList()
        {
        }


        [DataMember]
        public List<OrderMeta> listOfJobs = new List<OrderMeta>();

    }

    public class PallAggregateJob
    {
        public PallAggregateOrder order;
        public Pallet currentPallete = new Pallet();
        public bool printBoxLabel { get; set; }

        public string startTime { get; set; }
        public string operatorId { get; set; }
        public bool labelPrint { get; set; }

        public List<Pallet> readyPallet = new List<Pallet>();

        public bool IsGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю

            //проверка в массивах уже отработанных
            foreach (Pallet p in readyPallet)
            {
                if (p.IsAlreadyInPallete(number))
                    return false;
            }

            //проверка по массиву доступных
            foreach (string s in order.boxNumbers)
            {
                if (s == number)
                    return true;
            }
            return false;
        }
        public string GetNextPalletNum()
        {
            bool bFound = false;
            foreach (string pn in order.palletNumbers)
            {
                bFound = true;
                //проверка в массивах уже отработанных
                foreach (Pallet p in readyPallet)
                {
                    if (p.Number == pn)
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                    return pn;
            }
            return "";
        }
        public int BoxWithoutPallet()
        {
            int b = 0;
            foreach (Pallet p in readyPallet)
            {
                b += p.boxNumbers.Count;
            }
            return order.boxNumbers.Count - b;
        }
    }

    // Отчет агрегации палеты
    [DataContract]
    public class PallAggregateReport
    {
        public PallAggregateReport()
        {
        }


        [DataMember]
        public string startTime { get; set; }

        [DataMember]
        public string endTime { get; set; }

        [DataMember]
        public string operatorId { get; set; }

        [DataMember]
        public List<Pallet> readyPallet = new List<Pallet>();

    }



    #endregion

    #region Ответы сервера
    [DataContract]
    class CloseNotFullBoxAvaible
    {
        [DataMember]
        public bool Result { get; set; }
    }

    #endregion

    #region Инвентаризация
    #region Old
    [DataContract]
    public class Invent1СOrderOld : Order1CBaseInfo
    {
        //константа обоззначающая номер несушествующей палеты
        public static string WithoutPalletId { get { return "000000000000000000"; } }

        [DataMember]
        public int numLabelAtPallet = 0;
        [DataMember]
        public int numРacksInBox = 0;
        [DataMember]
        public int numLayersInBox = 0;
        [DataMember]
        public int numBoxInPallet = 0;

        [DataMember]
        public string urlLabelPalletTemplate = "";
        [DataMember]
        public string urlLabelBoxTemplate = "";
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();
        [DataMember]
        public List<baseAcc1СOrder.Pallet> palletsNumbers = new List<baseAcc1СOrder.Pallet>();

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            return true;
        }
    }

    //отчет в 1С
    [DataContract]
    public class InventReportOld : BaseReportInfo
    {
        [DataContract]
        public class ReportPallet
        {
            public ReportPallet()
            {
            }
            [DataMember]
            public string palletNumber { get; set; }

            [DataMember]
            public List<ReportBox> boxNumbers = new List<ReportBox>();
        }

        [DataContract]
        public class ReportBox
        {
            public ReportBox()
            {
            }
            [DataMember]
            public string boxNumber { get; set; }

            [DataMember]
            public List<string> Numbers = new List<string>();
        }


        [DataMember]
        public string lotNo;

        [DataMember]
        public List<ReportPallet> palletsNumbers = new List<ReportPallet>();

        public bool AddBoxToZeroPallete(UnitItem box)
        {
            if (box.tp != SelectedItem.ItemType.Короб)
                return false;

            ReportBox rb = new ReportBox();
            rb.boxNumber = box.num;
            //перекинуть все элементы из короба по массивам
            foreach (UnitItem uI in box.items)
            {
                rb.Numbers.Add(uI.num);
            }

            //найти нулевую палету и добавить коробку
            foreach (ReportPallet rPal in palletsNumbers)
            {
                if (rPal.palletNumber == PalleteRezervCodes.PalletIDForUnpaletBox)
                {
                    rPal.boxNumbers.Add(rb);
                    return true;
                }
            }

            //нулевая палета не найдена! создать палету
            ReportPallet zP = new ReportPallet();
            zP.palletNumber = PalleteRezervCodes.PalletIDForUnpaletBox;
            zP.boxNumbers.Add(rb);
            palletsNumbers.Add(zP);
            return true;
        }
    }
    #endregion

    [DataContract]
    public class Invent1СOrder : Base1COrder
    {
        [DataMember]
        public string gtin { get; set; }
        //[DataMember]
        //public string addProdInfo { get; set; }
        [DataMember]
        public string lotNo { get; set; }

        private DateTime _expDate;

        [DataMember]
        private string expDate
        {
            get { return _expDate.ToString("yyyy-MM-ddThh:mm:ssz"); }//yyMMdd
            set
            {
                _expDate = DateTime.Parse(value);
                ExpDate = _expDate.ToString("yyMMdd");
            }
        }

        [DataMember]
        public string ExpDate;


        [DataContract]
        public class Pallets
        {
            [DataMember]
            public string palletNumber;

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            public bool CheckContent()
            {

                if (palletNumber == null)
                    throw new JobFormatException("Тег palletNumber  не распознан");

                if (boxNumbers.Count == 0)
                    throw new JobFormatException("Тег boxNumber не распознан");

                return true;
            }
        }

        [DataMember]
        public string productName;

        [DataMember]
        public int numРacksInBox;

        [DataMember]
        public int numBoxInPallet;

        [DataMember]
        public int num;

        [DataMember]
        public List<Pallets> palletsNumbers = new List<Pallets>();

        public Invent1СOrder()
        {

        }

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            foreach (Pallets p in palletsNumbers)
            {
                p.CheckContent();
            }

            return true;
        }
    }

    [DataContract]
    public class InventJobOrder : Order1CBaseInfo
    {
        [DataMember]
        public int numLabelAtPallet = 0;
        [DataMember]
        public int numРacksInBox = 0;
        [DataMember]
        public int numLayersInBox = 0;
        [DataMember]
        public int numBoxInPallet = 0;

        [DataMember]
        public string urlLabelPalletTemplate = "";
        [DataMember]
        public string urlLabelBoxTemplate = "";
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();
        [DataMember]
        public List<UnitItem> palletsNumbers = new List<UnitItem>();

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            return true;
        }

        public int GetNotInventoryContainers()
        {
            int result = 0;
            foreach (UnitItem u in palletsNumbers)
            {
                result += u.GetUtinCountAtStatusAndType(CodeState.New, new SelectedItem.ItemType[] { SelectedItem.ItemType.Короб, SelectedItem.ItemType.Паллета });
            }

            return result;

        }
    }

    //отчет в 1С
    [DataContract]
    public class InventReport : BaseReportInfo
    {
        [DataMember]
        public string lotNo { get; set; }

        [DataMember] //массив уникальных кодов упаковок без вложения
        public List<string> Numbers = new List<string>();

        [DataMember] //массив уникальных номеров контейнеров
        public List<string> containerNumbers = new List<string>();
    }

    [DataContract]
    public class InventJob : baseTsdAccJob<InventJobOrder>
    {
        [DataMember]
        public int num;
        [DataMember]
        public string productName;

        [DataMember]
        public List<UnitItem> readyInvent = new List<UnitItem>();

        public UnitItem selectedItem = null;

        //[DataMember]
        // public bool printBoxLabel { get; set; }
        // [DataMember]
        // public bool printPalleteLabel { get; set; }
        //возвращает елемент в зависимости от его номера
        // если он обработан иначе нулл
        public UnitItem NumberAlreayInProssed(string code)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if ((u.num == code) && (u.st == CodeState.Verify))
                    return u;

                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 == null)
                    continue;
                if ((u1.num == code) && (u1.st == CodeState.Verify))
                    return u1;
            }
            return null;
        }
        public bool NumberInOrder(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {

                if (code == u.num)
                    return true;

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                    return true;

            }
            return false;
        }
        public UnitItem GetNumberFormOrder(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {

                if (code == u.num)
                    return u;

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                {
                    if (u1.GetRoot() == null)
                        u1.SetRoot(u);
                    return u1;
                }

            }
            return null;
        }
        //возвращает первый не использованный номер коробки из задания
        public UnitItem GetNewNumberForBox()
        {
            //проверка по массиву доступных
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                //если элемент палета попустится ниже
                if (u.tp == SelectedItem.ItemType.Паллета)
                {
                    UnitItem u1 = u.GetFirstCodeFromUnitOneLevelAtStatus(CodeState.New);
                    if (u1 != null)
                        return u1;
                }//если элемент коробка рассмотреть статус
                else if (u.tp == SelectedItem.ItemType.Короб)
                {
                    if (u.st == CodeState.New)
                        return u;
                }

            }
            return null;
        }

        //пробует установить указанныц статус для элемента
        public UnitItem TrySetNumberStatus(string code, CodeState newState)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (u.num == code)
                {
                    switch (newState)
                    {
                        case CodeState.Verify:
                            if (u.st == CodeState.New)
                                u.st = newState;
                            else
                                throw new Exception("Статус не может быть установлен.");
                            break;
                        default:
                            throw new Exception("Статус не может быть установлен!");
                    }
                    return u;
                }

                UnitItem u1 = u.TrySetNumberStatus(code, newState);
                if (u1 == null)
                    continue;
                if ((u1.num == code) && (u1.st == CodeState.Verify))
                    return u1;
            }
            return null;
        }

        public bool TryToDisbandContainer(string code, CodeState newStatus, bool upElementToRootLevel)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                UnitItem u1 = null;
                if (u.num == code)
                    u1 = u;



                if (u1 == null)
                    u1 = u.CodeAlreadyInUnit(code);


                if (u1 != null)
                {
                    u1.st = newStatus;
                    //поднять все елементы из контейнера на уровени выше 
                    if (upElementToRootLevel)
                    {
                        if (u1.GetRoot() == null)
                            order1C.palletsNumbers.AddRange(u1.items);
                        else
                            u1.GetRoot().items.AddRange(u1.items);

                        u1.items.Clear();

                    }

                    return true;
                }
            }
            return false;
        }
        //добавляет елемент в задание
        public UnitItem AddItemToOrder(UnitItem newItem)
        {
            //найти контейнер для пачек россыпью и добавить туда елемент
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (u.num == PalleteRezervCodes.PalletIDForUnboxedPack)
                {
                    u.items.Add(newItem);
                    return newItem;
                }
            }
            //если палета не найдена создать ее
            UnitItem p = new UnitItem(PalleteRezervCodes.PalletIDForUnboxedPack, SelectedItem.ItemType.Паллета);
            p.items.Add(newItem);
            order1C.palletsNumbers.Add(p);
            return newItem;

        }
        public bool VerifyProductNum(Util.GsLabelData ld)
        {
            if (ld == null)
                return false;
            //проверить GTIN итд
            //Util.GsLabelData ld = new Util.GsLabelData(quantity);
            string number = ld.SerialNumber;
            //проверить GTIN 
            if (ld.GTIN != order1C.gtin)
                return false;

            //проверить доп поля если они есть
            if (ld.Charge_Number_Lot != null)
            {
                if (order1C.lotNo != "")
                {
                    if (ld.Charge_Number_Lot != order1C.lotNo)
                        return false;
                }
            }

            if (ld.ExpiryDate_JJMMDD != null)
            {
                if (order1C.expDate != "")
                {
                    if (ld.ExpiryDate_JJMMDD != order1C.expDate)
                        return false;
                }
            }

            return true;
        }
        public bool AddToReadyInvent(UnitItem itm)
        {
            foreach (UnitItem i in order1C.palletsNumbers)
            {

                if (i.SetUnitStatus(i.num, CodeState.Verify) != null)
                {
                    readyInvent.Add(itm);
                    return true;
                }
            }
            return false;
        }
        public bool IsBoxGoodForAdd(string number)
        {

            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Короб)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public bool IsPackGoodForAdd(string number)
        {
            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Упаковка)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public bool IsPalletGoodForAdd(string number)
        {
            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Паллета)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public string GetPalletAtNumBox(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (code == u.num)
                    return "";

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                    return u.num;

            }
            return "";
        }
        public UnitItem GetNextPallet()
        {
            foreach (UnitItem pn in order1C.palletsNumbers)
            {
                if (pn.st == CodeState.New)
                    return pn;
            }
            return null;
        }
        public int BoxesWithoutPallet()
        {
            //int b = 0;
            //foreach (PartPallet p in readyPallet)
            //{
            //    b += p.boxes.Count;
            //}
            //return order.boxNumbers.Count - b;
            return 0;
        }
        public UnitItem GetReadyPalletAtNum(string code, CodeState _st)
        {
            foreach (UnitItem pn in order1C.palletsNumbers)
            {
                if (pn.num == code)
                {
                    if (pn.st == _st)
                        return pn;
                    else
                        return null;
                }
            }
            return null;
        }

    }
    #endregion

    #region Переупаковка

    [DataContract]
    public class Repack1СOrder : Order1CBaseInfo //Base1COrder//
    {
        [DataMember]
        public int numLabelAtPallet = 0;
        [DataMember]
        public int numРacksInBox = 0;
        [DataMember]
        public int numLayersInBox = 0;
        [DataMember]
        public int numBoxInPallet = 0;

        [DataMember]
        public string urlLabelPalletTemplate = "";
        [DataMember]
        public string urlLabelBoxTemplate = "";
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();
        [DataMember]
        public List<baseAcc1СOrder.Pallet> palletsNumbers = new List<baseAcc1СOrder.Pallet>();

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            if (boxLabelFields == null)
                throw new JobFormatException("Тег boxLabelFields не распознан");

            if (palletLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не распознан");

            bool bFound = false;
            //присвоить имя препарата в шапку
            foreach (LabelField lf in boxLabelFields)
            {
                if (lf.FieldName == "#productName#")
                {
                    productName = lf.FieldData;
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
                throw new JobFormatException("В массиве полей этикетки короба отсутствует обезательное поле #productName# ");

            return true;
        }
    }

    //отчет в 1С
    [DataContract]
    public class RepackReport : BaseReportInfo
    {
        [DataContract]
        public class ReportPallet
        {
            public ReportPallet()
            {
            }
            [DataMember]
            public string palletNumber { get; set; }

            [DataMember]
            public List<ReportBox> boxNumbers = new List<ReportBox>();
        }

        [DataContract]
        public class ReportBox
        {
            public ReportBox()
            {
            }
            [DataMember]
            public string boxNumber { get; set; }

            [DataMember]
            public List<string> Numbers = new List<string>();
        }


        [DataMember]
        public string lotNo;

        [DataMember]
        public List<ReportPallet> palletsNumbers = new List<ReportPallet>();

        public bool AddBoxToZeroPallete(UnitItem box)
        {
            if (box.tp != SelectedItem.ItemType.Короб)
                return false;

            ReportBox rb = new ReportBox();
            rb.boxNumber = box.num;
            //перекинуть все элементы из короба по массивам
            foreach (UnitItem uI in box.items)
            {
                rb.Numbers.Add(uI.num);
            }

            //найти нулевую палету и добавить коробку
            foreach (ReportPallet rPal in palletsNumbers)
            {
                if (rPal.palletNumber == "000000000000000000")
                {
                    rPal.boxNumbers.Add(rb);
                    return true;
                }
            }

            //нулевая палета не найдена! создать палету
            ReportPallet zP = new ReportPallet();
            zP.palletNumber = "000000000000000000";
            zP.boxNumbers.Add(rb);
            palletsNumbers.Add(zP);
            return true;
        }
    }

    [DataContract]
    public class RepackJob : baseTsdAccJob<Repack1СOrder>
    {
        [DataMember]
        public List<UnitItem> repackPalets = new List<UnitItem>();
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();

        public UnitItem selectedItem;

        public UnitItem NumberAlreayInProssed(string code)
        {
            foreach (UnitItem u in repackPalets)
            {
                if (u.num == code)
                {
                    u.invLv = u.tp;
                    return u;
                }

                UnitItem u1 = u.CodeAlreadyInUnit(code);

                if (u1 != null)
                {
                    u.invLv = u1.tp;
                    u1.SetRoot(u);
                    return u1;
                }
            }
            return null;
        }

        public UnitItem NumberInOrder(string code)
        {
            UnitItem result = new UnitItem(code, SelectedItem.ItemType.Неизвестно);
            //уровенить палеты
            foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            {
                if (code == pl.palletNumber)
                {
                    result.tp = SelectedItem.ItemType.Паллета;
                    break;
                }
                //уровень короба
                foreach (string boxNum in pl.boxNumbers)
                {
                    if (code == boxNum)
                    {
                        result.tp = SelectedItem.ItemType.Короб;
                        return result;
                    }
                }
            }
            return result;
        }

        public UnitItem GetNumberFormOrder(string code)
        {
            UnitItem result = new UnitItem(code);
            //уровенить палеты
            foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            {
                if (code == pl.palletNumber)
                {
                    result.tp = SelectedItem.ItemType.Паллета;
                    return result;
                }
                //уровень короба
                foreach (string boxNum in pl.boxNumbers)
                {
                    if (code == boxNum)
                    {
                        result.tp = SelectedItem.ItemType.Короб;

                        UnitItem root = new UnitItem(pl.palletNumber);
                        root.tp = SelectedItem.ItemType.Паллета;

                        result.SetRoot(root);
                        return result;
                    }
                }
            }
            return null;
        }

    }
    #endregion

    #region Списание палет или коробов
    [DataContract]
    public class Remove1СOrder : baseAcc1СOrder
    {
        /*
        [DataContract]
        public class NotCompleteBox
        {
            [DataMember]
            string boxNumber;

            [DataMember]
            int numРacksInnotCompleteBox;
        }*/


        [DataMember]
        public List<string> notCompleteBoxNumbers = new List<string>();
    }

    //отчет в 1С
    [DataContract]
    public class RemoveReport : BaseReportInfo
    {
        [DataContract]
        public class MovedNumbers
        {
            [DataMember]
            public string number;

            [DataMember]
            public string box;
        }

        [DataMember]
        public List<string> deleteNumbers = new List<string>();
        [DataMember]
        public List<MovedNumbers> moveNumbers = new List<MovedNumbers>();
        [DataMember]
        public List<string> deleteBox = new List<string>();
        [DataMember]
        public List<string> deletePallets = new List<string>();
    }


    [DataContract]
    public class RemovePackJob : baseTsdAccJob<Remove1СOrder>
    {
        [DataMember]
        public List<SelectedItem> SelectedItems = new List<SelectedItem>();

        public bool Add(SelectedItem itm)
        {
            int addNumbers = itm.GetItemsNumber();

            //сравнить добовляющееся количество чтоб оно не превышало 
            //нужное
            if (number < (allreadyNum + addNumbers))
            {
                //System.Windows.Forms.MessageBox.Show("Количество больше чем в задании");
                return false;
            }



            //увеличить обшее количество 
            allreadyNum += addNumbers;
            SelectedItems.Add(itm);

            return true;
        }

        public bool Remove(SelectedItem itm)
        {

            //уменьшить обшее количество 
            allreadyNum -= itm.GetItemsNumber();
            SelectedItems.Remove(itm);
            //SelectedItems.Add(new Item(itm.type,itm.numРacks,itm.fullNumber));

            return true;
        }

        public bool VerifyBoxNum(string fullcode)
        {
            //номер в задании?
            if (!IsOrderedBoxNumber(fullcode))
                return false;

            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            //номер уже обработан?
            foreach (SelectedItem it in SelectedItems)
            {
                if (it.fullNumber == ld.SerialShippingContainerCode00)
                    return false;
            }

            return true;
        }
        public bool IsOrderedBoxNumber(string fullcode)
        {
            string number;
            if (fullcode.Length > 18)
            {
                //проверить GTIN итд
                Util.GsLabelData ld = new Util.GsLabelData(fullcode);

                // принимаем только третичную упаковку
                if (ld.SerialShippingContainerCode00 == null)
                    return false;

                number = ld.SerialShippingContainerCode00;
            }
            else
                number = fullcode;

            //уровенить палеты
            foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            {
                if (number == pl.palletNumber)
                    return true;
                //уровень короба
                foreach (string boxNum in pl.boxNumbers)
                {
                    if (number == boxNum)
                        return true;
                }
            }
            return false;
        }

        public SelectedItem GetItemFromOrder(string fullcode)
        {
            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            // принимаем только третичную упаковку
            if (ld.SerialShippingContainerCode00 == null)
                return null;

            string number = ld.SerialShippingContainerCode00;

            //уровенить палеты
            foreach (baseAcc1СOrder.Pallet pl in order1C.palletsNumbers)
            {
                if (number == pl.palletNumber)
                    return new SelectedItem(SelectedItem.ItemType.Паллета, pl.boxNumbers.Count, pl.palletNumber, false);
                //уровень короба
                foreach (string boxNum in pl.boxNumbers)
                {
                    if (number == boxNum)
                        return new SelectedItem(SelectedItem.ItemType.Короб, 1, number, false, pl.palletNumber);
                }
            }
            return null;
        }

        public SelectedItem VerifyProdNum(string number)
        {
            //номер уже обработан?
            foreach (SelectedItem it in SelectedItems)
            {
                if (it.fullNumber == number)
                    return it;
            }
            return null;
        }

        public SelectedItem GetItemAtNum(string number)
        {
            foreach (SelectedItem sitem in SelectedItems)
            {
                if (number == sitem.fullNumber)
                    return sitem;
            }
            return null;
        }
    }
    #endregion

    #region Приемка на склад
    #region Old accept
    [DataContract]
    public class Accept1СOrderOld
    {

        [DataContract]
        public class Pallets
        {
            [DataMember]
            public string palletNumber;

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            public bool CheckContent()
            {

                if (palletNumber == null)
                    throw new JobFormatException("Тег palletNumber  не распознан");

                if (boxNumbers.Count == 0)
                    throw new JobFormatException("Тег boxNumber не распознан");

                return true;
            }
        }

        [DataMember]
        public string id;

        [DataMember]
        public string lotNo;

        [DataMember]
        public List<Pallets> palletsNumbers = new List<Pallets>();

        public Accept1СOrderOld()
        {

        }

        public bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            foreach (Pallets p in palletsNumbers)
            {
                p.CheckContent();
            }

            return true;
        }
    }
    #endregion
    [DataContract]
    public class Accept1СOrder : Base1COrder //BaseJobInfo
    {

        [DataMember]
        public string gtin { get; set; }
        //[DataMember]
        //public string addProdInfo { get; set; }
        [DataMember]
        public string lotNo { get; set; }

        private DateTime _expDate;
        [DataMember]
        private string expDate
        {
            get { return _expDate.ToString("yyyy-MM-ddThh:mm:ssz"); }//yyMMdd
            set
            {
                _expDate = DateTime.Parse(value);
                ExpDate = _expDate.ToString("yyMMdd");
            }
        }

        [DataMember]
        public string ExpDate;


        [DataContract]
        public class Pallets
        {
            [DataMember]
            public string palletNumber;

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            public bool CheckContent()
            {

                if (palletNumber == null)
                    throw new JobFormatException("Тег palletNumber  не распознан");

                if (boxNumbers.Count == 0)
                    throw new JobFormatException("Тег boxNumber не распознан");

                return true;
            }
        }

        [DataMember]
        public string productName;

        [DataMember]
        public int numРacksInBox;

        [DataMember]
        public int numBoxInPallet;

        [DataMember]
        public int num;

        [DataMember]
        public List<Pallets> palletsNumbers = new List<Pallets>();

        public Accept1СOrder()
        {

        }

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не распознан");

            if (palletsNumbers == null)
                throw new JobFormatException("Тег palletsNumbers не распознан");

            if (palletsNumbers.Count == 0)
                throw new JobFormatException("Массив palletsNumbers не может содержать 0 елементов");

            foreach (Pallets p in palletsNumbers)
            {
                p.CheckContent();
            }

            return true;
        }
    }

    //отчет в 1С
    //класс задания выполняющегося на Сервере
    [DataContract]
    public class AcceptReport : BaseReportInfo
    {
        [DataMember]
        public string lotNo;

        [DataMember]
        public List<string> Numbers = new List<string>();

        [DataMember]
        public List<string> containerNumbers = new List<string>();

        //[DataMember]
        //public List<UnitItem> Numbers = new List<UnitItem>();
    }

    [DataContract]
    public class AcceptJob : baseTsdAccJob<InventJobOrder>
    {
        [DataMember]
        public int num;
        [DataMember]
        public string productName;

        [DataMember]
        public List<UnitItem> readyInvent = new List<UnitItem>();

        public UnitItem selectedItem = null;

        //[DataMember]
        // public bool printBoxLabel { get; set; }
        // [DataMember]
        // public bool printPalleteLabel { get; set; }
        //возвращает елемент в зависимости от его номера
        // если он обработан иначе нулл
        public UnitItem NumberAlreayInProssed(string code)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if ((u.num == code) && (u.st == CodeState.Verify))
                    return u;

                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 == null)
                    continue;
                if ((u1.num == code) && (u1.st == CodeState.Verify))
                    return u1;
            }
            return null;
        }
        public bool NumberInOrder(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {

                if (code == u.num)
                    return true;

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                    return true;

            }
            return false;
        }
        public UnitItem GetNumberFormOrder(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {

                if (code == u.num)
                    return u;

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                {
                    if (u1.GetRoot() == null)
                        u1.SetRoot(u);
                    return u1;
                }

            }
            return null;
        }
        //возвращает первый не использованный номер коробки из задания
        public UnitItem GetNewNumberForBox()
        {
            //проверка по массиву доступных
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                //если элемент палета попустится ниже
                if (u.tp == SelectedItem.ItemType.Паллета)
                {
                    UnitItem u1 = u.GetFirstCodeFromUnitOneLevelAtStatus(CodeState.New);
                    if (u1 != null)
                        return u1;
                }//если элемент коробка рассмотреть статус
                else if (u.tp == SelectedItem.ItemType.Короб)
                {
                    if (u.st == CodeState.New)
                        return u;
                }

            }
            return null;
        }

        //пробует установить указанныц статус для элемента
        public UnitItem TrySetNumberStatus(string code, CodeState newState)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (u.num == code)
                {
                    switch (newState)
                    {
                        case CodeState.Verify:
                            if (u.st == CodeState.New)
                                u.st = newState;
                            else
                                throw new Exception("Статус не может быть установлен.");
                            break;
                        default:
                            throw new Exception("Статус не может быть установлен!");
                    }
                    return u;
                }

                UnitItem u1 = u.TrySetNumberStatus(code, newState);
                if (u1 == null)
                    continue;
                if ((u1.num == code) && (u1.st == CodeState.Verify))
                    return u1;
            }
            return null;
        }

        public bool TryToDisbandContainer(string code, CodeState newStatus, bool upElementToRootLevel)
        {
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                UnitItem u1 = null;
                if (u.num == code)
                    u1 = u;



                if (u1 == null)
                    u1 = u.CodeAlreadyInUnit(code);


                if (u1 != null)
                {
                    u1.st = newStatus;
                    //поднять все елементы из контейнера на уровени выше 
                    if (upElementToRootLevel)
                    {
                        if (u1.GetRoot() == null)
                            order1C.palletsNumbers.AddRange(u1.items);
                        else
                            u1.GetRoot().items.AddRange(u1.items);

                        u1.items.Clear();

                    }

                    return true;
                }
            }
            return false;
        }
        //добавляет елемент в задание
        public UnitItem AddItemToOrder(UnitItem newItem)
        {
            //найти контейнер для пачек россыпью и добавить туда елемент
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (u.num == PalleteRezervCodes.PalletIDForUnboxedPack)
                {
                    u.items.Add(newItem);
                    return newItem;
                }
            }
            //если палета не найдена создать ее
            UnitItem p = new UnitItem(PalleteRezervCodes.PalletIDForUnboxedPack, SelectedItem.ItemType.Паллета);
            p.items.Add(newItem);
            order1C.palletsNumbers.Add(p);
            return newItem;

        }
        /*
           //если не сошелся GTIN заблокировать терминал
            if (ld.EAN_NumberOfTradingUnit != job.order1C.gtin)
            {
                RebuildList(Mode.ErrorMessage, "Пачка другого ЛП!!!");
                //Program.configData.OperatorLock = true;
                //Program.configData.Safe();
                //RebuildList(Mode.ErrorMessageAndLock, "Пачка другого ЛП!!! Терминал заблокирован. Для разблокировки введите пароль мастера");
                return;
            }

            //проверить серию если есть
            if (ld.Charge_Number_Lot != null)
            {
                if (ld.Charge_Number_Lot != job.order1C.lotNo){
                     RebuildList(Mode.ErrorMessage, "Пачка другой серии !!");
                return;
                }
            }

            //проверить тнвэд если есть
            if (ld.PruductIdentificationOfProducer != null){
                if(ld.PruductIdentificationOfProducer != job.order1C.addProdInfo)
                {
                    //MessageBox.Show("Пачка другого ЛП");
                    RebuildList(Mode.ErrorMessage, "ТНВЭД не совпадает !!");
                    return;
                }
            }
         */
        public string VerifyProductNum(Util.GsLabelData ld)
        {
            if (ld == null)
                return "Ошибка распознавания номера";
            //проверить GTIN итд
            //Util.GsLabelData ld = new Util.GsLabelData(quantity);
            //string number = ld.SerialNumber;
            //проверить GTIN ее и тнвэд
            if (ld.GTIN != order1C.gtin)
                return "Пачка другого ЛП !";

           // if (ld.PruductIdentificationOfProducer != order1C.addProdInfo)
            //    return "ТНВЭД не совпадает !";


            //проверить доп поля если они есть
            if (ld.Charge_Number_Lot != null)
            {
                if (order1C.lotNo != "")
                {
                    if (ld.Charge_Number_Lot != order1C.lotNo)
                        return "Пачка другой серии !";
                }
            }

            if (ld.ExpiryDate_JJMMDD != null)
            {
                if (order1C.expDate != "")
                {
                    if (ld.ExpiryDate_JJMMDD != order1C.expDate)
                        return "Не совпадает срок годности !";
                }
            }


            return "";
        }
        public bool AddToReadyInvent(UnitItem itm)
        {
            foreach (UnitItem i in order1C.palletsNumbers)
            {

                if (i.SetUnitStatus(i.num, CodeState.Verify) != null)
                {
                    readyInvent.Add(itm);
                    return true;
                }
            }
            return false;
        }
        public bool IsBoxGoodForAdd(string number)
        {

            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Короб)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public bool IsPackGoodForAdd(string number)
        {
            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Упаковка)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public bool IsPalletGoodForAdd(string number)
        {
            UnitItem u = GetNumberFormOrder(number);
            if (u == null)
                return false;
            if (u.tp != SelectedItem.ItemType.Паллета)
                return false;
            //проверка в массивах уже отработанных
            if (u.st != CodeState.New)
                return false;

            return true;
        }
        public string GetPalletAtNumBox(string code)
        {
            //уровенить палеты
            foreach (UnitItem u in order1C.palletsNumbers)
            {
                if (code == u.num)
                    return "";

                //уровень короба
                UnitItem u1 = u.CodeAlreadyInUnit(code);
                if (u1 != null)
                    return u.num;

            }
            return "";
        }
        public UnitItem GetNextPallet()
        {
            foreach (UnitItem pn in order1C.palletsNumbers)
            {
                if (pn.st == CodeState.New)
                    return pn;
            }
            return null;
        }
        public int BoxesWithoutPallet()
        {
            //int b = 0;
            //foreach (PartPallet p in readyPallet)
            //{
            //    b += p.boxes.Count;
            //}
            //return order.boxNumbers.Count - b;
            return 0;
        }
        public UnitItem GetReadyPalletAtNum(string code, CodeState _st)
        {
            foreach (UnitItem pn in order1C.palletsNumbers)
            {
                if (pn.num == code)
                {
                    if (pn.st == _st)
                        return pn;
                    else
                        return null;
                }
            }
            return null;
        }

    }

    [DataContract]
    public class AcceptJobOld : IBaseJob
    {
        #region Реализация интерфейса BaseJob
        [DataMember]
        private OrderMeta meta = new OrderMeta();

        public OrderMeta JobMeta
        {
            get
            {
                //meta.name = "Серия:" + order.invoiceNum + "\r" + order.customer;
                //meta.id = order.id;
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

                return true;
            }
        }

        public object GetTsdJob()
        {
            return null;
        }
        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.WaitSend)
                    return true;
                else
                    return false;
            }
        }
        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat) { return " Нет реализации"; }
        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Приёмка"; }
        #endregion

        [DataMember]
        public Accept1СOrder order1C;

        public AcceptJobOld()
        {
        }

        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string gtin { get; set; }
        //[DataMember]
        //public string addProdInfo { get; set; }
        [DataMember]
        public string lotNo { get; set; }
        [DataMember]
        public string expDate { get; set; }
        [DataMember]
        public string productName;

        [DataMember]
        public int numРacksInBox;

        [DataMember]
        public int numBoxInPallet;

        [DataMember]
        public int num; // количество пачек без упаковки в коробах или палетах

        [DataMember]
        public int allreadyNum { get; set; }
        [DataMember]
        public int number { get; set; }
        [DataMember]
        public string startTime { get; set; }
        [DataMember]
        public string serverUrl { get; set; }
        [DataMember]
        public string operatorId { get; set; }

        [DataMember]
        public List<SelectedItem> SelectedItems = new List<SelectedItem>();

        public bool Add(SelectedItem itm)
        {
            int addNumbers = itm.GetItemsNumber();

            //сравнить добовляющееся количество чтоб оно не превышало 
            //нужное
            if (number < (allreadyNum + addNumbers))
            {
                //System.Windows.Forms.MessageBox.Show("Количество больше чем в задании");
                return false;
            }



            //увеличить обшее количество 
            allreadyNum += addNumbers;
            SelectedItems.Add(itm);

            return true;
        }

        public bool Remove(SelectedItem itm)
        {

            //уменьшить обшее количество 
            allreadyNum -= itm.GetItemsNumber();
            SelectedItems.Remove(itm);
            //SelectedItems.Add(new Item(itm.type,itm.numРacks,itm.fullNumber));

            return true;
        }

        public bool VerifyProductNum(string fullcode)
        {
            //номер в задании?
            if (!IsOrderedNumber(fullcode))
                return false;

            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            //номер уже обработан?
            foreach (SelectedItem it in SelectedItems)
            {
                if (it.fullNumber == ld.SerialShippingContainerCode00)
                    return false;
            }

            return true;
        }
        public bool IsOrderedNumber(string fullcode)
        {
            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            // принимаем только третичную упаковку
            if (ld.SerialShippingContainerCode00 == null)
                return false;

            string number = ld.SerialShippingContainerCode00;

            //уровенить палеты
            foreach (Accept1СOrder.Pallets pl in order1C.palletsNumbers)
            {
                if (number == pl.palletNumber)
                    return true;
                //уровень короба
                foreach (string boxNum in pl.boxNumbers)
                {
                    if (number == boxNum)
                        return true;
                }
            }
            return false;
        }

        public SelectedItem GetItemAtNum(string number)
        {
            foreach (SelectedItem sitem in SelectedItems)
            {
                if (number == sitem.fullNumber)
                    return sitem;
            }
            return null;
        }

        // [DataMember]
        // public List<ShippingJob.Operator> operators = new List<ShippingJob.Operator>();
    }
    #endregion

    #region Приемка всего на склад
    [DataContract]
    public class AcceptAll1СOrder : Order1CBaseInfo //Base1COrder//
    {
        [DataMember]
        public string contagent = "";
        [DataMember]
        public string invoiceNum = "";

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

           // if (lotNo == null)
           //     throw new JobFormatException("Тег lotNo не распознан");

           // if (contagent == null)
           //     throw new JobFormatException("Тег contagent не распознан");

            if (invoiceNum == null)
                throw new JobFormatException("Тег invoiceNum не распознан");

            productName = "не используется";

            return true;
        }
    }

    [DataContract]
    public class AcceptAllJob : baseTsdAccJob<AcceptAll1СOrder>
    {
        [DataMember]
        public List<UnitItem> SelectedItems = new List<UnitItem>();
        public bool Add(UnitItem itm)
        {
            //проверить что номер еще не добавлен
            foreach (UnitItem u in SelectedItems)
            {
                if (u.num == itm.num)
                    return false;
            }

            SelectedItems.Add(itm);
            return true;
        }
        public bool Remove(UnitItem itm)
        {
            return SelectedItems.Remove(itm);
        }
        public bool VerifyProductNum(string fullcode)
        {
            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            //номер уже обработан?
            foreach (UnitItem it in SelectedItems)
            {
                if (it.num == ld.SerialShippingContainerCode00)
                    return false;
            }

            return true;
        }
        public bool IsOrderedNumber(string fullcode)
        {
            //проверить GTIN итд
            Util.GsLabelData ld = new Util.GsLabelData(fullcode);

            // принимаем только третичную упаковку
            return ld.SerialShippingContainerCode00 == null ? false : true;
        }
        public UnitItem GetItemAtNum(string number,string _gtin)
        {
            foreach (UnitItem sitem in SelectedItems)
            {
                if (sitem.tp == SelectedItem.ItemType.Упаковка)
                {
                    if ((number == sitem.num)&&(_gtin == sitem.gtin))
                        return sitem;
                }
                else
                {
                    if (number == sitem.num)
                        return sitem;
                }
            }
            return null;
        }
    }

    [DataContract]
    public class AcceptAllReport : BaseReportInfo
    {
        //[DataMember]
        //public List<OperatorRep> operators = new List<OperatorRep>();

        [DataMember]
        public List<UnitItemM> Items = new List<UnitItemM>();
    }
    #endregion

    #region Отбор образцов
    //класс задания получаемого от 1С
    [DataContract]
    public class Samples1СOrder : Order1CBaseInfo //BaseJobInfo
    {
        [DataContract]
        public class NotCompleteBox
        {
            [DataMember]
            public string boxNumber;
            [DataMember]
            public int numРacksInnotCompleteBox;
        }

        [DataContract]
        public class NotCompletePallets
        {
            [DataMember]
            public string palletNumber;
            [DataMember]
            public int numBoxInnotCompletePallet;
        }

        [DataMember]
        public string Date;

        [DataMember]
        public string urlLabelPalletTemplate;

        [DataMember]
        public int numLabelAtPallet;

        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();

        [DataMember]
        public List<string> boxNumbers = new List<string>();

        [DataMember]
        public List<string> palletNumbers = new List<string>();

        [DataMember]
        public List<NotCompleteBox> notCompleteBoxNumbers = new List<NotCompleteBox>();

        [DataMember]
        public List<NotCompletePallets> notCompletePalletsNumbers = new List<NotCompletePallets>();

        public Samples1СOrder()
        {
            id = "";
            gtin = "";
            lotNo = "";
            expDate = "";
            //addProdInfo = "";
            //jobType = typeof(Samples1СOrder);

            Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        }

        public override bool CheckContent()
        {
            // try
            // {
            if (id == null)
                return false;

            if (lotNo == null)
                return false;

            if (urlLabelPalletTemplate == null)
                throw new JobFormatException("Тег urlLabelPalletTemplate не содержит путь к шаблону");

            //
            if (palletLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не может быть null");

            if (palletLabelFields.Count == 0)
                throw new JobFormatException("Тег palletLabelFields должен содержать как минимум объект #productName#");

            bool bFound = false;
            //присвоить имя препарата в шапку
            foreach (LabelField lf in palletLabelFields)
            {
                if (lf.FieldName == "#productName#")
                {
                    productName = lf.FieldData;
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
                throw new JobFormatException("В массиве полей этикетки короба отсутствует обезательное поле #productName# ");


            return true;
            //  }
            //   catch (Exception ex)
            //  {
            //      ex.ToString();
            //  }
            //   return false;
        }
    }



    //отчет в 1С
    //класс задания выполняющегося на Сервере
    [DataContract]
    public class SamplesReport : BaseReportInfo
    {
        [DataMember]
        public List<string> sampleNumbers = new List<string>();
        [DataMember]
        public List<SamplingJob.MoveNumbers> moveNumbers = new List<SamplingJob.MoveNumbers>();
    }


    //выполняется на тсд отчет по взятию образцов
    [DataContract]
    public class SamplingJob : baseTsdAccJob<Samples1СOrder>//IBaseJob
    {
        [DataMember]
        public string currentPallet = null;

        //класс коробка+ номер перемещенной пачки
        [DataContract]
        public class MoveNumbers
        {
            public MoveNumbers():this("","") { }

            public MoveNumbers(string b, string p)
            {
                box = b;
                number = p;
            }


            [DataMember]
            public string box { get; set; }
            [DataMember]
            public string number { get; set; }
        }

        [DataContract]
        public class SampledObject
        {
            [DataMember]
            public int type; // 0 unk / 1 move / 2 sampl
            [DataMember]
            public string Number;
            [DataMember]
            public string boxNum;
            [DataMember]
            public string PalletNum;
            [DataMember]
            public string FromBoxNum;
        }

        [DataMember]
        public SampledObject currentSampleObject = new SampledObject();

        [DataMember]
        public List<SampledObject> sampledObjects = new List<SampledObject>();

        public bool IsBoxGoodForAdd(string number)
        {
            //проверка по массиву доступных
            foreach (string op in order1C.boxNumbers)
            {
                if (op == number)
                    return true;
            }
            //проверка по массиве неполных
            foreach (Samples1СOrder.NotCompleteBox s in order1C.notCompleteBoxNumbers)
            {
                if (s.boxNumber == number)
                    return true;
            }
            return false;
        }
        public bool IsPackGoodForAdd(string number)
        {
            //проверка в массивах уже отработанных
            foreach (SampledObject p in sampledObjects)
            {
                if (p.Number == number)
                    return false;
            }

            return true;
        }
        public bool IsPalletGoodForAdd(string number)
        {
            //проверка по массиву доступных
            foreach (string s in order1C.palletNumbers)
            {
                if (s == number)
                    return true;
            }
            //проверка по массиве неполных
            foreach (Samples1СOrder.NotCompletePallets s in order1C.notCompletePalletsNumbers)
            {
                if (s.palletNumber == number)
                    return true;
            }
            return false;
        }

        public bool BoxInOrder(string number)
        {
            //SampledObject result = new SampledObject();

            //проверить код палеты ?
            if (IsPalletGoodForAdd(number))
            {
                //result.PalletNum = number;
                //result.boxNum = "N";
                return true;
            }

            //проверить код коробки ?
            if (IsBoxGoodForAdd(number))
            {
                //result.boxNum = number;
                return true;
            }

            return false;
        }
        public bool BoxIsInSampled(string boxCode)
        {
            foreach (SampledObject so in sampledObjects)
            {
                if (so.boxNum == boxCode)
                    return true;
            }
            return false;
        }
        public SampledObject GetNewSampledObjectAtCode(string number)
        {
            SampledObject result = new SampledObject();

            //проверить код палеты ?
            if (IsPalletGoodForAdd(number))
            {
                result.PalletNum = number;
                result.boxNum = "N";
                return result;
            }

            //проверить код коробки ?
            if (IsBoxGoodForAdd(number))
            {
                result.boxNum = number;
                return result;
            }

            return null;
        }

    }
    #endregion

    #region Отгрузка
    //класс задания получаемого от 1С
    [DataContract]
    public class Shipping1СOrder : Order1CBaseInfo
    {
        [DataContract]
        public class notCompleteBoxItem
        {
            [DataMember]
            public string num = "";
            [DataMember]
            public int quantity = 0;

            public UnitItem GetUniItem()
            {
                UnitItem u = new UnitItem(num);
                u.qP = quantity;
                return u;
            }
        }

        [DataContract]
        public class Product : BaseJobInfo
        {
            [DataMember]
            public string productName = "";
            [DataMember]
            public int numРacksInBox = 0;
            [DataMember]
            public int numBoxesInPallet = 0;
            [DataMember]
            public int quantity = 0;
            [DataMember]
            public List<LabelField> palletLabelFields = new List<LabelField>();
            [DataMember]
            public string urlLabelPalletTemplate = "";

            [DataMember]
            public List<Shipping1СOrder.Pallets> palletsNumbers = new List<Shipping1СOrder.Pallets>();
            [DataMember]
            public List<notCompleteBoxItem> notCompleteBoxNumbers = new List<notCompleteBoxItem>();

            public Product()
            {
                id = "";
                GTIN = "";
                lotNo = "";

                //addProdInfo = "";
            }
            public override bool CheckContent()
            {

                // if (id == null)
                //     throw new Exception("Тег id не распознан");

                if (lotNo == null)
                    throw new JobFormatException("Тег lotNo не распознан");

                if (GTIN == null)
                    throw new JobFormatException("Тег gtin не распознан для продукта " + GTIN);

               // if (addProdInfo == null)
                //    throw new JobFormatException("Тег addProdInfo не распознан для продукта " + gtin);

                if (ExpDate == null)
                    throw new JobFormatException("Тег expDate не распознан для продукта " + GTIN);

                if (productName == null)
                    throw new JobFormatException("Тег productName не распознан для продукта " + GTIN);

                if (numРacksInBox == 0)
                    throw new JobFormatException("Тег numРacksInBox не может быть 0 для продукта " + GTIN);


                //if (numBoxesInPallet == 0)
                //    throw new Exception("Тег numBoxesInPallet не может быть 0");

                if (quantity == 0)
                    throw new JobFormatException("Тег quantity не не может быть 0 для продукта " + GTIN);

                if (palletsNumbers == null)
                    throw new JobFormatException("Тег palletsNumbers  не распознан для продукта " + GTIN);

                //if (palletsNumbers.Count == 0)
                //    throw new JobFormatException("Массив palletsNumbers  не может содержать 0 елементов для продукта " + gtin);

                if (notCompleteBoxNumbers == null)
                    notCompleteBoxNumbers = new List<notCompleteBoxItem>();

                //// foreach (UnitItem pl in Items)
                //{
                //     pl.CheckContent();
                // }



                return true;
            }

            public string VerifyProductNum(Util.GsLabelData ld)//string code)
            {
                //проверить GTIN итд
                //Util.GsLabelData ld = new Util.GsLabelData(code);
                if (ld == null)
                    return "Ошибка распознавания номера";

                //проверить GTIN 
                if (ld.GTIN != GTIN)
                    return "Пачка другого ЛП !";

                //if (ld.PruductIdentificationOfProducer != addProdInfo)
                //    return "ТНВЭД не совпадает !";

                //проверить доп поля если они есть
                if (ld.Charge_Number_Lot != null)
                {
                    if (lotNo != "")
                    {
                        if (ld.Charge_Number_Lot != lotNo)
                            return "Пачка другой серии !";
                    }
                }

                if (ld.ExpiryDate_JJMMDD != null)
                {
                    if (ExpDate != "")
                    {
                        if (ld.ExpiryDate_JJMMDD != ExpDate)
                            return "Не совпадает срок годности !";
                    }
                }


                return "";
            }
        }

        [DataContract]
        public class Pallets
        {
            [DataMember]
            public SelectedItem.ItemType tp;

            [DataMember]
            public string palletNumber;

            [DataMember]
            public List<string> boxNumbers = new List<string>();
            [DataMember]
            public List<notCompleteBoxItem> NotComplBox = new List<notCompleteBoxItem>();

            public bool CheckContent()
            {

                if (palletNumber == null)
                    throw new JobFormatException("Тег palletNumber  не распознан");

                if (boxNumbers.Count == 0)
                    throw new JobFormatException("Тег boxNumber не распознан");

                return true;
            }
        }

        [DataMember]
        public string customer;

        [DataMember]
        public string invoiceNum;

        [DataMember]
        public string urlLabelPalletTemplate = "";

        [DataMember]
        public int numLabelAtPallet = 1;

        [DataMember]
        public List<Product> product = new List<Product>();

        public Shipping1СOrder()
        {

        }

        public override bool CheckContent()
        {
            if (id == null)
                throw new JobFormatException("Тег id не распознан");

            if (customer == null)
                throw new JobFormatException("Тег customer не распознан");

            if (product == null)
                throw new JobFormatException("Тег product не распознан");

            if (product.Count == 0)
                throw new JobFormatException("Массив product не может содержать 0 елементов");

            foreach (Product p in product)
            {
                p.CheckContent();
            }

            return true;
        }
    }



    //отчет в 1С
    //класс задания выполняющегося на Сервере
    [DataContract]
    public class SalesReport : BaseReportInfo
    {
        /*
        [DataContract]
        public class Product
        {
            [DataMember]
            public string gtin;

            [DataMember]
            public string lotNo;

            //[DataMember]
            //public string expDate;

            [DataMember]
            public List<string> palletsNumbers = new List<string>();

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            [DataMember]
            public List<string> Numbers = new List<string>();


            public bool IsNonEmpty()
            {
                if (palletsNumbers.Count > 0)
                    return true;

                if (boxNumbers.Count > 0)
                    return true;

                if (Numbers.Count > 0)
                    return true;

                return false;
            }
        } 
        //[DataMember]
        //public List<Product> product = new List<Product>();
        
             */

        [DataMember]
        public List<UnitItemM> Items = new List<UnitItemM>();
    }

    [DataContract]
    public class ShippingJob : baseTsdAccJob<Shipping1СOrder>//IBaseJob
    {

        [DataContract]
        public class Item
        {
            [DataMember]
            public SelectedItem.ItemType type;//0-def,1-pallete,2-box,3-коробка
            [DataMember]
            public string fullNumber;
            [DataMember]
            public string rootContainerNumber;
            [DataMember]
            public int numberItemInPack;
            [DataMember]
            public int numРacks;//количество пачек в объекте.ПАЧЕК!!!НЕ КОРОБОК!!
            [DataMember]
            public string productId;

            public Item(SelectedItem.ItemType t, int _numItemInPack, string np)
            {
                type = (SelectedItem.ItemType)t;
                numberItemInPack = _numItemInPack;
                fullNumber = np;
            }

            public int GetItemsQuantity()
            {
                int itemNum = -1;
                switch (type)
                {
                    case SelectedItem.ItemType.Паллета:
                        itemNum = numРacks * numberItemInPack;
                        break;
                    case SelectedItem.ItemType.Короб:
                        itemNum = numberItemInPack;
                        break;
                    case SelectedItem.ItemType.Упаковка:
                        itemNum = 1;
                        break;
                }

                return itemNum;
            }
        }

        [DataContract]
        public class AddItemAnswer
        {
            [DataMember]
            public List<Product> products = new List<Product>();
            [DataMember]
            public int curentPaletteItemCount { get; set; }
            [DataMember]
            public int curentPaletteBoxCount;
            [DataMember]
            public int curentPalettePackCount;
            [DataMember]
            public List<string> paletteProductGtin = new List<string>();

        }
        [DataContract]
        public class Product
        {
            public Product(string _id, string n, int num, string _gtin, string lNo, string addProdInf, string _expDate, string paltmpl)
            {
                id = _id;
                name = n;
                number = num;
                gtin = _gtin;
                lotNo = lNo;
                //addProdInfo = addProdInf;
                expDate = _expDate;
                urlLabelPalletTemplate = paltmpl;
            }
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string gtin { get; set; }
            [DataMember]
            public string lotNo { get; set; }
            //[DataMember]
            //public string addProdInfo { get; set; }
            [DataMember]
            public string expDate { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public int number { get; set; }
            [DataMember]
            public int allreadyNum { get; set; }
            [DataMember]
            public List<string> Pallets = new List<string>();
            [DataMember]
            public List<string> Boxs = new List<string>();
            [DataMember]
            public List<string> Pack = new List<string>();
            [DataMember]
            public List<LabelField> palletLabelFields = new List<LabelField>();
            [DataMember]
            public string urlLabelPalletTemplate = "";

            [DataMember]
            public List<UnitItem> SelectedItems = new List<UnitItem>();

            public bool Add(UnitItem itm)
            {
                int addNumbers = itm.GetItemsQuantity();

                //сравнить добовляющееся количество чтоб оно не превышало 
                //нужное
                if (number < (allreadyNum + addNumbers))
                {
                    //System.Windows.Forms.MessageBox.Show("Количество больше чем в задании");
                    return false;
                }

                //увеличить обшее количество 
                allreadyNum += addNumbers;
                SelectedItems.Add(itm);

                return true;
            }

            public bool Remove(UnitItem itm)
            {

                //уменьшить обшее количество 
                allreadyNum -= itm.GetItemsQuantity();
                SelectedItems.Remove(itm);
                //SelectedItems.Add(new Item(itm.type,itm.numРacks,itm.fullNumber));

                return true;
            }

            public bool VerifyProductNum(string code)
            {
                //проверить GTIN итд
                Util.GsLabelData ld = new Util.GsLabelData(code);
                string number = ld.SerialNumber;
                //проверить GTIN ее и тнвэд
                if (ld.GTIN != gtin) return false;

                //проверить доп поля если они есть
                if (ld.Charge_Number_Lot != null)
                {
                    if (ld.Charge_Number_Lot != lotNo)
                        return false;
                }

                if (ld.ExpiryDate_JJMMDD != null)
                {
                    if (ld.ExpiryDate_JJMMDD != expDate)
                        return false;
                }

                return true;
            }

            public UnitItem GetItemAtNum(string number)
            {
                foreach (UnitItem sitem in SelectedItems)
                {
                    if (number == sitem.num)
                        return sitem;
                }
                return null;
            }

            //обновить счеткичи отобранного на основе текущих данных
            public void RefreshCounters()
            {
                allreadyNum = 0;

                foreach (UnitItem itm in SelectedItems)
                    allreadyNum += itm.GetItemsQuantity();

            }
        }

        public ShippingJob()
        {
        }

        [DataMember]
        public string invoise { get; set; }
        [DataMember]
        public string customer { get; set; }

        [DataMember]
        public string operatorId { get; set; }
        [DataMember]
        public string paletteCode { get; set; }
        [DataMember]
        public SelectedItem.ItemType palleteType { get; set; }

        [DataMember]
        public List<ShippingJob.Product> product = new List<ShippingJob.Product>();

        [DataMember]
        public List<string> paletteProductGtin = new List<string>();
        [DataMember]
        public int paletteItemsCount { get; set; }
        [DataMember]
        public int palettePackCount { get; set; }
        [DataMember]
        public int paletteBoxCount { get; set; }
        [DataMember]
        public int numLabelAtPallete { get; set; }
        [DataMember]
        public string urlLabelPaletteTemplate { get; set; }


        // [DataMember]
        //public List<LabelField> paletteLabelFields = new List<LabelField>();

        // [DataMember]
        // public List<ShippingJob.Operator> operators = new List<ShippingJob.Operator>();
    }

    [DataContract]
    public class ShippingQuery
    {
        [DataMember]
        public string jobId { get; set; }

        [DataMember]
        public string gtin { get; set; }

        [DataMember]
        public string number { get; set; }

        [DataMember]
        public string palNumber { get; set; }

        [DataMember]
        public string operatorId { get; set; }

        [DataMember]
        public ShippingQueryType qType { get; set; }

        public ShippingQuery(string id, string _gtin, string num, string _palNum, ShippingQueryType t, string _opId)
        {
            jobId = id;
            number = num;
            qType = t;
            gtin = _gtin;
            operatorId = _opId;
            palNumber = _palNum;
        }
    }

    [DataContract]
    public class ShippingQueryNewPaletteNum
    {
        [DataMember]
        public string id { get; set; }

        [DataMember]
        public string number { get; set; }
        [DataMember]
        public string label { get; set; }
        
    }

    [DataContract]
    public class PaleteLadelData
    {
        [DataContract]
        public class ProductInfo
        {
            [DataMember]
            public string gtin { get; set; }
            [DataMember]
            public string lotNo { get; set; }
           // [DataMember]
           // public string addProdInfo { get; set; }
            [DataMember]
            public string expDate { get; set; }
            [DataMember]
            public string name { get; set; }
            /// <summary>
            /// Количество отобранного товара в пачках!
            /// </summary>
            [DataMember]
            public int quantity { get; set; }
            [DataMember]
            public int numLabelAtPallet = 0;

            //[DataMember]
            //public string urlLabelPalletTemplate = "";
            //[DataMember]
            //public List<LabelField> palletLabelFields = new List<LabelField>();
        }

        [DataMember]
        public string sscc;
        [DataMember]
        public List<ProductInfo> products = new List<ProductInfo>();

        [DataMember]
        public int curentPaletteItemCount;
        [DataMember]
        public int curentPaletteBoxCount;
        [DataMember]
        public int curentPalettePackCount;
    }

    public enum ShippingQueryType
    {
        Add = 0,
        Delete = 1,
        CloseOrder = 2,
        ClosePalete = 3,
        DropPaleteDropContent = 4,
        DropPaleteSafeContent = 5,
        CloseBox = 6
    }
    #endregion

    #region Аггрегация коробок в короба и палеты
    [DataContract]
    public class FullPartAgg1СOrder : Order1CBaseInfo // AggCorobBaseInfo
    {
        public FullPartAgg1СOrder()
        {
            id = "";
            gtin = "";
            lotNo = "";
            //ExpDate = "";
            //addProdInfo = "";
            //prefixBoxCode = "";
            Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
            // formatExpDate = "dd.MM.yyyy";
        }


        #region добавить из AggCorobBaseInfo
        public DateTime date;

        [DataMember]
        public string Date
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set
            {
                if (value != null)
                {
                    if (value.Length == 6)
                    {
                        String dFormat = "yyMMdd";
                        date = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                    }
                    else
                        date = DateTime.Parse(value);
                }
                else
                    date = DateTime.MinValue;

            }
        }

        [DataMember]
        public int numPacksInBox = 0; 
        [DataMember]
        public int numBoxInPallet = 0;
        [DataMember]
        public int numLayersInBox = 0;
        [DataMember]
        public int numLabelAtBox = 0;
        //[DataMember]
        //public string formatExpDate = "";
        [DataMember]
        public string urlLabelBoxTemplate = "";
        [DataMember]
        public string urlLabelPalletTemplate = "";
        
        //[DataMember]
        //public string prefixBoxCode { get; set; }

        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();
        //
        //private DateTime _expDate;

        //[DataMember]
        //private string expDate
        //{
        //    get { return _expDate.ToString("yyyy-MM-ddThh:mm:ssz"); }//yyMMdd
        //    set
        //    {
        //        if (value != null)
        //        {
        //            if (value.Length == 6)
        //            {
        //                String dFormat = "yyMMdd";
        //                _expDate = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None);
        //            }
        //            else
        //                _expDate = DateTime.Parse(value);
        //        }
        //        else
        //            _expDate = DateTime.MinValue;

        //        ExpDate = _expDate.ToString("yyMMdd");
        //    }
        //}

        [DataMember]
        public string ExpDate
        {
            get
            {
                return expDate;
            }
            set
            {
                expDate = value;
            }
        }

        #endregion

        [DataMember]
        public List<string> boxNumbers = new List<string>();
        [DataMember]
        public List<string> palletNumbers = new List<string>();

        [DataMember]
        public int numPacksInSeries;
        [DataMember]
        public int lineNum;

        [DataMember]
        public string formatBoxNumber = "";

        public override bool CheckContent()
        {

            if (id == null)
                throw new JobFormatException("Тег id не может быть пуст");

            //if (boxNumbers == null)
            //    throw new JobFormatException("Тег boxNumbers не может отсутствовать и быть пустым");

            //if (palletNumbers == null)
            //    throw new JobFormatException("Тег palletNumbers не может  отсутствовать и быть пустым");

            if (formatExpDate == null)
                throw new JobFormatException("Тег formatExpDate не может быть пуст");


            if (urlLabelBoxTemplate == null)
                throw new JobFormatException("Тег urlLabelBoxTemplate не содержит путь к шаблону");

            //
            if (boxLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не может быть null");

            if (boxLabelFields.Count == 0)
                throw new JobFormatException("Тег palletLabelFields должен содержать как минимум объект #productName#");

            if (numLayersInBox == 0)
                throw new JobFormatException("Тег numLayersInBox не может быть 0");

            if (numLabelAtBox < 1)
                throw new JobFormatException("Тег numLabelAtBox не может  быть меньше 1");

            //if (numPacksInSeries < 1)
            //    throw new JobFormatException("Тег numPacksInSeries не может быть меньше 1");

            //проверить корректность соотношения пачек и слоем
            if ((numPacksInBox / numLayersInBox) < 1)
                throw new JobFormatException("Частное numРacksInBox на numLayersInBox не может быть меньше 1");

            foreach (LabelField fl in boxLabelFields)
            {
                if (fl.FieldName == "#productName#")
                    return true;
            }
            throw new JobFormatException("Тег boxLabelFields должен содержать объект #productName#");
        }
    }

    /// <summary>
    /// Класс для компактного сохранения данных UnitItem в json 
    /// компактность за счет отказа от большинства полей
    /// </summary>
    [DataContract]
    public class UnitItemStrong
    {
        [DefaultValue("")]
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public int type { get; set; } = 0;
        [DataMember]
        public string num { get; set; }
        [IgnoreDataMember]
        public DateTime tm { get; set; } = DateTime.Now;
        [DataMember]
        public string time { 
            get { return tm.ToString("yyyy-MM-ddThh:mm:sszz");}
            set
            {
                if (value != null)
                {
                   tm = DateTime.Parse(value);
                }
                else
                    tm = DateTime.MinValue;
            }
        }
        [DataMember]
        public List<UnitItemStrong> items = new List<UnitItemStrong>();

        public UnitItemStrong(string _id, int _type, string _num)
        {
            id = _id;
            type = _type;
            num = _num;
        }
    }

    //отчет в 1С
    [DataContract]
    public class FullPartAggReport : BaseReportInfo
    {
        [IgnoreDataMember]
        public new string OperatorId { get { return base.OperatorId; } set { base.OperatorId = value; } }

        public bool DataIsSet { get; set; } = false;
        [DataMember]
        public bool partOfList = false;
        [DataMember]
        public List<UnitItemStrong> Items = new List<UnitItemStrong>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
    }

    //Класс задания выполняющегося на ТСД для коробов
    [DataContract]
    public class FullPartAggTsdJob : baseTsdAccJob<FullPartAgg1СOrder>///AggCorobBaseInfo, IBaseJob
    {
        [DataMember]
        public List<DefectiveCode> brackBox = new List<DefectiveCode>();

        [DataMember]
        public FullSerializeBox selectedBox = new FullSerializeBox("");

        [DataMember]
        public string operatorId { get; set; }
        [DataMember]
        public bool labelPrint { get; set; }
        [DataMember]
        public bool printBoxLabel { get; set; }
        [DataMember]
        public string checkedNumber { get; set; }
        [DataMember]
        public string msg { get; set; }

        [DataMember]
        public string palNum { get; set; }
        [DataMember]
        public List<string> boxNumbers = new List<string>();

        public FullPartAggTsdJob()
            : base()
        {
            //jobType = typeof(LineAggregateJob);
        }

        //возвращает первый не использованный номер коробки из задания
        public bool IsPackGoodForAdd(string number)
        {
            //проверка в массивах уже отработанных
            if (selectedBox.IsAlreadyInBox(number))
                return false;

            return false;
        }
    }

    [DataContract]
    public class FullPartPalAggBox
    {
        [DataMember]
        public string Num { get; set; }
        [DataMember]
        public bool Chk { get; set; }
        [DataMember]
        public int id { get; set; }
    }

    //Класс задания выполняющегося на ТСД для палет
    [DataContract]
    public class FullPartAggPallete
    {
        private DateTime date = DateTime.Now;

        [DataMember]
        public string boxTime
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set { date = DateTime.Parse(value); }
        }
        [DataMember]
        public string tsdId { get; set; }//номер тсд который работает с коробом. или работал с ним.
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public int PalId { get; set; }
        [DataMember]
        public string Num { get; set; }
        [DataMember]
        public NumberState State { get; set; }

        public FullPartAggPallete() : this(0,"") { }
        public FullPartAggPallete(int id,string n) { PalId = id; Num = n; State = NumberState.Доступен; }

        [DataMember]
        public List<string> boxNumbers = new List<string>();
    }
    #endregion

    #region Сериализация в  короба 
    [DataContract]
    public class ItemNum
    {
        [DataMember]
        public string GTIN { get; set; }
        [DataMember]
        public string Sn { get; set; }
        [DataMember]
        public string Field93 { get; set; }
        [DataMember]
        public string FullNum { get; set; }
    }

    [DataContract]
    public class FullNumReadyBox
    {
        // [DataMember]
        public DateTime date;


        public string _boxNumber = "";

        [DataMember]
        public string boxNumber
        {
            get { return _boxNumber; }
            set { _boxNumber = value; }
        }


        [DataMember]
        public List<ItemNum> productNumbers = new List<ItemNum>();

        [DataMember]
        public string boxTime
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set { date = DateTime.Parse(value); }
        }

        [DataMember]
        public string id { get; set; }

        public FullNumReadyBox()
        {
            id = "";
        }

        public FullNumReadyBox(FullNumReadyBox o)
        {
            boxNumber = o.boxNumber;
            boxTime = o.boxTime;
            id = o.id;
            productNumbers.AddRange(o.productNumbers);
        }
        public bool IsAlreadyInBox(string number)
        {
            foreach (ItemNum s in productNumbers)
            {
                if (s.Sn == number)
                    return true;
            }

            return false;
        }
        public bool RemovePack(string number)
        {
            foreach (ItemNum s in productNumbers)
            {
                if (s.Sn == number)
                {
                    productNumbers.Remove(s);
                    return true;
                }
            }

            return false;
        }

        public bool AddLayer(List<string> layerCodes)
        {

            return false;
        }
    }
    [DataContract]
    public class FullSerializeBox : FullNumReadyBox
    {
        [DataMember]
        public NumberState state { get; set; }
        [DataMember]
        public string tsdId { get; set; }//номер тсд который работает с коробом. или работал с ним.
        [DataMember]
        public int PalId { get; set; }//Id записи палеты в массисе readyPalets описывающий палету в которой числится короб
        [DataMember]
        public bool ManualCodeAdded { get; set; }
        public FullSerializeBox() : this("") { }
        public FullSerializeBox(string n) { boxNumber = n; ManualCodeAdded = false; }
        public FullSerializeBox Clone()
        {
            FullSerializeBox o = new FullSerializeBox(boxNumber);
            //
            o.date = date;
            o.productNumbers.AddRange(productNumbers);
            o.boxTime = boxTime;
            o.id = id;
            o.ManualCodeAdded = ManualCodeAdded;
            o.PalId = PalId;
            return o;
        }
    }

    [DataContract]
    public class FullSerialize1cOrder : Order1CBaseInfo 
    {
        public FullSerialize1cOrder()
        {
            id = "";
            gtin = "";
            lotNo = "";
            //Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        }

        [DataMember]
        public int numPacksInBox = 0;
        [DataMember]
        public int numLayersInBox = 0;
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();

        [DataMember]
        public int formatBoxNumber = 0;

        public override bool CheckContent()
        {

            if (id == null)
                throw new JobFormatException("Тег id не может быть пуст");

            if (gtin == null)
                throw new JobFormatException("Тег gtin не может отсутствовать и быть пустым");

            if (lotNo == null)
                throw new JobFormatException("Тег lotNo не может  отсутствовать и быть пустым");

            //if (formatExpDate == null)
            //    throw new JobFormatException("Тег formatExpDate не может быть пуст");
            formatExpDate = "ddmmyy";
            //
            if (boxLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не может быть null");

            if (boxLabelFields.Count == 0)
                throw new JobFormatException("Тег palletLabelFields должен содержать как минимум объект #productName#");

            if (numLayersInBox == 0)
                throw new JobFormatException("Тег numLayersInBox не может быть 0");

 
            //проверить корректность соотношения пачек и слоем
            if ((numPacksInBox / numLayersInBox) < 1)
                throw new JobFormatException("Частное numРacksInBox на numLayersInBox не может быть меньше 1");

            foreach (LabelField fl in boxLabelFields)
            {
                if (fl.FieldName == "#productName#")
                    return true;
            }
            throw new JobFormatException("Тег boxLabelFields должен содержать объект #productName#");
        }
    }

    ///Ответ проверки короба на кривые ноера
    [DataContract]
    public class BoxCheckresult
    {
        [DataMember]
        public bool verify { get; set; }
        [DataMember]
        public string msg { get; set; } = "";
       
        [DataMember]
        public List<Box> items { get; set; } = new List<Box>();
        [DataMember]
        public List<string> DefectCodes { get; set; } = new List<string>();

        public BoxCheckresult(){}
    }

    [DataContract]
    public class UnitSerializeRep
    {
        [DefaultValue("")]
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string num { get; set; }
        [IgnoreDataMember]
        public DateTime tm { get; set; } = DateTime.Now;
        [DataMember]
        public string time
        {
            get { return tm.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set
            {
                if (value != null)
                {
                    tm = DateTime.Parse(value);
                }
                else
                    tm = DateTime.MinValue;
            }
        }

        public UnitSerializeRep(string _id,  string _num, DateTime time)
        {
            id = _id;
            num = _num;
            tm = time;
        }
    }

    [DataContract]
    public class FullSerializeReport : BaseReportInfo
    {
        [IgnoreDataMember]
        public new string OperatorId { get { return base.OperatorId; } set { base.OperatorId = value; } }

        public bool DataIsSet { get; set; } = false;
        [DataMember]
        public bool partOfList = false;
        [DataMember]
        public List<UnitSerializeRep> Items = new List<UnitSerializeRep>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
    }

    //Класс задания выполняющегося на ТСД для коробов
    [DataContract]
    public class FullSerializeTsdJob : baseTsdAccJob<FullSerialize1cOrder>///AggCorobBaseInfo, IBaseJob
    {
        [DataMember]
        public List<DefectiveCode> brackBox = new List<DefectiveCode>();

        [DataMember]
        public FullSerializeBox selectedBox = new FullSerializeBox("");

        [DataMember]
        public string operatorId { get; set; }     
        [DataMember]
        public bool labelPrint { get; set; }
        [DataMember]
        public bool printBoxLabel { get; set; }
        [DataMember]
        public string checkedNumber { get; set; }
        [DataMember]
        public string msg { get; set; }

        [DataMember]
        public string palNum { get; set; }
        [DataMember]
        public List<string> boxNumbers = new List<string>();

        public FullSerializeTsdJob()
            : base()
        {
            //jobType = typeof(LineAggregateJob);
        }

        //возвращает первый не использованный номер коробки из задания
        public bool IsPackGoodForAdd(string number)
        {
            //проверка в массивах уже отработанных
            if (selectedBox.IsAlreadyInBox(number))
                return false;

            return false;
        }
    }
    #endregion

    #region Агрегация в палеты неа складе
    //класс задания получаемого от 1С
    [DataContract]
    public class PalAggregate1СOrder : Order1CBaseInfo, ICloneable
    {
        public PalAggregate1СOrder()
        {
            id = "";
            gtin = "";
            lotNo = "";
            //ExpDate = "";
            //addProdInfo = "";
            //prefixBoxCode = "";
            //Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
            // formatExpDate = "dd.MM.yyyy";
        }

        public DateTime date;

        [DataMember]
        public string Date
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:sszz"); }
            set { date = DateTime.Parse(value); }
        }

        [DataMember]
        public string line;
        [DataMember]
        public bool lineComplited;
        [DataMember]
        public int numBoxInPallet = 0;
        [DataMember]
        public string urlLabelPalletTemplate;
        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();
        [DataMember]
        public int numLabelAtPallet = 0;
       // [DataMember]
        //public string urlLabelBoxTemplate;
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();


        [DataMember]
        public List<string> boxNumbers = new List<string>();
        [DataMember]
        public List<string> palletNumbers = new List<string>();

        //[DataMember]
        //public int numPacksInSeries;
        public override bool CheckContent()
        {

            if (id == null)
                throw new JobFormatException("Тег id не может быть пуст");

            //if (addProdInfo == null)
             //   throw new JobFormatException("Тег addProdInfo не может быть пуст");

            if (urlLabelPalletTemplate == null)
                throw new JobFormatException("Тег urlLabelPalletTemplate не содержит путь к шаблону");

            //
            if (palletLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не может быть null");

            if (palletLabelFields.Count == 0)
                throw new JobFormatException("Тег palletLabelFields должен содержать как минимум объект #productName#");

            if (numBoxInPallet == 0)
                throw new JobFormatException("Тег numBoxInPallet не может быть 0");

            if (numLabelAtPallet < 1)
                throw new JobFormatException("Тег numLabelAtPallet не может  быть меньше 1");

            //if (boxNumbers == null)
            //    throw new JobFormatException("Тег boxNumbers не может быть null");

            //if (boxNumbers.Count == 0)
            //    throw new JobFormatException("Тег boxNumbers должен содержать как минимум объект 1");

            //if (palletNumbers == null)
            //    throw new JobFormatException("Тег boxNumbers не может быть null");

            //if (palletNumbers.Count == 0)
            //    throw new JobFormatException("Тег palletNumbers должен содержать как минимум объект 1");

           

            //проверить корректность соотношения пачек и слоем
            // if ((numРacksInBox / numLayersInBox) < 1)
            //    throw new JobFormatException("Частное numРacksInBox на numLayersInBox не может быть меньше 1");

            bool bFound = false;
            //присвоить имя препарата в шапку
            foreach (LabelField lf in boxLabelFields)
            {
                if (lf.FieldName == "#productName#")
                {
                    productName = lf.FieldData;
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
                throw new JobFormatException("В массиве полей этикетки короба отсутствует обезательное поле #productName# ");

            return true;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        /*
        public PalAggregate1СOrder Copy()
        {
            PalAggregate1СOrder r = (PalAggregate1СOrder)this.Clone();
            r.palletNumbers = new List<string>();
            return r;
        }*/
    }

    //отчет в 1С
    [DataContract]
    public class PalAggerateReport : BaseReportInfo
    {
        [DataContract]
        public class ReadyPallete
        {

            private string _palletNumbers = "";
            [DataMember]
            public string number
            {
                get { return _palletNumbers; }
                set { _palletNumbers = value; }
            }
            [DataMember]
            public string operatorId { get; set; }
            [DataMember]
            public string createTime;

            [DataMember]
            public List<string> boxNumbers = new List<string>();

            public ReadyPallete()
            {

            }

            public ReadyPallete(UnitItem o)
            {
                // boxNumber = o.boxNumber;
                // boxTime = o.boxTime;
                // id = o.id;
                // productNumbers.AddRange(o.productNumbers);
            }

        }

        [DataMember]
        public List<ReadyPallete> palletsNumbers = new List<ReadyPallete>();
    }

    [DataContract]
    public class PalAggregateJob : baseTsdAccJob<PalAggregate1СOrder>
    {
        //[DataMember]
        //public List<UnitItem> repackPalets = new List<UnitItem>();
        [DataMember]
        public decimal startBoxDiapazon = 0;
        [DataMember]
        public decimal stopBoxDiapazon = 0;
        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();

        [DataMember]
        public List<LabelField> palletLabelFields = new List<LabelField>();

        [DataMember]
        public PalletItem selectedItem;

        public UnitItem NumberAlreayInProssed(string code)
        {
            foreach (UnitItem u in selectedItem.items)
            {
                if (u.num == code)
                {
                    u.invLv = u.tp;
                    return u;
                }

                UnitItem u1 = u.CodeAlreadyInUnit(code);

                if (u1 != null)
                {
                    u.invLv = u1.tp;
                    u1.SetRoot(u);
                    return u1;
                }
            }
            return null;
        }

        public UnitItem NumberInOrder(string code)
        {
            UnitItem result = new UnitItem(code, SelectedItem.ItemType.Неизвестно);
            //уровень короба
            foreach (string pl in order1C.boxNumbers)
            {
                if (code == pl)
                {
                    result.tp = SelectedItem.ItemType.Короб;
                    break;
                }
            }
            return result;
        }

        public UnitItem GetNumberFormOrder(string code)
        {
            UnitItem result = new UnitItem(code);
            //уровенить палеты
            foreach (string pl in order1C.palletNumbers)
            {
                if (code == pl)
                {
                    result.tp = SelectedItem.ItemType.Паллета;
                    return result;
                }
            }
            return null;
        }

        public bool BoxNumInRange(string num)
        {
            try {
                decimal n = Convert.ToDecimal(num);

                if((n>= startBoxDiapazon) && (n<= stopBoxDiapazon))
                        return true;

            } catch {  }
            return false;
        }
        /*
        public UnitItem GetNextPalete()
        {
            //UnitItem result = new UnitItem("", SelectedItem.ItemType.Неизвестно);
            //уровенить палеты
            foreach (UnitItem pl in repackPalets)
            {
                if (pl.st == CodeState.New)
                    return pl;
            }
            return null;
        }

        public bool SetPalletState(string num, CodeState newState)
        {
            foreach (UnitItem u in repackPalets)
            {
                if (num == u.num)
                {
                    u.st = newState;
                    return true;
                }
            }
            return false;
        }
        */
    }

    [DataContract]
    public class PalletItem:UnitItem
    {
        [DataMember]
        public int boxAvaible = 0;

        public PalletItem() : this(null, 0) { }
        public PalletItem(UnitItem i) : this(i, 0) { }
             
        public PalletItem(UnitItem i,int _ab)
        {
            boxAvaible = _ab;

            if (i != null)
            {
                this.id = i.id;
                this.invLv = i.invLv;
                gtin = i.gtin;
                dt = i.dt;
                oId = i.oId;
                num = i.num;
                qP = i.qP;
                qIp = i.qIp;
                tp = i.tp;
                st = i.st;
                SetRoot(i.GetRoot());
                pN = i.pN;
                //
                foreach (UnitItem a in i.items)
                    items.Add(a.Clone());
            }
        }
    }
    #endregion

    #region Аггрегация коробок в короба без палет
    //класс задания получаемого от 1С
    [DataContract]
    public class PartAggregate1СOrder : Order1CBaseInfo // AggCorobBaseInfo
    {
        public PartAggregate1СOrder()
        {
            id = "";
            gtin = "";
            lotNo = "";
            //ExpDate = "";
            //addProdInfo = "";
            prefixBoxCode = "";
            Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
           // formatExpDate = "dd.MM.yyyy";
    }


        #region добавить из AggCorobBaseInfo
        public DateTime date;

        [DataMember]
        public string Date
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:ssz"); }
            set
            {
                if (value != null)
                {
                    if (value.Length == 6)
                    {
                        String dFormat = "yyMMdd";
                        date = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                    }
                    else
                        date = DateTime.Parse(value);
                }
                else
                    date = DateTime.MinValue;

            }
        }

        private int NumPaksInBox = 0;

        [DataMember]
        public int numРacksInBox { get => NumPaksInBox; set { NumPaksInBox = value; } }
        [DataMember]
        public int numPacksInBox { get => NumPaksInBox; set {  NumPaksInBox = value; } }

        [DataMember]
        public int numLayersInBox = 1;
        [DataMember]
        public int numLabelAtBox = 0;
        //[DataMember]
        //public string formatExpDate = "";

      

        [DataMember]
        public string urlLabelBoxTemplate = "";

        [DataMember]
        public string prefixBoxCode { get; set; }

        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();

        [DataMember]
        public string ExpDate
        {
            get
            {
                return expDate;
            }
            set
            {
                expDate = value;
            }
        }

        #endregion

        [DataMember]
        public List<string> boxNumbers = new List<string>();

        [DataMember]
        public int numPacksInSeries;
        [DataMember]
        public int lineNum;

        public override bool CheckContent()
        {

            if (id == null)
                throw new JobFormatException("Тег id не может быть пуст");

            //if (addProdInfo == null)
            //    throw new JobFormatException("Тег addProdInfo не может быть пуст");

            if (formatExpDate == null)
                throw new JobFormatException("Тег formatExpDate не может быть пуст");


            if (urlLabelBoxTemplate == null)
                throw new JobFormatException("Тег urlLabelBoxTemplate не содержит путь к шаблону");

            //
            if (boxLabelFields == null)
                throw new JobFormatException("Тег palletLabelFields не может быть null");

            if (boxLabelFields.Count == 0)
                throw new JobFormatException("Тег palletLabelFields должен содержать как минимум объект #productName#");

            if (numLayersInBox == 0)
                throw new JobFormatException("Тег numLayersInBox не может быть 0");

            if (numLabelAtBox < 1)
                throw new JobFormatException("Тег numLabelAtBox не может  быть меньше 1");

            if(numPacksInSeries < 1)
                throw new JobFormatException("Тег numPacksInSeries не может быть меньше 1");

            //проверить корректность соотношения пачек и слоем
            if ((numРacksInBox / numLayersInBox) < 1)
                throw new JobFormatException("Частное numРacksInBox на numLayersInBox не может быть меньше 1");

            foreach (LabelField fl in boxLabelFields)
            {
                if (fl.FieldName == "#productName#")
                    return true;
            }
            throw new JobFormatException("Тег palletLabelFields должен содержать объект #productName#");
        }
    }


    //класс задания получаемого от 1С
    [DataContract]
    public class NewPartAggregate1СOrder : PartAggregate1СOrder 
    {
        public NewPartAggregate1СOrder()
        {
            
        }
        public NewPartAggregate1СOrder(PartAggregate1СOrder obj)
        { }

        //[DataMember]
        //public string productName { get; set; } = string.Empty;
        

        [DataMember]
        public string formatDatePrint;

        [DataMember]
        public List<string> productNumbers = new List<string>();

        
        public DateTime ManufactureDate
        {
            get { return DateTime.Parse(Date); }
        }
        [DataMember]
        public int numBoxInPallet { get; set; } = 0;
        [DataMember]
        public int packWeightGramm { get; set; } = 0;

        public override bool CheckContent()
        {

            //if(productNumbers == null) 
            //    throw new JobFormatException("Масив productNumbers отсутствует в задании.");

            //if (productNumbers.Count == 0)
            //    throw new JobFormatException("Масив productNumbers пуст.");


            if (base.CheckContent())
                return true;

            throw new JobFormatException("Тег palletLabelFields должен содержать объект #productName#");
        }
    }


    [DataContract]
    public class PartAggSrvBoxNumber : ReadyBox
    {
        [DataMember]
        public NumberState state { get; set; }
        [DataMember]
        public string tsdId { get; set; }//номер тсд который работает с коробом. или работал с ним.
        [DataMember]
        public int PalId { get; set; } = -1;//Id записи палеты в массисе readyPalets описывающий палету в которой числится короб
        [DataMember]
        public bool ManualCodeAdded { get; set; }
    
        public string GS1SerialOrSSCC18 { get
            {
                GsLabelData gs = new GsLabelData(boxNumber);
                if (!string.IsNullOrEmpty(gs.SerialShippingContainerCode00))
                    return gs.SerialShippingContainerCode00;
                else if (!string.IsNullOrEmpty(gs.SerialNumber))
                    return gs.SerialNumber;
                else
                    return boxNumber;
            } }

        public PartAggSrvBoxNumber() : this("") { }
        public PartAggSrvBoxNumber(string n) { boxNumber = n; ManualCodeAdded = false; }
        public PartAggSrvBoxNumber Clone()
        {
            PartAggSrvBoxNumber o = new PartAggSrvBoxNumber(boxNumber);
            //
            o.date = date;
            o.productNumbers.AddRange(productNumbers);
            o.boxTime = boxTime;
            o.id = id;
            o.ManualCodeAdded = ManualCodeAdded;
            o.PalId = PalId;
            return o;
        }
    }

    [DataContract]
    public class LayerItem
    {
        [DataMember]
        public string number;
        [DataMember]
        public string fn;//полный номер продукта так как он считан сканером 
        [DataMember]
        public int layerNum;

        public LayerItem() : this("", 0,"") { }
        public LayerItem(string num, int lay,string fullCode) { number = num; layerNum = lay;fn = fullCode; }
    }

    public class LastLayer
    {
        private int _CodeCount;
        private int _Number;
        private bool _LayerIsFull;
        private bool _LayerManualAdd;

        public LastLayer(int ln, int cc, bool nf, bool m)
        {
            _CodeCount = cc;
            _Number = ln;
            _LayerIsFull = nf;
            _LayerManualAdd = m;
        }
        public int CodeCount { get { return _CodeCount; } }
        public int Number { get { return _Number; } }
        public bool LayerIsFull { get { return _LayerIsFull; } }
        public bool LayerManualAdd { get { return _LayerManualAdd; } }
    }

    [DataContract]
    public enum BoxWithLayersPlace
    {
        Unknow,
        Left,
        Right
    }
    [DataContract]
    public class BoxWithLayersOld
    {
        [DataMember]
        public BoxWithLayersPlace Place { get; set; }

     
        public bool CloseNotFull = false;
        [DataMember]
        private int _layerNum = 0;
        [DataMember]
        private int _maxLayers = 1;
        [DataMember]
        private int _maxNumbers = 1;
        [DataMember]
        public string Number = "";
        //[DataMember]
        //public string ReplaceNumber = "";
        [DataMember]
        public List<LayerItem> Numbers = new List<LayerItem>();

        public BoxWithLayersOld() : this("", 1, 1) { }
        public BoxWithLayersOld(string n, int maxLayers, int maxNumbers):this (n,maxLayers,maxNumbers,BoxWithLayersPlace.Unknow){ }
        public BoxWithLayersOld(string n, int maxLayers, int maxNumbers, BoxWithLayersPlace p)
        {
            Number = n;
            ManualCodeAdded = false;
            _maxNumbers = maxNumbers;
            _maxLayers = maxLayers;
            Place = p;
        }

        public int NumbersCount { get { return Numbers.Count; } }
        public int LayerNum { get { return _layerNum; } }

        [DataMember]//флаг того что в короб добавили элемент вручную сканером!
        public bool ManualCodeAdded { get; set; }
        public LastLayer LastLayer
        {
            get
            {
                //вычислить количество элементов в последнем слое
                int cc = (Numbers.FindAll(x => x.layerNum == _layerNum)).Count;
                //вычислить полный слой или нет
                bool LayerIsFull = false;
                try
                {
                    //посчитать максимальное количество в слое
                    int mlc = _maxNumbers / _maxLayers;
                    if (mlc == cc)
                        LayerIsFull = true;
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }


                return new LastLayer(_layerNum, cc, LayerIsFull, ManualCodeAdded);
            }
        }


        public BoxWithLayersOld Clone()
        {
            BoxWithLayersOld o = new BoxWithLayersOld(Number, _maxLayers, _maxNumbers);
            o.Place = Place;
            o.CloseNotFull = CloseNotFull;
            //
            o.ManualCodeAdded = ManualCodeAdded;
            o.Numbers.AddRange(Numbers);
            //o.boxTime = boxTime;
            //o.id = id;
            return o;
        }
        //добавлять можно только уникальные для текущего короба коды
        //добавляет неполный слой
        public int PartOfLayer(List<string> layer)
        {
            //проверить на превышение количества слоев
            if (_layerNum + 1 > _maxLayers)
                return -1;

            _layerNum++;
            //проверить на уникальность
            foreach (string s in layer)
            {
                foreach (LayerItem l in Numbers)
                {
                    if (l.number == s)
                        return -1;
                }
            }

            //все номера уникальны добавляем новый слой 
            foreach (string s in layer)
                Numbers.Add(new LayerItem(s, _layerNum,""));

            return _layerNum;
        }
        //добавлять можно только уникальные для текущего короба коды
        public int AddLayer(List<string> layer)
        {
            //проверить на превышение количества слоев
            if (_layerNum + 1 > _maxLayers)
                return -1;

            _layerNum++;
            //проверить на уникальность
            foreach (string s in layer)
            {
                foreach (LayerItem l in Numbers)
                {
                    if (l.number == s)
                        return -1;
                }
            }

            //все номера уникальны добавляем новый слой 
            foreach (string s in layer)
                Numbers.Add(new LayerItem(s, _layerNum,""));

            //если количество кодов в слое соответствует ожидаемому поставить признак слой собран
            //?!?!?!??!


            return _layerNum;
        }
        public bool AddItem(string item, string fullNumber)
        {
            //проверить на уникальность
            foreach (LayerItem l in Numbers)
            {
                if (l.number == item)
                    return false;
            }

            // номера уникальны добавляем новый слой 
            Numbers.Add(new LayerItem(item, _layerNum, fullNumber));

            return true;
        }
        public bool IsAlreadyInBox(string number)
        {
            foreach (LayerItem l in Numbers)
            {
                if (l.number == number)
                    return true;
            }
            return false;
        }
     

        
        public bool RemoveItem(string number)
        {
            for (int i = 0; i < Numbers.Count; i++)
            {
                if (Numbers[i].number == number)
                {
                    Numbers.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool ClearLastLayer()
        {
            //очистить последний добавленный уровень
            if (_layerNum < 1)
                return false;

            //удалить все с номером слоя
            Numbers.RemoveAll(x => x.layerNum == _layerNum);
            //уменьшить счетчик
            _layerNum--;

            return false;
        }

        public bool ClearBox()
        {
            Numbers.Clear();
            _layerNum = 0;
            ManualCodeAdded = false;
            return true;
        }
    }

    //Класс задания выполняющегося на ТСД
    [DataContract]
    public class LineAggregateJobInfo : BaseJobInfo
    {
        [DataMember]
        public List<PartAggSrvBoxNumber> BoxInWorck = new List<PartAggSrvBoxNumber>();
        [DataMember]
        public List<FullPartAggPallete> PalletInWorck = new List<FullPartAggPallete>();
        [DataMember]
        public int BoxAvailable { get; set; }
        [DataMember]
        public int BoxVerify { get; set; }
        [DataMember]
        public int PalleteAvailable { get; set; }
        
        public LineAggregateJobInfo(): base() { }
    }

    //Класс задания выполняющегося на ТСД
    [DataContract]
    public class FullNumLineAggregateJobInfo : BaseJobInfo
    {
        [DataMember]
        public List<FullSerializeBox> BoxInWorck = new List<FullSerializeBox>();
        [DataMember]
        public List<FullPartAggPallete> PalletInWorck = new List<FullPartAggPallete>();
        [DataMember]
        public int BoxAvailable { get; set; }
        [DataMember]
        public int BoxVerify { get; set; }
        [DataMember]
        public int PalleteAvailable { get; set; }

        public FullNumLineAggregateJobInfo() : base() { }
    }


    //Класс задания выполняющегося на ТСД
    [DataContract]
    public class LineAggregateJob : baseTsdAccJob<PartAggregate1СOrder>///AggCorobBaseInfo, IBaseJob
    {
        [DataMember]
        public List<DefectiveCode> brackBox = new List<DefectiveCode>();
     
        [DataMember]
        public PartAggSrvBoxNumber selectedBox = new PartAggSrvBoxNumber("");
   
        [DataMember]
        public string operatorId { get; set; }
        [DataMember]
        public bool labelPrint { get; set; }
        [DataMember]
        public bool printBoxLabel { get; set; }
        [DataMember]
        public string checkedNumber { get; set; }
        [DataMember]
        public string msg { get; set; }

        public LineAggregateJob()
            : base()
        {
            //jobType = typeof(LineAggregateJob);
        }

        //возвращает первый не использованный номер коробки из задания
        public bool IsPackGoodForAdd(string number)
        {
            //проверка в массивах уже отработанных
            if (selectedBox.IsAlreadyInBox(number))
                return false;

            return false;
        }

    }

    //базовый класс для класов данных аггрегации коробов
    [DataContract]
    public class AggCorobBaseInfo : BaseJobInfo
    {
        public AggCorobBaseInfo()
            : base()
        {
            id = "";
            GTIN = "";
            lotNo = "";
            //expDate = "";
            //addProdInfo = "";
            prefixBoxCode = "";
            Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        }
        public DateTime date;

        [DataMember]
        public string Date
        {
            get { return date.ToString("yyyy-MM-ddThh:mm:ssz"); }
            set {
                if (value != null)
                {
                    if (value.Length == 6)
                    {
                        String dFormat = "yyMMdd";
                        date = DateTime.ParseExact(value, dFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                    }
                    else
                        date = DateTime.Parse(value);
                }
                else
                    date = DateTime.MinValue;

            }
        }

        [DataMember]
        public int numРacksInBox { get; set; } = 0;
        [DataMember]
        public int numLayersInBox { get; set; } = 1;
        [DataMember]
        public int numPacksInLayer { get; set; } = 0;
        [DataMember]
        public int numLabelAtBox = 0;
        [DataMember]
        public string formatExpDate = "";
        [DataMember]
        public string urlLabelBoxTemplate = "";

        [DataMember]
        public string prefixBoxCode { get; set; }

        [DataMember]
        public List<LabelField> boxLabelFields = new List<LabelField>();
    }


    public delegate void OrderAcceptedEventHandler(object sender);

    //Класс отчета в 1С
    [DataContract]
    public class PartAggregateReport
    {
        [DataMember]
        public string id = "";
        [DataMember]
        public string startTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public string endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public bool partOfList = false;
        [DataMember]
        public List<ReadyBox> readyBox = new List<ReadyBox>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
    }

    [DataContract]
    public class LineAggregateHelpData : AggCorobBaseInfo, IBaseJob
    {
        private OrderMeta meta = new OrderMeta();

        #region Реализация интерфейса BaseJob

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

                meta.name = "Серия:" + lotNo + "\r" + name;
                meta.id = id;
                meta.type = 0;
                meta.state = 0;
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

                if (JobState == JobStates.CloseAndAwaitSend)
                    return false;

                return true;
            }
        }

        public object GetTsdJob()
        {
            return null;
        }
        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.WaitSend)
                    return true;
                else
                    return false;
            }
        }

        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat) { return " Нет реализации"; }
        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Справка"; }
        #endregion
    }
    #endregion

    #region Агрегация в полуавтомате с камерой
    [DataContract]
    public class Sampled
    {

        public Sampled(string n, string operatorId)
        {
            number = n;
            id = operatorId;
        }
        public Sampled()
        {
            number = "";
            id = "";
        }
        [DataMember]
        public string number = "";
        [DataMember]
        public string id = "";
        [DataMember]
        public string time = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
    }

    //Класс отчета в 1С для 
    [DataContract]
    public class PartAggregateOSRReport
    {
        [DataMember]
        public string id = "";
        [DataMember]
        public string startTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public string endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public bool partOfList = false;
        [DataMember]
        public List<ReadyBox> readyBox = new List<ReadyBox>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
        [DataMember]
        public List<Sampled> sampledCodes = new List<Sampled>();

    }
    #endregion

    #region Агрегация биотики

    [DataContract]
    public class BiotikiReport : BaseReportInfo
    {
        [DataMember]
        public List<ReadyBox> readyBox = new List<ReadyBox>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
        [DataMember]
        public List<Operator> operators = new List<Operator>();

    }
    #endregion

    #region Справка
    [DataContract]
    public class LinkDataToHeplService
    {
        [DataMember]
        public string link;

        [DataMember]
        public string login;

        [DataMember]
        public string pass;
    }

    [DataContract]
    public class Help1СAnswer : BaseJobInfo
    {
        [DataMember]
        public bool stock;
        [DataMember]
        public string formatExpDate;
        [DataMember]
        public string manufacturedDate;
        [DataMember]
        public string formatManufacturedDate;
        [DataMember]
        public string itemTypeName;
        [DataMember]
        public string ParentContainerNumber;
        [DataMember]
        public string currentStatus; //состояние по 1с. типа выпушен, списан итд..
        [DataMember]
        public int itemCount;  //количество вложенных в этот контейнер пачек\коробов\

        [DataMember]
        public string containerNumber;
        [DataMember]
        public string containerNumberType;
        [DataMember]
        public int numLabelAtContainer;
        [DataMember]
        public int numPacksInContainer;// numРacksInContainer;
        [DataMember]
        public string urlLabelContainerTemplate;

        [DataMember]
        public List<LabelField> containerLabelFields = new List<LabelField>();

        public Help1СAnswer()
        {
            id = "";
            GTIN = "";
            lotNo = "";
            //expDate = "";
            //addProdInfo = "";
            jobType = typeof(Help1СAnswer);

            //Date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        }

        public override bool CheckContent()
        {
            return true;
        }
    }
    #endregion

    #region чтоб не мешалось
    // Задание на агрегацию всей серии

    [DataContract]

    public class PartAggregateOrder
    {

        [DataContract]

        public class Pallet
        {

            [DataMember]

            public string Number;

            [DataMember]

            public List<string> boxNumbers = new List<string>();

            public bool IsBoxInPallete(string num)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == num)
                        return true;
                }
                return false;
            }
        }
        public PartAggregateOrder()
        {
            id = "";

            lotNo = "";

            productName = "";
            productDescription = "";
            numBoxesInPallet = 0;
            numLabelAtPallet = 0;
        }

        [DataMember]

        public string id { get; set; }


        [DataMember]

        public string GTIN { get; set; }

        [DataMember]

        public string lotNo { get; set; }

        [DataMember]

        public string boxPrefix { get; set; }

        [DataMember]

        public string expDate { get; set; }
        // public string expDate { get; set; }     
        // public string addProdInfo { get; set; }  

        [DataMember]

        public string productDescription { get; set; }

        [DataMember]

        public string productName { get; set; }

        [DataMember]

        public int numBoxesInPallet { get; set; }

        [DataMember]

        public int numLabelAtPallet { get; set; }

        [DataMember]

        public int numРacksInBox { get; set; }

        [DataMember]

        public int numRowInBox { get; set; }

        [DataMember]

        public string lineNum = "";

        [DataMember]

        public string LabelPalletTemplate = "http://l3/label/pal.zpl";

        [DataMember]

        public string LabelBoxTemplate = "http://l3/label/box.zpl";


        [DataMember]

        public List<PartAggregateOrder.Pallet> pallets = new List<PartAggregateOrder.Pallet>();



        public bool CheckContent()
        {
            try
            {
                if (id == null)
                    return false;

                if (productDescription == null)
                    return false;

                if (lotNo == null)
                    return false;

                if (productName == null)
                    return false;

                if (lineNum == "")
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return false;
        }
    }
    public class PartPallet
    {

        public PartPallet()
        {
        }



        [DataMember]

        public string Number { get; set; }

        [DataMember]

        public string startTime { get; set; }

        [DataMember]

        public string endTime { get; set; }

        [DataMember]

        public string PalletType { get; set; }

        [DataMember]

        public List<Box> boxes = new List<Box>();

        public bool IsPackAlreadyInPallete(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.IsAlreadyInBox(number))
                    return true;
            }
            return false;
        }
        public bool IsBoxAlreadyInPallete(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.Number == number)
                    return true;
            }
            return false;
        }

        public bool RemoveBox(string number)
        {
            foreach (Box s in boxes)
            {
                if (s.Number == number)
                {
                    boxes.Remove(s);
                    return true;
                }
            }

            return false;
        }
    }
    public class PartAggregateJob : IBaseJob
    {
        public PartAggregateOrder order;
        public List<string> brackBox = new List<string>();
        private OrderMeta meta = new OrderMeta();



        public PartPallet currentPallete = new PartPallet();
        public bool printBoxLabel { get; set; }

        public string startTime { get; set; }
        public string operatorId { get; set; }
        public bool labelPrint { get; set; }

        public List<PartPallet> readyPallet = new List<PartPallet>();

        #region Реализация интерфейса BaseJob
        [DataMember]
        public OrderMeta JobMeta
        {
            get
            {
                meta.id = order.id;
                meta.name = order.productName;
                meta.type = 1;
                meta.state = JobIcon.Default;
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

                return true;
            }
        }

        public object GetTsdJob()
        {
            return null;
        }
        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.WaitSend)
                    return true;
                else
                    return false;
            }
        }

        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat) { return " Нет реализации"; }
        public object GetReport() { throw new NotImplementedException(); }

        public string GetFuncName() { return "Агрегация"; }
        #endregion


        //возвращает первый не использованный номер коробки из задания
        public string GetNewNumberForBox()
        {
            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                foreach (string b in op.boxNumbers)
                {
                    if (!currentPallete.IsBoxAlreadyInPallete(b))
                        return b;
                }
            }
            return "";
        }
        public bool IsBoxGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю


            //проверка в массивах уже отработанных
            if (currentPallete.IsBoxAlreadyInPallete(number))
                return false;

            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsBoxAlreadyInPallete(number))
                    return false;
            }

            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                if (op.IsBoxInPallete(number))
                    return true;
            }
            return false;
        }
        public bool IsPackGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю

            //проверка в массивах уже отработанных
            if (currentPallete.IsPackAlreadyInPallete(number))
                return false;


            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsPackAlreadyInPallete(number))
                    return false;
            }

            //проверка по массиву доступных
            //foreach (string s in order.p)
            //{
            //    if (s == number)
            //        return true;
            //}
            return false;
        }
        public bool IsPalletGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю
            if (number == "")
                return false;

            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.Number == number)
                    return false;
            }

            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet s in order.pallets)
            {
                if (s.Number == number)
                    return true;
            }
            return false;
        }
        public string GetPalletAtNumBox(string boxNum)
        {
            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsPackAlreadyInPallete(boxNum))
                    return p.Number;
            }

            //проверка в задании
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                if (op.IsBoxInPallete(boxNum))
                    return op.Number;
            }
            return "";
        }
        public PartAggregateOrder.Pallet GetNextPallet()
        {
            bool bFound = false;
            foreach (PartAggregateOrder.Pallet pn in order.pallets)
            {
                bFound = true;
                //проверка в массивах уже отработанных
                foreach (PartPallet p in readyPallet)
                {
                    if (p.Number == pn.Number)
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                    return pn;
            }
            return null;
        }
        public int BoxesWithoutPallet()
        {
            //int b = 0;
            //foreach (PartPallet p in readyPallet)
            //{
            //    b += p.boxes.Count;
            //}
            //return order.boxNumbers.Count - b;
            return 0;
        }
        public PartPallet GetReadyPalletAtNum(string num)
        {
            foreach (PartPallet pn in readyPallet)
            {
                if (pn.Number == num)
                    return pn;
            }
            return null;
        }
    }

    public class PartAggregateJobOld : IBaseJob
    {
        public PartAggregateOrder order;
        public List<string> brackBox = new List<string>();

        private OrderMeta meta = new OrderMeta();


        //public  Type JobType { get { return typeof(PartAggregateJob); } }

        public PartPallet currentPallete = new PartPallet();
        public bool printBoxLabel { get; set; }

        public string startTime { get; set; }
        public string operatorId { get; set; }
        public bool labelPrint { get; set; }

        public List<PartPallet> readyPallet = new List<PartPallet>();

        #region Реализация интерфейса BaseJob
        [DataMember]
        public OrderMeta JobMeta
        {
            get
            {
                meta.id = order.id;
                meta.name = order.productName;
                meta.type = 1;
                meta.state = JobIcon.Default;
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

                return true;
            }
        }

        public bool WaitSend
        {
            get
            {
                if (JobState == JobStates.WaitSend)
                    return true;
                else
                    return false;
            }
        }

        public string ParceReport<T>(T rep) { throw new NotImplementedException(); }
        public string SendReports(string url, string user, string pass, bool partOfList, int reguestTimeOut, bool repeat) { return " Нет реализации"; }
        public object GetReport() { throw new NotImplementedException(); }
        public string GetFuncName() { return "Агрегация"; }
        #endregion

        //возвращает первый не использованный номер коробки из задания
        public string GetNewNumberForBox()
        {
            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                foreach (string b in op.boxNumbers)
                {
                    if (!currentPallete.IsBoxAlreadyInPallete(b))
                        return b;
                }
            }
            return "";
        }
        public object GetTsdJob()
        {
            return null;
        }

        public object GetTsdSqLiteJob() { throw new NotImplementedException(); }
        public bool IsBoxGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю


            //проверка в массивах уже отработанных
            if (currentPallete.IsBoxAlreadyInPallete(number))
                return false;

            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsBoxAlreadyInPallete(number))
                    return false;
            }

            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                if (op.IsBoxInPallete(number))
                    return true;
            }
            return false;
        }
        public bool IsPackGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю

            //проверка в массивах уже отработанных
            if (currentPallete.IsPackAlreadyInPallete(number))
                return false;


            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsPackAlreadyInPallete(number))
                    return false;
            }

            //проверка по массиву доступных
            //foreach (string s in order.p)
            //{
            //    if (s == number)
            //        return true;
            //}
            return false;
        }
        public bool IsPalletGoodForAdd(string number)
        {
            //поиск по двум масивам.. 
            //по хорошему надо сдлеать 1 с признаками но и хуй с ним и так хер успеваю
            if (number == "")
                return false;

            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.Number == number)
                    return false;
            }

            //проверка по массиву доступных
            foreach (PartAggregateOrder.Pallet s in order.pallets)
            {
                if (s.Number == number)
                    return true;
            }
            return false;
        }
        public string GetPalletAtNumBox(string boxNum)
        {
            //проверка в массивах уже отработанных
            foreach (PartPallet p in readyPallet)
            {
                if (p.IsPackAlreadyInPallete(boxNum))
                    return p.Number;
            }

            //проверка в задании
            foreach (PartAggregateOrder.Pallet op in order.pallets)
            {
                if (op.IsBoxInPallete(boxNum))
                    return op.Number;
            }
            return "";
        }
        public PartAggregateOrder.Pallet GetNextPallet()
        {
            bool bFound = false;
            foreach (PartAggregateOrder.Pallet pn in order.pallets)
            {
                bFound = true;
                //проверка в массивах уже отработанных
                foreach (PartPallet p in readyPallet)
                {
                    if (p.Number == pn.Number)
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                    return pn;
            }
            return null;
        }
        public int BoxesWithoutPallet()
        {
            //int b = 0;
            //foreach (PartPallet p in readyPallet)
            //{
            //    b += p.boxes.Count;
            //}
            //return order.boxNumbers.Count - b;
            return 0;
        }
        public PartPallet GetReadyPalletAtNum(string num)
        {
            foreach (PartPallet pn in readyPallet)
            {
                if (pn.Number == num)
                    return pn;
            }
            return null;
        }
    }


    [DataContract]

    public class ShippingJobOrderOld
    {



        [DataContract]

        public class Pallet
        {

            [DataMember]

            public string Number;

            [DataMember]

            public List<string> boxNumbers = new List<string>();

            public bool IsBoxInPallete(string num)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == num)
                        return true;
                }
                return false;
            }
        }


        [DataContract]

        public class Item
        {
            public Item(string g, string ln, string fl, int num, int t)
            {
                GTIN = g;
                lotNo = ln;
                fullNumber = fl;
                numРacks = num;
                type = t;
            }

            [DataMember]

            public string GTIN { get; set; }

            [DataMember]

            public string lotNo { get; set; }


            [DataMember]

            public string expDate { get; set; }

            [DataMember]

            public string fullNumber { get; set; }

            [DataMember]

            public int numРacks { get; set; }

            [DataMember]

            public int type { get; set; }//0-def,1-pallete,2-box,3-коробка
        }


        [DataContract]

        public class Product
        {

            [DataMember]

            public string GTIN { get; set; }

            [DataMember]

            public string lotNo { get; set; }


            [DataMember]

            public string expDate { get; set; }

            [DataMember]

            public string productName { get; set; }

            [DataMember]

            public int numРacksInBox { get; set; }

            [DataMember]

            public int numBoxesInPallet { get; set; }

            [DataMember]

            public int number { get; set; }

            [DataMember]

            public List<ShippingJobOrderOld.Pallet> palletsNumbers = new List<ShippingJobOrderOld.Pallet>();

            public bool PalletIsPresent(string num)
            {
                foreach (ShippingJobOrderOld.Pallet p in palletsNumbers)
                {
                    if (p.Number == num)
                        return true;
                }
                return false;
            }
            public bool BoxIsPresent(string num)
            {
                foreach (ShippingJobOrderOld.Pallet p in palletsNumbers)
                {
                    foreach (string b in p.boxNumbers)
                    {
                        if (b == num)
                            return true;
                    }
                }
                return false;
            }

        }

        public ShippingJobOrderOld()
        {
        }

        [DataMember]

        public string id { get; set; }


        [DataMember]

        public string Customer { get; set; }

        [DataMember]

        public string Number { get; set; }




        [DataMember]

        public List<ShippingJobOrderOld.Product> products = new List<ShippingJobOrderOld.Product>();

        public ShippingJobOrderOld.Item GetObjectAtCode(string num)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                //попробовать распознать код 
                Util.GsLabelData ld = new Util.GsLabelData(num);
                //string number = ld.SerialNumber;


                //начинаем поиск по всей бд.... 
                foreach (ShippingJobOrderOld.Product pr in products)
                {
                    foreach (Pallet pl in pr.palletsNumbers)
                    {
                        //проверить номер палеты
                        if (pl.Number == num)
                            return new ShippingJobOrderOld.Item(pr.GTIN, pr.lotNo, num, (pr.numBoxesInPallet * pr.numРacksInBox), 1);

                        //номер не палетный проверяем все номера коробок в етой палете 
                        //может это номер коробки?
                        foreach (string s in pl.boxNumbers)
                        {
                            //проверить номер палеты
                            if (s == num)
                                return new ShippingJobOrderOld.Item(pr.GTIN, pr.lotNo, num, pr.numРacksInBox, 2);
                        }
                    }

                    //код не палетный.. может код пачки?
                    //проверить GTIN експ дате и 
                    if (ld.GTIN == pr.GTIN)
                        return new ShippingJobOrderOld.Item(pr.GTIN, pr.lotNo, ld.GTIN, 1, 3);

                }
            }
            catch { }
            finally
            {
                Console.WriteLine(sw.ElapsedMilliseconds);
                sw.Stop();
            }

            return null;
        }

    }

    //класс хранения данных по отгрузке




    /*
     * строка	id	Уникальный идентификатор задания, будет присвоен отчету по этому заданию
строка	GTIN 	Номер GTIN. 14 символов 
строка	lotNo 	Номер производственной серии. До 20 символов
строка	expDate 	Дата истечения срока годности. 6 символов в формате «ГГММДД».
строка	productName	Наименование продукта для отображения на дисплее
массив объектов Pallet	palletNumbers	массив уникальных номеров Pallet описывающие номера паллет и содержащиеся в них номера коробов
строка	lastBox 	Номер последнего короба (не полного) 
int	numProductInlastBox	Кол-во пачек в последнем коробе.
Файл или ссылка	LabelPalletTemplate 	Шаблон этикетки для печати на третичной упаковке (паллете)
Файл или ссылка	LabelBoxTemplate 	Шаблон этикетки для печати на третичной упаковке (коробе)
int	numРacksInBox 	Кол-во упаковок в коробе
int	numBoxesInPallet 	Кол-во коробов в полной паллете
int	numLabelAtPallet	Количество этикеток на паллету
int	numRowInBox	Кол-во слоев в коробе

     */


    [DataContract]

    public class RepackOrder
    {
        public class Pallete
        {
            public string palletNumber;
            public List<string> boxNumbers = new List<string>();

            public Pallete() { }

            public bool IsBoxInPallete(string num)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == num)
                        return true;
                }
                return false;
            }
        }
        public RepackOrder()
        {
            id = "";

            lotNo = "";

            productName = "";
            productDescription = "";
            numBoxesInPallet = 0;
            numLabelAtPallet = 0;
        }

        [DataMember]

        public string id { get; set; }
        //public string gtin { get; set; }  

        [DataMember]

        public string lotNo { get; set; }
        // public string expDate { get; set; }     
        // public string addProdInfo { get; set; }  

        [DataMember]

        public string productDescription { get; set; }

        [DataMember]

        public string productName { get; set; }

        [DataMember]

        public int numProductInlastBox { get; set; }

        [DataMember]

        public int numРacksInBox { get; set; }

        [DataMember]

        public int numBoxesInPallet { get; set; }

        [DataMember]

        public int numLabelAtPallet { get; set; }

        [DataMember]

        public int numRowInBox { get; set; }



        [DataMember]

        public string LabelPalletTemplate = "http://l3/label/pal.zpl";

        [DataMember]

        public string LabelBoxTemplate = "http://l3/label/box.zpl";


        [DataMember]

        public List<RepackOrder.Pallete> palletNumbers = new List<RepackOrder.Pallete>();



        public bool CheckContent()
        {
            try
            {
                if (id == null)
                    return false;

                if (productDescription == null)
                    return false;

                if (lotNo == null)
                    return false;

                if (productName == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return false;
        }
    }

    //класс хранения данных по отгрузке

    [DataContract]
    public class RepackJobOld
    {
        public enum ItemType
        {
            Неизвестно = 0,
            Паллета = 1,
            Короб = 2,
            Упаковка = 3
        }


        [DataContract]

        public class Item
        {
            public ItemType type;//0-def,1-pallete,2-box,3-коробка
            public string fullNum;
            public int numberItemInPack;

            public Item(int t, int n, string np)
            {
                type = (ItemType)t;
                numberItemInPack = n;
                fullNum = np;
            }
        }

        [DataContract]

        public class Product
        {
            public Product(string n, int num, string gt)
            {
                name = n;
                number = num;
                GTIN = gt;
            }

            [DataMember]

            public string GTIN { get; set; }

            [DataMember]

            public string name { get; set; }

            [DataMember]

            public int number { get; set; }

            [DataMember]

            public int allreadyNum { get; set; }


            [DataMember]

            public List<string> Pallets = new List<string>();

            [DataMember]

            public List<string> Boxs = new List<string>();

            [DataMember]

            public List<string> Pack = new List<string>();


            [DataMember]

            public List<Item> SelectedItems = new List<Item>();

            public bool Add(RepackJobOld.Item itm)
            {

                return false;
            }

            public bool Remove(RepackJobOld.Item itm)
            {
                return false;
            }

        }


        [DataContract]

        public class Pallet
        {

            public Pallet()
            {
            }



            [DataMember]

            public string Number { get; set; }

            [DataMember]

            public string startTime { get; set; }

            [DataMember]

            public string endTime { get; set; }

            [DataMember]

            public string PalletType { get; set; }

            [DataMember]

            public List<string> boxNumbers = new List<string>();



            public bool IsAlreadyInPallete(string number)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == number)
                        return true;
                }

                return false;
            }

            public bool RemoveBox(string number)
            {
                foreach (string s in boxNumbers)
                {
                    if (s == number)
                    {
                        boxNumbers.Remove(s);
                        return true;
                    }
                }

                return false;
            }
        }


        public RepackJobOld()
        {
        }

        [DataMember]

        public string id { get; set; }

        public string operatorId { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }

        public RepackOrder order { get; set; }


        [DataMember]

        public List<string> defectiveCodes = new List<string>();

        [DataMember]

        public List<string> defectiveBox = new List<string>();

        [DataMember]

        public List<string> defectivePallet = new List<string>();


        [DataMember]

        public List<Operator> operators = new List<Operator>();

        public bool IsPalletAlreadyDisbanded(string number)
        {
            //проверка по массиву уже расформированных
            foreach (string s in defectivePallet)
            {
                if (s == number)
                    return true;
            }
            return false;
        }
        public bool IsBoxAlreadyDisbanded(string number)
        {
            //проверка по массиву уже расформированных
            foreach (string s in defectiveBox)
            {
                if (s == number)
                    return true;
            }
            return false;
        }
        public bool IsPackAlreadyDisbanded(string number)
        {
            //проверка по массиву уже расформированных
            foreach (string s in defectiveCodes)
            {
                if (s == number)
                    return true;
            }
            return false;
        }



        public bool CheckBoxNum(string boxNum)
        {
            //проверка в задании
            foreach (RepackOrder.Pallete op in order.palletNumbers)
            {
                if (op.IsBoxInPallete(boxNum))
                    return true;
            }
            return false;
        }

        public string GetPalletCodeAtNumBox(string boxNum)
        {
            //проверка в задании
            foreach (RepackOrder.Pallete op in order.palletNumbers)
            {
                if (op.IsBoxInPallete(boxNum))
                    return op.palletNumber;
            }
            return "";
        }

        public RepackOrder.Pallete GetPalletAtNumBox(string boxNum)
        {
            //проверка в задании
            foreach (RepackOrder.Pallete op in order.palletNumbers)
            {
                if (op.IsBoxInPallete(boxNum))
                    return op;
            }
            return null;
        }

        public RepackOrder.Pallete GetPalleteAtCode(string number)
        {
            RepackOrder.Pallete result = new RepackOrder.Pallete();

            //проверить код палеты ?
            foreach (RepackOrder.Pallete s in order.palletNumbers)
            {
                if (s.palletNumber == number)
                    return s;
            }

            //проверить код коробки ?
            result = GetPalletAtNumBox(number);
            return result;
        }
    }

    #endregion

}