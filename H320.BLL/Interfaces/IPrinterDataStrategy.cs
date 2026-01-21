using System.Collections.Generic;

namespace H320.BLL.Interfaces
{
    public interface IPrinterDataStrategy
    {
       
        public List<byte> PreparedData(string data);
        List<byte> PreparedDmxData(string data);
    }
}
