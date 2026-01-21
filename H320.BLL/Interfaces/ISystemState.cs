using PharmaLegaсy.Models;
using System.Collections.ObjectModel;

namespace BoxAgr.BLL.Interfaces
{
    public interface ISystemState
    {
        public System.Windows.Media.Brush StatusBackground { get; set; }
        public string StatusText { get; set; }
        public bool CriticalError { get; set; }

        public bool StopLine { get; set; }
        //public ObservableCollection<SerialCode> ListCurrentSerials { get; set; }
        //public string SerialInBoxCounter { get; set; }

    }
}
