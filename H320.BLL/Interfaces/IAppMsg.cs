using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace BoxAgr.BLL.Interfaces
{
    public interface IAppMsg
    {
        public void ClearMsgBelt();
        public void ShowMessageOnUpBanner(string moduleId, string msgData, EventLogEntryType errType, int eventId);
    }
}
