using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BoxAgr.BLL.Http.Reports.Baikal
{
    [DataContract]
    public class BaikalReport
    {
        [DataMember]
        public string id { get; set; } = string.Empty;
        [DataMember]
        public string startTime { get; set; } = string.Empty;
        [DataMember]
        public string endTime { get; set; } = string.Empty;
        [DataMember]
        public List<UserAuthorizationHistotyItem> operators { get; set; } = new List<UserAuthorizationHistotyItem>();
        [DataMember]
        public List<BaikalItem> items { get; set; } = new();

    }
}
