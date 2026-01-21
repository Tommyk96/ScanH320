using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Models
{
    public enum HandScannerMode
    {
        Default = 0,
        VerifyBox = 1,
        AddPack = 2,
        Brack = 3,
        Sample = 4,
        Help = 5,
        VerifyPallet = 6,
        Reprint = 7,
        AddCodeToCurrentBox = 8,
        DropCodeFromCurrentBox = 9
    }
}
