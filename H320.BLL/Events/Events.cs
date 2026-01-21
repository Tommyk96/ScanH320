using BoxAgr.BLL.Models;
using FSerialization;
using Peripherals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Events
{
    public delegate void SessionStateEvent(int id, PeripheralsType type, SessionStates data);

    public delegate void MessageRecievedEvent(object sender, int id, string data);
    public delegate void DisconnectEvent(object sender, int id);

    public delegate void MaxNoReadInlineStateEvent(int id, int noreadCount);
    public delegate void AddLayerEvent(int id,  int point,bool manualAdd, BoxAddStatus state, Unit[] layer,BoxWithLayers box);

    public delegate void ScanDataEventHandler(string data);
    public delegate void EnterUserEventHandler(string data);

    public delegate void BoxInPositionHandler(object sender, bool state);
    public delegate void PowerLossHandler(object sender, bool state);

}
