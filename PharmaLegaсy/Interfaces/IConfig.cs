using System.ComponentModel;
using System.Runtime.Serialization;
using Util;

namespace PharmaLegaсy.Interfaces
{
    public interface IConfig
    {    
        public int NumRows {  get; set; }
        public int NumColumns { get; set; }
        public int BoxHeight { get; set; }
        public int BoxWidth { get; set; }
        public bool BoxGrid { get; set; }
        public bool AggregateOn { get; set; }

        public void Save();

    }
}
