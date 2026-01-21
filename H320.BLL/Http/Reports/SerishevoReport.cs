using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BoxAgr.BLL.Http.Reports
{
    [DataContract]
    public class SerishevoReport
    {
        [DataMember]
        public string id { get; set; } = string.Empty;
        [DataMember]
        public string startTime { get; set; } = string.Empty;
        [DataMember]
        public string endTime { get; set; } = string.Empty;
        [DataMember]
        public List<UserAuthorizationHistotyItem> operators { get; set; } = [];
        [DataMember]
        public List<string> defectiveCodes { get; set; } = [];
        [DataMember]
        public List<string> Packs { get; set; } = [];

        /// <summary>
        /// Не заполняется! только для совместимости!
        /// </summary>
        [DataMember]
        public List<object> repeatPacks { get; set; } = [];
    }
}
