using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace PharmaLegacy.data
{
    public class Printer
    {
        public Printer(String uniqueId, String name, String port,String ipAddr,  String description)
        {
            this.UniqueId = uniqueId;
            this.Name = name;
            this.Port = port;
            this.IpAddr = ipAddr;
            this.Description = description;
        }

        public string UniqueId { get; private set; }
        public string Name { get; private set; }
        public string Port { get; private set; }
        public string IpAddr { get; private set; }
        public string Description { get; private set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

}
