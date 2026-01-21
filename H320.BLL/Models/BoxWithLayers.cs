using FSerialization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using static FSerialization.Box;
using BoxAgr.BLL.Exeptions;

namespace BoxAgr.BLL.Models
{
    public class BoxWithLayers
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
        public string Number { get;  } = "";
        [DataMember]
        public List<Unit> Numbers = new List<Unit>();

        public BoxWithLayers() : this("", 1, 1) { }
        public BoxWithLayers(string n, int maxLayers, int maxNumbers) : this(n, maxLayers, maxNumbers, BoxWithLayersPlace.Unknow) { }
        public BoxWithLayers(string n, int maxLayers, int maxNumbers, BoxWLState state) : this(n, maxLayers, maxNumbers, BoxWithLayersPlace.Unknow, state) { }
        public BoxWithLayers(string n, int maxLayers, int maxNumbers, BoxWithLayersPlace p) : this(n, maxLayers, maxNumbers, p, BoxWLState.Uncknow) { }
        public BoxWithLayers(string n, int maxLayers, int maxNumbers, BoxWithLayersPlace p, BoxWLState state)
        {
            Number = n;
            ManualCodeAdded = false;
            _maxNumbers = maxNumbers;
            _maxLayers = maxLayers;
            Place = p;
            State = state;
        }

        public int NumbersCount { get { return Numbers.Count + cLayer.Count; } }
        public int LayerNum { get { return _layerNum; } }
        public BoxWLState State { get; set; } = BoxWLState.Uncknow;

        [DataMember]//флаг того что в короб добавили элемент вручную сканером!
        public bool ManualCodeAdded { get; set; }
        public LastLayer LastLayer
        {
            get
            {
                //вычислить количество элементов в последнем слое
                int cc = (Numbers.FindAll(x => x.LayerNum == _layerNum)).Count;
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

        //текущий собираемый слой
        public  List<Unit> cLayer { get; } = new List<Unit>();
        private bool _IscLayerOpen;

        public BoxWithLayers Clone()
        {
            BoxWithLayers o = new BoxWithLayers(Number, _maxLayers, _maxNumbers);
            o.Place = Place;
            o.CloseNotFull = CloseNotFull;
            //
            o.ManualCodeAdded = ManualCodeAdded;
            o.Numbers.AddRange(Numbers);
            //o.boxTime = boxTime;
            //o.id = id;
            return o;
        }

        public bool OpenNewLayer()
        {
            //проверить на превышение количества слоев
            if (_layerNum + 1 > _maxLayers)
                return false; 

            _layerNum++;
            cLayer.Clear();
            _IscLayerOpen = true;
            return true;

        }
        public bool AddUnitToAssembledLayer(List<Unit> units)
        {
            if (!_IscLayerOpen)
                OpenNewLayer();
            
            

            //проверить на уникальность
            foreach (Unit s in units)
            {
                foreach (Unit l in Numbers)
                {
                    if (l.Barcode == s.Barcode)
                    {
                        s.CodeState = CodeState.ProductRepit;
                        throw new BoxInfoExeption($"Номер {s.Barcode} уже присутвтвует в слое {l.LayerNum}", units);
                    }
                }
                //все номера уникальны добавляем новый слой 
                if (s.CodeState == CodeState.Verify)
                {
                    s.LayerNum = _layerNum;
                    cLayer.Add(s);
                }
            }
            return true;
        }

        public bool AddUnitsToAssembledLayer(List<Unit> units)
        {
            if (!_IscLayerOpen)
                OpenNewLayer();

            //проверить на уникальность
            foreach (Unit s in units)
            {
                foreach (Unit l in Numbers)
                {
                    if (l.Barcode == s.Barcode)
                    {
                        s.CodeState = CodeState.ProductRepit;
                        throw new BoxInfoExeption($"Номер {s.Barcode} уже присутвтвует в слое {l.LayerNum}", units);
                    }
                }
                //все номера уникальны добавляем новый слой 
                if (s.CodeState == CodeState.Verify || s.CodeState == CodeState.ManualAdd)
                {
                    s.LayerNum = _layerNum;
                    cLayer.Add(s);
                }
            }
            return true;
        }
        public bool CloseAssembledLayerAndOpenNew()
        {
            Numbers.AddRange(cLayer.ToArray());
            cLayer.Clear();
            _IscLayerOpen = false;
            return OpenNewLayer();
        }
        public void CloseAssembledLayer()
        {
            Numbers.AddRange(cLayer.ToArray());
            cLayer.Clear();
            _IscLayerOpen = false;
        }
        public void ClearAssembledLayer()
        {
            cLayer.Clear();
        }

        //добавлять можно только уникальные для текущего короба коды
        //добавляет неполный слой
        //public int PartOfLayer(List<string> layer)
        //{
        //    //проверить на превышение количества слоев
        //    if (_layerNum + 1 > _maxLayers)
        //        return -1;

        //    _layerNum++;
        //    //проверить на уникальность
        //    foreach (string s in layer)
        //    {
        //        foreach (Unit l in Numbers)
        //        {
        //            if (l.Barcode == s)
        //                return -1;
        //        }
        //    }

        //    //все номера уникальны добавляем новый слой 
        //    foreach (string s in layer)
        //        Numbers.Add(new Unit() { Barcode = s, LayerNum = _layerNum, Number = "" });

        //    return _layerNum;
        //}
        //добавлять можно только уникальные для текущего короба коды
        public int AddLayer(List<Unit> layer)
        {
            //проверить на превышение количества слоев
            if (_layerNum + 1 > _maxLayers)
                return -1;

            _layerNum++;
            //проверить на уникальность
            foreach (Unit s in layer)
            {
                foreach (Unit l in Numbers)
                {
                    if (l.Barcode == s.Barcode)
                    {
                        s.CodeState = CodeState.ProductRepit;
                        throw new BoxInfoExeption($"Номер {s.Barcode} уже присутвтвует в слое {l.LayerNum}", layer);
                    }
                }
                //все номера уникальны добавляем новый слой 
                if (s.CodeState == CodeState.Verify)
                {
                    s.LayerNum = _layerNum;
                    Numbers.Add(s);
                }
            }

            return _layerNum;
        }

        public bool AddItem(string item, string fullNumber)
        {
            //проверить на уникальность
            foreach (Unit l in Numbers)
            {
                if (l.Number == item)
                    return false;
            }

            // номера уникальны добавляем новый слой 
            Numbers.Add(new Unit() { Number = item, LayerNum = _layerNum, Barcode = fullNumber });

            return true;
        }
        public bool IsAlreadyInBox(string number)
        {
            foreach (Unit l in Numbers)
            {
                if (l.Number == number)
                    return true;
            }
            return false;
        }

        public bool RemoveItem(string number)
        {
            for (int i = 0; i < Numbers.Count; i++)
            {
                if (Numbers[i].Number == number)
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
            Numbers.RemoveAll(x => x.LayerNum == _layerNum);
            //уменьшить счетчик
            _layerNum--;
            _IscLayerOpen = false;

            return false;
        }
       
        public bool ClearBox()
        {
            Numbers.Clear();
            _layerNum = 0;
            ManualCodeAdded = false;
            _IscLayerOpen = false;
            return true;
        }
    }
}
