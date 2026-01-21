using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace PharmaLegaсy.Models
{
#pragma warning disable CA1812
    [DataContract]
    public class BoxAdditionalNumbers
    {
        [DataMember]
        public string id = "";
        [DataMember]
        public List<string> boxNumbers = new List<string>();
    }
#pragma warning restore
}
