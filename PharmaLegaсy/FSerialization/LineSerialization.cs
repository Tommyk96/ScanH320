using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Util;

namespace FSerialization
{
    [DataContract]
    public class Report
    {
        [DataMember]
        public string id = "";
        [DataMember]
        public string startTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");
        [DataMember]
        public string endTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssz");

        [DataMember]
        public List<Operator> operators = new List<Operator>();
        [DataMember]
        public List<Box> readyBox = new List<Box>();
        [DataMember]
        public List<DefectiveCode> defectiveCodes = new List<DefectiveCode>();
        [DataMember]
        public List<Sampled> sampleNumbers = new List<Sampled>();
        [DataMember]
        public List<string> emptyNumbers = new List<string>();

    }
}
