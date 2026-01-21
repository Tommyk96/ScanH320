using H320.BLL.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace H320.BLL.Strategies.Printers
{
    internal class DefaultPrintDataStrategy : IPrinterDataStrategy
    {

        public List<byte> PreparedData(string data)
        {
            return [.. Encoding.UTF8.GetBytes(data)];
        }

        public List<byte> PreparedDmxData(string data)
        {
            return [.. Encoding.UTF8.GetBytes(data)];
        }
    }
}
