using FSerialization;
using System;
using System.Collections.Generic;

namespace BoxAgr.BLL.Models
{
    public class Unit
    {
        public string Number { get; set; } = "";
        public int LayerNum { get; set; }
        public string Barcode { get; set; } = "";
        public string StatusInfo { get; set; } = "";
        /// <summary>
        /// Номер короба в котором находится продукт
        /// </summary>
        public string BoxNumber { get; set; } = "";

        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public Int32 Width { get; set; } = 50;
        public Int32 CellRowId { get; set; }
        public Int32 CellColumId { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public CodeState CodeState { get; set; }

        public List<UnitPoint> Points { get; set; } = [];

    }

    public class UnitPoint
    {
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
    }
}
