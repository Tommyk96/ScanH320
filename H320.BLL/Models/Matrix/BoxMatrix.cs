using System;

namespace BoxAgr.BLL.Models.Matrix
{
    public class BoxMatrix : ICloneable
    {
      
        public bool BoxGrid { get; set; }
        public string GTIN { get; set; } = "";
        public string Name { get; set; } = "";

        public object Clone()
        {
            return MemberwiseClone();
        }
        public void CopyFrom(BoxMatrix obj)
        {
            
            BoxGrid = obj.BoxGrid;
            Name = obj.Name;
            GTIN = obj.GTIN;
        }

        
    }

}
