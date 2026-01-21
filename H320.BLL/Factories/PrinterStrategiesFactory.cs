using H320.BLL.Interfaces;
using H320.BLL.Strategies.Printers;

namespace H320.BLL.Factories
{
    public class PrinterStrategiesFactory
    {
        public static IPrinterDataStrategy CreatePrintDataStrategy(string printerType)
        {
            switch (printerType)
            {
                case "TSC":
                    return new TscDataPrepare();
                default:
                    return new DefaultPrintDataStrategy();
            }
        }
    }
}
